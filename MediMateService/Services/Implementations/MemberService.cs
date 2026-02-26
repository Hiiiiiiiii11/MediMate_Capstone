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

        public MemberService(
            IUnitOfWork unitOfWork,
            IUploadPhotoService uploadPhotoService,
            ICurrentUserService currentUserService,
            IAuthenticationService authService) // 2. Inject
        {
            _unitOfWork = unitOfWork;
            _uploadPhotoService = uploadPhotoService;
            _currentUserService = currentUserService;
            _authService = authService; // 3. Gán
        }
        public async Task<ApiResponse<IEnumerable<MemberResponse>>> GetAllMember()
        {
            var members = await _unitOfWork.Repository<Members>().GetAllAsync();
            var memberDto = members.Select(u => new MemberResponse
            {
                MemberId = u.MemberId,
                FamilyId = u.FamilyId,
                FullName = u.FullName,
                DateOfBirth = u.DateOfBirth,
                AvatarUrl = u.AvatarUrl,
                Gender = u.Gender,
                IsActive = u.IsActive,
                UserId = u.UserId,
                IdentityCode = u.IdentityCode,
                SyncToken = u.SyncToken,
                SyncTokenExpireAt = u.SyncTokenExpireAt,
                Role = u.Role,



            });
            return ApiResponse<IEnumerable<MemberResponse>>.Ok(memberDto, "Lấy danh sách members  thành công.");
        }

        // --- HÀM 1: TẠO PROFILE PHỤ THUỘC & JOIN GIA ĐÌNH ---
        public async Task<ApiResponse<InitDependentResponse>> InitDependentProfileAsync(InitDependentRequest request, Guid? currentUserId = null)
        {
            Families family = null;

            // --- TRƯỜNG HỢP 1: USER ĐÃ ĐĂNG NHẬP (BỐ MẸ TẠO CHO CON) ---
            if (currentUserId.HasValue && request.TargetFamilyId.HasValue)
            {
                family = await _unitOfWork.Repository<Families>().GetByIdAsync(request.TargetFamilyId.Value);
                if (family == null) return ApiResponse<InitDependentResponse>.Fail("Gia đình không tồn tại.", 404);

                // Check quyền: Người tạo phải thuộc gia đình này (tốt nhất là Owner)
                var requester = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == family.FamilyId && m.UserId == currentUserId.Value)).FirstOrDefault();

                if (requester == null || requester.Role != Roles.Owner)
                {
                    return ApiResponse<InitDependentResponse>.Fail("Chỉ Chủ gia đình mới có quyền tạo thêm thành viên.", 403);
                }
            }
            // --- TRƯỜNG HỢP 2: DEPENDENT CHƯA ĐĂNG NHẬP (TỰ TẠO BẰNG MÃ JOIN CODE) ---
            else if (!string.IsNullOrWhiteSpace(request.JoinCode))
            {
                family = (await _unitOfWork.Repository<Families>()
                    .FindAsync(f => f.JoinCode == request.JoinCode)).FirstOrDefault();

                if (family == null) return ApiResponse<InitDependentResponse>.Fail("Mã gia đình không chính xác.", 404);
            }
            // --- NẾU KHÔNG CÓ CẢ 2 ---
            else
            {
                return ApiResponse<InitDependentResponse>.Fail("Vui lòng cung cấp mã gia đình hoặc ID gia đình đích.", 400);
            }

            // 3. Kiểm tra loại gia đình (Dùng chung cho cả 2 trường hợp)
            if (family.Type == FamilyType.Personal)
            {
                return ApiResponse<InitDependentResponse>.Fail("Không thể thêm thành viên vào hồ sơ cá nhân.", 403);
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

            // --- LOGIC TỰ ĐỘNG ĐĂNG NHẬP Ở ĐÂY ---
            string? accessToken = null;
            // 5. Trả về kết quả
            if (!currentUserId.HasValue)
            {
                // Gọi sang AuthService để lấy Token
                accessToken = _authService.GenerateJwtTokenForDependent(newMember,"dependent");
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

            var response = members.Select(m => new MemberResponse
            {
                MemberId = m.MemberId,
                UserId = m.UserId,
                FullName = m.FullName,
                DateOfBirth = m.DateOfBirth,
                Gender = m.Gender,
                Role = m.Role,
                AvatarUrl = m.AvatarUrl,
                IsActive = m.IsActive
            });

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

            return ApiResponse<MemberResponse>.Ok(new MemberResponse
            {
                MemberId = member.MemberId,
                UserId = member.UserId,
                FullName = member.FullName,
                DateOfBirth = member.DateOfBirth,
                Gender = member.Gender,
                Role = member.Role,
                AvatarUrl = member.AvatarUrl,
                IsActive = member.IsActive
            });
        }

        public async Task<ApiResponse<MemberResponse>> UpdateMemberAsync(Guid memberId, Guid userId, UpdateMemberRequest request)
        {
            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (member == null)
            {
                return ApiResponse<MemberResponse>.Fail("Member not found", 404);
            }

            // Check quyền: 
            // 1. Chính chủ (UserId khớp)
            // 2. Hoặc là Owner của Family đó
            bool isSelf = member.UserId == userId;
            bool isOwner = false;

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

            // Update logic
            member.FullName = request.FullName;
            if (request.DateOfBirth.HasValue)
            {
                member.DateOfBirth = request.DateOfBirth.Value;
            }

            member.Gender = request.Gender;

            // Chỉ Owner mới được quyền đổi Role người khác (VD: Thăng chức vợ lên làm Owner)
            //if (isOwner && !string.IsNullOrEmpty(request.Role))
            //{
            //    member.Role = request.Role;
            //}if (request.AvatarFile != null)
            {
                // Gọi hàm upload mới
                var uploadResult = await _uploadPhotoService.UploadPhotoAsync(request.AvatarFile);

                // Lưu link vào DB
                // Với Avatar, ta thường dùng OriginalUrl (đã được crop face ở service)
                member.AvatarUrl = uploadResult.OriginalUrl;
            }

            _unitOfWork.Repository<Members>().Update(member);
            await _unitOfWork.CompleteAsync();

            return await GetMemberByIdAsync(memberId, userId);
        }

        public async Task<ApiResponse<bool>> RemoveMemberAsync(Guid memberId, Guid userId)
        {
            var memberToRemove = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (memberToRemove == null)
            {
                return ApiResponse<bool>.Fail("Member not found", 404);
            }

            // Check quyền
            bool isSelf = memberToRemove.UserId == userId; // Tự rời nhóm
            bool isOwner = false; // Chủ kick

            if (memberToRemove.FamilyId != null)
            {
                var requester = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == memberToRemove.FamilyId && m.UserId == userId)).FirstOrDefault();
                if (requester != null && requester.Role == Roles.Owner)
                {
                    isOwner = true;
                }
            }

            if (!isSelf && !isOwner)
            {
                return ApiResponse<bool>.Fail("Không có quyền thực hiện.", 403);
            }

            // Logic Xóa:
            // Cách 1: Xóa hẳn khỏi DB (Hard Delete)
            // _unitOfWork.Repository<Members>().Remove(memberToRemove);

            // Cách 2: Set FamilyId = null (Kick ra khỏi nhà, thành mồ côi) -> NÊN DÙNG
            memberToRemove.FamilyId = null;
            memberToRemove.IsActive = false; // Tạm thời unactive

            _unitOfWork.Repository<Members>().Update(memberToRemove);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Đã xóa thành viên khỏi gia đình.");
        }


        public async Task<ApiResponse<bool>> JoinFamilyUnifiedAsync(Guid? userId, JoinFamilyRequest request)
        {
            // 1. Tìm Family theo Code
            var family = (await _unitOfWork.Repository<Families>()
                .FindAsync(f => f.JoinCode == request.JoinCode)).FirstOrDefault();

            if (family == null)
            {
                return ApiResponse<bool>.Fail("Mã gia đình không chính xác.", 404);
            }

            if (family.Type == FamilyType.Personal)
            {
                return ApiResponse<bool>.Fail("Không thể tham gia hồ sơ cá nhân.", 403);
            }

            // --- TRƯỜNG HỢP A: NGƯỜI DÙNG ĐÃ ĐĂNG NHẬP (CÓ USER ID) ---
            if (userId.HasValue)
            {
                // Kiểm tra đã trong nhóm chưa
                var exists = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == family.FamilyId && m.UserId == userId.Value)).Any();

                if (exists)
                {
                    return ApiResponse<bool>.Ok(true, "Bạn đã là thành viên của gia đình này.");
                }

                // Lấy info User để tạo Member mới
                var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId.Value);
                var newMember = new Members
                {
                    MemberId = Guid.NewGuid(),
                    FamilyId = family.FamilyId,
                    UserId = userId.Value, // Link tài khoản
                    FullName = user.FullName,
                    DateOfBirth = user.DateOfBirth ?? DateTime.UtcNow,
                    Gender = user.Gender ?? "Other",
                    Role = Roles.Member,
                    AvatarUrl = user.AvatarUrl,
                    IsActive = true
                };

                await _unitOfWork.Repository<Members>().AddAsync(newMember);
                await _unitOfWork.CompleteAsync();

                return ApiResponse<bool>.Ok(true, $"Đã tham gia gia đình '{family.FamilyName}' với tư cách thành viên.");
            }

            // --- TRƯỜNG HỢP B: NGƯỜI PHỤ THUỘC (CHƯA LOGIN, CÓ MEMBER ID) ---
            else if (request.ExistingMemberId.HasValue)
            {
                // Tìm Member mồ côi đang lưu trên máy
                var member = await _unitOfWork.Repository<Members>().GetByIdAsync(request.ExistingMemberId.Value);

                if (member == null)
                {
                    return ApiResponse<bool>.Fail("Hồ sơ thành viên không tồn tại.", 404);
                }

                // Nếu đã có nhà rồi thì thôi (trừ khi chính là nhà này)
                if (member.FamilyId != null)
                {
                    return member.FamilyId == family.FamilyId
                        ? ApiResponse<bool>.Ok(true, "Đã ở trong gia đình này.")
                        : ApiResponse<bool>.Fail("Hồ sơ này đã thuộc về gia đình khác.", 409);
                }

                // Cập nhật Family
                member.FamilyId = family.FamilyId;
                //member.IdentityCode = null; // Reset mã định danh

                _unitOfWork.Repository<Members>().Update(member);
                await _unitOfWork.CompleteAsync();

                return ApiResponse<bool>.Ok(true, $"Hồ sơ '{member.FullName}' đã được thêm vào gia đình '{family.FamilyName}'.");
            }

            // --- TRƯỜNG HỢP C: KHÔNG CÓ CẢ 2 ---
            return ApiResponse<bool>.Fail("Dữ liệu không hợp lệ. Cần đăng nhập hoặc có hồ sơ thành viên.", 400);
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
            member.SyncTokenExpireAt = DateTime.UtcNow.AddMinutes(5); // Mã QR chỉ có hiệu lực 5 phút

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


    }
}