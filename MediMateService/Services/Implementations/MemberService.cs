using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs; // Nhớ cài package QRCoder
using Share.Common;
using Share.Constants;
using static MediMateRepository.Model.Families;

namespace MediMateService.Services.Implementations
{
    public class MemberService : IMemberService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadPhotoService _uploadPhotoService;
        private readonly ICurrentUserService _currentUserService;

        // 1. THÊM Auth Service
        private readonly IAuthenticationService _authService;
        private readonly IActivityLogService _activityLogService;

        public MemberService(
            IUnitOfWork unitOfWork,
            IUploadPhotoService uploadPhotoService,
            ICurrentUserService currentUserService,
            IAuthenticationService authService,
            IActivityLogService activityLogService) // 2. Inject
        {
            _unitOfWork = unitOfWork;
            _uploadPhotoService = uploadPhotoService;
            _currentUserService = currentUserService;
            _authService = authService; // 3. Gán
            _activityLogService = activityLogService;
        }

        public async Task<ApiResponse<IEnumerable<MemberResponse>>> GetAllMember()
        {
            var members = await _unitOfWork.Repository<Members>().GetAllAsync();
            var memberDto = members.Select(MapToResponse);

            return ApiResponse<IEnumerable<MemberResponse>>.Ok(memberDto, "Lấy danh sách members thành công.");
        }

        // --- HÀM 1: TẠO PROFILE PHỤ THUỘC & JOIN GIA ĐÌNH ---
        public async Task<ApiResponse<InitDependentResponse>> InitDependentProfileAsync(InitDependentRequest request, Guid? currentUserId = null)
        {
            Families family = null;
            Members requester = null;

            // --- TRƯỜNG HỢP 1: USER ĐÃ ĐĂNG NHẬP (BỐ MẸ TẠO CHO CON) ---
            if (currentUserId.HasValue && request.TargetFamilyId.HasValue)
            {
                family = await _unitOfWork.Repository<Families>().GetByIdAsync(request.TargetFamilyId.Value);
                if (family == null) return ApiResponse<InitDependentResponse>.Fail("Gia đình không tồn tại.", 404);

                requester = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == family.FamilyId && m.UserId == currentUserId.Value)).FirstOrDefault();

                if (requester == null || requester.Role != Roles.Owner)
                {
                    return ApiResponse<InitDependentResponse>.Fail("Chỉ Chủ gia đình mới có quyền tạo thêm thành viên.", 403);
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.JoinCode))
            {
                family = (await _unitOfWork.Repository<Families>()
                    .FindAsync(f => f.JoinCode == request.JoinCode)).FirstOrDefault();

                if (family == null) return ApiResponse<InitDependentResponse>.Fail("Mã gia đình không chính xác.", 404);
            }
            else
            {
                return ApiResponse<InitDependentResponse>.Fail("Vui lòng cung cấp mã gia đình hoặc ID gia đình đích.", 400);
            }

            if (!family.IsOpenJoin && requester == null) // Bỏ qua check mã nếu là owner tự tạo con
            {
                return ApiResponse<InitDependentResponse>.Fail("Gia đình này đang tạm khóa tính năng tham gia bằng mã.", 403);
            }

            if (family.Type == FamilyType.Personal)
            {
                return ApiResponse<InitDependentResponse>.Fail("Không thể thêm thành viên vào hồ sơ cá nhân.", 403);
            }
            string normalizedName = request.FullName.Trim().ToLower();

            var isDuplicate = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == family.FamilyId &&
                                m.FullName.ToLower() == normalizedName &&
                                m.IsActive == true)) // Chỉ check những người đang hoạt động
                .Any();

            if (isDuplicate)
            {
                return ApiResponse<InitDependentResponse>.Fail($"Thành viên có tên '{request.FullName}' đã tồn tại trong gia đình này.", 409);
            }

            var newMember = new Members
            {
                MemberId = Guid.NewGuid(),
                FamilyId = family.FamilyId,
                UserId = null,
                FullName = request.FullName,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Role = Roles.Member,
                IdentityCode = null,
                IsActive = true
            };

            await _unitOfWork.Repository<Members>().AddAsync(newMember);
            await _unitOfWork.CompleteAsync();

            Guid doerId = requester != null ? requester.MemberId : newMember.MemberId;
            await _activityLogService.LogActivityAsync(
                familyId: family.FamilyId,
                memberId: doerId,
                actionType: ActivityActionTypes.CREATE,
                entityName: ActivityEntityNames.MEMBER,
                entityId: newMember.MemberId,
                description: $"Đã thêm thành viên mới: {newMember.FullName}."
            );
            // --- LOGIC TỰ ĐỘNG ĐĂNG NHẬP Ở ĐÂY ---
            string? accessToken = null;
            // 5. Trả về kết quả
            if (!currentUserId.HasValue)
            {
                // Gọi sang AuthService để lấy Token
                accessToken = _authService.GenerateJwtTokenForDependent(newMember, "dependent");
            }

            // Trả về kết quả kèm Token
            return ApiResponse<InitDependentResponse>.Ok(new InitDependentResponse
            {
                MemberId = newMember.MemberId,
                FamilyId = family.FamilyId,
                FullName = newMember.FullName,
                FamilyName = family.FamilyName,

                AccessToken = accessToken // Frontend nhận cái này và lưu vào LocalStorage

            }, $"Tạo hồ sơ và thêm vào gia đình '{family.FamilyName}' thành công.");
        }

        // --- HÀM 2: CHỦ FAMILY QUÉT QR ĐỂ NHẬN MEMBER ---

        public async Task<ApiResponse<IEnumerable<MemberResponse>>> GetMembersByFamilyIdAsync(Guid familyId, Guid userId)
        {
            // Check quyền: User phải thuộc Family đó mới được xem danh sách
            var currentUserMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId && m.UserId == userId)).FirstOrDefault();

            if (currentUserMember == null)
            {
                return ApiResponse<IEnumerable<MemberResponse>>.Fail("Bạn không thuộc gia đình này.", 403);
            }

            var members = await _unitOfWork.Repository<Members>().FindAsync(m => m.FamilyId == familyId);

            var response = members.Select(MapToResponse);

            return ApiResponse<IEnumerable<MemberResponse>>.Ok(response);
        }

        public async Task<ApiResponse<MemberResponse>> GetMemberByIdAsync(Guid memberId, Guid userId)
        {
            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (member == null)
            {
                return ApiResponse<MemberResponse>.Fail("Thành viên không tồn tại", 404);
            }

            // Check quyền: Phải cùng Family mới xem được (Trừ khi member này chưa có family - mồ côi)
            if (member.FamilyId != null)
            {
                var isFamilyMate = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == member.FamilyId && m.UserId == userId)).Any();

                if (!isFamilyMate)
                {
                    return ApiResponse<MemberResponse>.Fail("Không có quyền truy cập.", 403);
                }
            }

            return ApiResponse<MemberResponse>.Ok(MapToResponse(member));
        }

        public async Task<ApiResponse<MemberResponse>> UpdateMemberAsync(Guid memberId, Guid userId, UpdateMemberRequest request)
        {
            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (member == null)
            {
                return ApiResponse<MemberResponse>.Fail("Member not found", 404);
            }
            bool isSelf = member.UserId == userId;
            bool isOwner = false;
            Members doer = null;

            if (member.FamilyId != null)
            {
                var requester = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == member.FamilyId && m.UserId == userId)).FirstOrDefault();
                if (requester != null && requester.Role == Roles.Owner)
                {
                    isOwner = true;
                }
            }

            if (!isSelf && !isOwner)
            {
                return ApiResponse<MemberResponse>.Fail("Bạn không có quyền sửa thông tin này.", 403);
            }
            var oldData = new { member.FullName, member.DateOfBirth, member.Gender, member.AvatarUrl };
            // Update logic
            member.FullName = request.FullName ?? member.FullName;
            if (request.DateOfBirth.HasValue) member.DateOfBirth = request.DateOfBirth.Value;
            if (!string.IsNullOrEmpty(request.Gender)) member.Gender = request.Gender;

            if (request.AvatarFile != null)
            {
                var uploadResult = await _uploadPhotoService.UploadPhotoAsync(request.AvatarFile);
                member.AvatarUrl = uploadResult.OriginalUrl;
            }

            _unitOfWork.Repository<Members>().Update(member);
            await _unitOfWork.CompleteAsync();
            if (doer != null && member.FamilyId.HasValue)
            {
                var newData = new { member.FullName, member.DateOfBirth, member.Gender, member.AvatarUrl };
                await _activityLogService.LogActivityAsync(
                    familyId: member.FamilyId.Value,
                    memberId: doer.MemberId,
                    actionType: ActivityActionTypes.UPDATE,
                    entityName: ActivityEntityNames.MEMBER,
                    entityId: member.MemberId,
                    description: $"Đã cập nhật thông tin của thành viên '{member.FullName}'.",
                    oldData: oldData,
                    newData: newData
                );
            }

            return await GetMemberByIdAsync(memberId, userId);
        }

        public async Task<ApiResponse<bool>> RemoveMemberAsync(Guid memberId, Guid userId)
        {
            var memberToRemove = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (memberToRemove == null)
            {
                return ApiResponse<bool>.Fail("Member not found", 404);
            }
            Guid familyId = memberToRemove.FamilyId.Value;
            // Check quyền
            bool isSelf = memberToRemove.UserId == userId; // Tự rời nhóm
            bool isOwner = false; // Chủ kick
            Members doer = null;

            var requester = (await _unitOfWork.Repository<Members>()
                 .FindAsync(m => m.FamilyId == familyId && m.UserId == userId)).FirstOrDefault();

            if (requester != null)
            {
                doer = requester;
                if (requester.Role == Roles.Owner) isOwner = true;
            }

            if (!isSelf && !isOwner)
                return ApiResponse<bool>.Fail("Không có quyền thực hiện.", 403);


            // Logic Xóa:
            // Cách 1: Xóa hẳn khỏi DB (Hard Delete)
            // _unitOfWork.Repository<Members>().Remove(memberToRemove);

            // Cách 2: Set FamilyId = null (Kick ra khỏi nhà, thành mồ côi) -> NÊN DÙNG
            memberToRemove.FamilyId = null;
            memberToRemove.IsActive = false; // Tạm thời unactive

            _unitOfWork.Repository<Members>().Update(memberToRemove);
            await _unitOfWork.CompleteAsync();
            if (doer != null)
            {
                string actionType = isSelf ? ActivityActionTypes.LEAVE : ActivityActionTypes.KICK;
                string desc = isSelf ? $"Đã rời khỏi gia đình." : $"Đã mời '{memberToRemove.FullName}' ra khỏi gia đình.";

                await _activityLogService.LogActivityAsync(
                    familyId: familyId,
                    memberId: doer.MemberId,
                    actionType: actionType,
                    entityName: ActivityEntityNames.MEMBER,
                    entityId: memberToRemove.MemberId,
                    description: desc
                );
            }

            return ApiResponse<bool>.Ok(true, "Đã xóa thành viên khỏi gia đình.");
        }
        public async Task<ApiResponse<bool>> DeleteMemberAsync(Guid memberId, Guid userId)
        {
            var memberToRemove = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (memberToRemove == null)
            {
                return ApiResponse<bool>.Fail("Member not found", 404);
            }

            // Check quyền
            bool isSelf = memberToRemove.UserId == userId; // Tự rời nhóm
            bool isOwner = false; // Chủ kick
            Members doer = null;
            Guid? familyId = memberToRemove.FamilyId;

            if (familyId != null)
            {
                var requester = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == familyId && m.UserId == userId)).FirstOrDefault();

                if (requester != null)
                {
                    doer = requester;
                    if (requester.Role == Roles.Owner) isOwner = true;
                }
            }

            if (!isSelf && !isOwner)
                return ApiResponse<bool>.Fail("Không có quyền thực hiện.", 403);
            string memberName = memberToRemove.FullName;
            // Logic Xóa:
            // Cách 1: Xóa hẳn khỏi DB (Hard Delete)
            _unitOfWork.Repository<Members>().Remove(memberToRemove);

            //// Cách 2: Set FamilyId = null (Kick ra khỏi nhà, thành mồ côi) -> NÊN DÙNG
            //memberToRemove.FamilyId = null;
            //memberToRemove.IsActive = false; // Tạm thời unactive

            //xóa vĩnh viễn khỏi DB 
            await _unitOfWork.CompleteAsync();

            if (doer != null && familyId.HasValue)
            {
                await _activityLogService.LogActivityAsync(
                    familyId: familyId.Value,
                    memberId: doer.MemberId,
                    actionType: ActivityActionTypes.DELETE,
                    entityName: ActivityEntityNames.MEMBER,
                    entityId: memberId,
                    description: $"Đã xóa vĩnh viễn hồ sơ của '{memberName}'."
                );
            }

            return ApiResponse<bool>.Ok(true, "Đã xóa thành viên khỏi gia đình.");
        }



        public async Task<ApiResponse<MemberQrResponse>> GenerateLoginQrForDependentAsync(Guid memberId, Guid currentUserId)
        {
            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (member == null) return ApiResponse<MemberQrResponse>.Fail("Thành viên không tồn tại.", 404);

            // Kiểm tra quyền: Chỉ chủ nhà hoặc người tạo mới được cấp mã đăng nhập cho người phụ thuộc
            if (!await _currentUserService.CheckAccess(memberId, currentUserId))
                return ApiResponse<MemberQrResponse>.Fail("Không có quyền cấp mã đăng nhập.", 403);

            // 1. Tạo SyncToken ngẫu nhiên (chỉ dùng 1 lần)
            // Guid.NewGuid().ToString("N") tạo ra chuỗi 32 ký tự bảo mật
            var syncToken = Guid.NewGuid().ToString("N");

            member.SyncToken = syncToken;
            member.SyncTokenExpireAt = DateTime.Now.AddMinutes(5); // Mã QR chỉ có hiệu lực 5 phút

            _unitOfWork.Repository<Members>().Update(member);
            await _unitOfWork.CompleteAsync();

            // 2. Tạo URL QR (Lưu ý: Data QR bây giờ là SyncToken chứ không phải IdentityCode)
            // Thêm prefix "LOGIN-" để Frontend dễ phân biệt loại mã QR
            string qrData = $"LOGIN-{syncToken}";
            var qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=350x350&data={qrData}";

            return ApiResponse<MemberQrResponse>.Ok(new MemberQrResponse
            {
                MemberId = member.MemberId,
                FullName = member.FullName,
                IdentityCode = member.IdentityCode, // Vẫn trả về nếu cần thiết
                QrCodeUrl = qrCodeUrl
            }, "Tạo mã QR đăng nhập thành công. Mã có hiệu lực trong 5 phút.");
        }

        // --- HÀM 3: THÊM THÀNH VIÊN LÀ USER ĐÃ CÓ TÀI KHOẢN (Vợ/Chồng/Con lớn) ---
        public async Task<ApiResponse<MemberResponse>> AddUserMemberToFamilyAsync(AddUserMemberRequest request, Guid ownerUserId)
        {
            // 1. Tìm Family của Owner
            var ownerMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == request.FamilyId && m.UserId == ownerUserId)).FirstOrDefault();

            if (ownerMember == null || ownerMember.Role != Roles.Owner)
            {
                return ApiResponse<MemberResponse>.Fail("Bạn không phải chủ gia đình hoặc không thuộc gia đình này.", 403);
            }

            // 2. Tìm User cần thêm qua Số điện thoại
            var targetUser = (await _unitOfWork.Repository<User>()
                .FindAsync(u => u.PhoneNumber == request.PhoneNumber)).FirstOrDefault();

            if (targetUser == null)
            {
                return ApiResponse<MemberResponse>.Fail("Không tìm thấy người dùng với số điện thoại này.", 404);
            }

            // 3. Kiểm tra xem người này đã ở trong Family này chưa
            var existingMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == request.FamilyId && m.UserId == targetUser.UserId)).FirstOrDefault();

            if (existingMember != null)
            {
                return ApiResponse<MemberResponse>.Fail("Người này đã là thành viên trong gia đình.", 409);
            }

            // 4. Kiểm tra xem người này đã ở Family khác chưa (Nếu logic business chỉ cho phép 1 người 1 nhà)
            // (Tùy chọn: Nếu cho phép ở nhiều nhà thì bỏ qua đoạn này)
            /*
            var inOtherFamily = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.UserId == targetUser.UserId)).Any();
            if (inOtherFamily) return ApiResponse<MemberResponse>.Fail("Người dùng này đã tham gia một gia đình khác.", 409);
            */

            // 5. Tạo Member mới liên kết với UserId
            var newMember = new Members
            {
                MemberId = Guid.NewGuid(),
                FamilyId = request.FamilyId,
                UserId = targetUser.UserId, // QUAN TRỌNG: Link tới Account thật
                FullName = targetUser.FullName, // Lấy tên thật từ Account
                DateOfBirth = targetUser.DateOfBirth ?? DateTime.Now,
                Gender = targetUser.Gender ?? "Other",
                Role = Roles.Member, // Mặc định là Member
                AvatarUrl = targetUser.AvatarUrl,
                IsActive = true
            };

            await _unitOfWork.Repository<Members>().AddAsync(newMember);
            await _unitOfWork.CompleteAsync();

            await _activityLogService.LogActivityAsync(
                familyId: request.FamilyId,
                memberId: ownerMember.MemberId,
                actionType: ActivityActionTypes.CREATE,
                entityName: ActivityEntityNames.MEMBER,
                entityId: newMember.MemberId,
                description: $"Đã thêm tài khoản '{newMember.FullName}' vào gia đình qua SĐT."
            );

            return ApiResponse<MemberResponse>.Ok(MapToResponse(newMember), "Thêm thành viên thành công.");
        }

        // --- HÀM 4: TẠO THÀNH VIÊN PHỤ THUỘC (CON CÁI/NGƯỜI GIÀ - KHÔNG CÓ USER) ---
        public async Task<ApiResponse<MemberResponse>> CreateDependentMemberAsync(CreateDependentRequest request, Guid ownerUserId)
        {
            // 1. Validate quyền chủ hộ
            var ownerMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == request.FamilyId && m.UserId == ownerUserId)).FirstOrDefault();

            if (ownerMember == null || ownerMember.Role != Roles.Owner)
            {
                return ApiResponse<MemberResponse>.Fail("Chỉ chủ gia đình mới được tạo hồ sơ phụ thuộc.", 403);
            }
            string normalizedName = request.FullName.Trim().ToLower();

            var isDuplicate = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == request.FamilyId &&
                                m.FullName.ToLower() == normalizedName &&
                                m.IsActive == true))
                .Any();

            if (isDuplicate)
            {
                return ApiResponse<MemberResponse>.Fail($"Thành viên có tên '{request.FullName}' đã tồn tại trong gia đình này. Vui lòng đặt tên khác", 409);
            }

            // 2. Tạo Member với UserId = NULL (Đây là dấu hiệu nhận biết Dependent)
            var newDependent = new Members
            {
                MemberId = Guid.NewGuid(),
                FamilyId = request.FamilyId,
                UserId = null, // QUAN TRỌNG: Không có tài khoản đăng nhập
                FullName = request.FullName,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Role = Roles.Member, // Dependent luôn là Member thường
                AvatarUrl = null, // Avatar mặc định
                IsActive = true
            };

            await _unitOfWork.Repository<Members>().AddAsync(newDependent);
            await _unitOfWork.CompleteAsync();
            return ApiResponse<MemberResponse>.Ok(MapToResponse(newDependent), "Tạo hồ sơ thành viên phụ thuộc thành công.");
        }
        // --- HÀM 5: USER TỰ JOIN GIA ĐÌNH BẰNG MÃ CODE ---
        public async Task<ApiResponse<bool>> JoinFamilyByJoinCodeAsync(JoinFamilyByCodeRequest request, Guid userId)
        {
            // 1. Tìm Family theo Code
            var family = (await _unitOfWork.Repository<Families>()
                .FindAsync(f => f.JoinCode == request.JoinCode)).FirstOrDefault();

            if (family == null)
            {
                return ApiResponse<bool>.Fail("Mã gia đình không chính xác.", 404);
            }

            // 2. CHECK QUYỀN MỞ CỬA (Logic bạn cần thêm)
            // Nếu gia đình đang đóng cửa (IsOpenJoin = false) -> Chặn ngay
            if (!family.IsOpenJoin)
            {
                return ApiResponse<bool>.Fail("Gia đình này đang tạm khóa tính năng tham gia bằng mã.", 403);
            }

            // 3. Không cho join vào Personal Family
            if (family.Type == FamilyType.Personal)
            {
                return ApiResponse<bool>.Fail("Không thể tham gia vào hồ sơ cá nhân.", 403);
            }

            // 4. Kiểm tra User đã ở trong gia đình này chưa
            var existingMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == family.FamilyId && m.UserId == userId)).FirstOrDefault();

            if (existingMember != null)
            {
                if (existingMember.IsActive == false)
                {
                    // Re-active lại thành viên cũ
                    existingMember.IsActive = true;
                    existingMember.Role = Roles.Member;
                    _unitOfWork.Repository<Members>().Update(existingMember);
                    await _unitOfWork.CompleteAsync();
                    return ApiResponse<bool>.Ok(true, $"Chào mừng bạn quay lại gia đình '{family.FamilyName}'.");
                }
                return ApiResponse<bool>.Ok(true, "Bạn đã là thành viên của gia đình này.");
            }

            // 5. Thêm thành viên mới
            var currentUser = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (currentUser == null) return ApiResponse<bool>.Fail("User error", 404);

            var newMember = new Members
            {
                MemberId = Guid.NewGuid(),
                FamilyId = family.FamilyId,
                UserId = userId,
                FullName = currentUser.FullName,
                DateOfBirth = currentUser.DateOfBirth ?? DateTime.Now,
                Gender = currentUser.Gender ?? "Other",
                AvatarUrl = currentUser.AvatarUrl,
                Role = Roles.Member,
                IsActive = true
            };

            await _unitOfWork.Repository<Members>().AddAsync(newMember);
            await _unitOfWork.CompleteAsync();

            await _activityLogService.LogActivityAsync(
                familyId: family.FamilyId,
                memberId: newMember.MemberId,
                actionType: ActivityActionTypes.JOIN,
                entityName: ActivityEntityNames.MEMBER,
                entityId: newMember.MemberId,
                description: $"Đã tham gia gia đình bằng Mã mời."
            );

            return ApiResponse<bool>.Ok(true, $"Tham gia gia đình '{family.FamilyName}' thành công.");
        }

        private MemberResponse MapToResponse(Members m)
        {
            return new MemberResponse
            {
                MemberId = m.MemberId,
                FamilyId = m.FamilyId,
                UserId = m.UserId,
                FullName = m.FullName,
                DateOfBirth = m.DateOfBirth,
                AvatarUrl = m.AvatarUrl,
                Gender = m.Gender,
                IsActive = m.IsActive,
                IdentityCode = m.IdentityCode,
                SyncToken = m.SyncToken,
                SyncTokenExpireAt = m.SyncTokenExpireAt,
                Role = m.Role
            };
        }

    }
}