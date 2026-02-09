using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs; // Nhớ cài package QRCoder
using QRCoder;
using Share.Common;
using System.Drawing;
using System.Drawing.Imaging;
using static MediMateRepository.Model.Families;

namespace MediMateService.Services
{
    public interface IMemberService
    {
        Task<ApiResponse<IEnumerable<MemberResponse>>> GetAllMember();
        Task<ApiResponse<InitDependentResponse>> InitDependentProfileAsync(InitDependentRequest request);
        Task<ApiResponse<bool>> AddMemberByIdentityQrAsync(Guid ownerId, AddMemberByIdentityRequest request);
        Task<ApiResponse<IEnumerable<MemberResponse>>> GetMembersByFamilyIdAsync(Guid familyId, Guid userId);

        // Get Member Detail
        Task<ApiResponse<MemberResponse>> GetMemberByIdAsync(Guid memberId, Guid userId);

        // Update Member Info
        Task<ApiResponse<MemberResponse>> UpdateMemberAsync(Guid memberId, Guid userId, UpdateMemberRequest request);

        // Delete/Remove Member (Soft delete or Remove from family)
        Task<ApiResponse<bool>> RemoveMemberAsync(Guid memberId, Guid userId);
        Task<ApiResponse<MemberQrResponse>> GetIdentityQrAsync(Guid memberId);
        Task<ApiResponse<bool>> JoinFamilyUnifiedAsync(Guid? userId, JoinFamilyRequest request);
    }

    public class MemberService : IMemberService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MemberService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

        // --- HÀM 1: TẠO PROFILE ĐỘC LẬP (MỒ CÔI) ---
        public async Task<ApiResponse<InitDependentResponse>> InitDependentProfileAsync(InitDependentRequest request)
        {
            // 1. Sinh mã định danh ngắn gọn (8 ký tự)
            var identityCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            // 2. Tạo Member mới
            var newMember = new Members
            {
                MemberId = Guid.NewGuid(),
                FamilyId = null, // QUAN TRỌNG: Chưa thuộc về ai
                UserId = null,   // Chưa có tài khoản login
                FullName = request.FullName,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Role = "Member",
                IdentityCode = identityCode, // Lưu mã để chờ quét
                IsActive = true
            };

            await _unitOfWork.Repository<Members>().AddAsync(newMember);
            await _unitOfWork.CompleteAsync();

            // 3. Tạo ảnh QR từ IdentityCode
            var qrBase64 = GenerateQrCode(identityCode);

            return ApiResponse<InitDependentResponse>.Ok(new InitDependentResponse
            {
                MemberId = newMember.MemberId,
                FullName = newMember.FullName,
                IdentityCode = identityCode,
                QrCodeBase64 = qrBase64
            });
        }

        // --- HÀM 2: CHỦ FAMILY QUÉT QR ĐỂ NHẬN MEMBER ---
        public async Task<ApiResponse<bool>> AddMemberByIdentityQrAsync(Guid ownerId, AddMemberByIdentityRequest request)
        {
            // 1. Kiểm tra Family đích và quyền Owner
            var targetFamily = await _unitOfWork.Repository<Families>().GetByIdAsync(request.TargetFamilyId);
            if (targetFamily == null) return ApiResponse<bool>.Fail("Gia đình không tồn tại.", 404);

            if (targetFamily.CreateBy != ownerId)
                return ApiResponse<bool>.Fail("Bạn không có quyền thêm thành viên vào gia đình này.", 403);
            if (targetFamily.Type == FamilyType.Personal)
            {
                return ApiResponse<bool>.Fail("Đây là hồ sơ cá nhân ! Không có quyền thêm thành viên vào gia đình này.", 403);
            }

            // 2. Tìm Member dựa vào IdentityCode (quét được từ QR)
            var targetMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.IdentityCode == request.IdentityCode)).FirstOrDefault();

            if (targetMember == null)
            {
                return ApiResponse<bool>.Fail("Mã QR không hợp lệ hoặc hồ sơ không tồn tại.", 404);
            }

            // 3. Kiểm tra xem Member này đã có nhà chưa (Tránh việc quét trộm người đã có gia đình)
            if (targetMember.FamilyId != null)
            {
                return ApiResponse<bool>.Fail("Thành viên này đã thuộc về một gia đình khác.", 409);
            }

            // 4. CẬP NHẬT FAMILY ID (Chính thức gia nhập)
            targetMember.FamilyId = request.TargetFamilyId;

            // 5. Xóa IdentityCode để bảo mật (Mã này chỉ dùng 1 lần để join)
            // Nếu muốn dùng lại mã này cho nhiều việc khác thì giữ lại, nhưng tốt nhất là xóa/reset.
            targetMember.IdentityCode = null;

            _unitOfWork.Repository<Members>().Update(targetMember);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, $"Đã thêm thành viên {targetMember.FullName} thành công.");
        }

        public async Task<ApiResponse<IEnumerable<MemberResponse>>> GetMembersByFamilyIdAsync(Guid familyId, Guid userId)
        {
            // Check quyền: User phải thuộc Family đó mới được xem danh sách
            var currentUserMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId && m.UserId == userId)).FirstOrDefault();

            if (currentUserMember == null)
                return ApiResponse<IEnumerable<MemberResponse>>.Fail("Bạn không thuộc gia đình này.", 403);

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
            if (member == null) return ApiResponse<MemberResponse>.Fail("Thành viên không tồn tại", 404);

            // Check quyền: Phải cùng Family mới xem được (Trừ khi member này chưa có family - mồ côi)
            if (member.FamilyId != null)
            {
                var isFamilyMate = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == member.FamilyId && m.UserId == userId)).Any();

                if (!isFamilyMate) return ApiResponse<MemberResponse>.Fail("Không có quyền truy cập.", 403);
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
            if (member == null) return ApiResponse<MemberResponse>.Fail("Member not found", 404);

            // Check quyền: 
            // 1. Chính chủ (UserId khớp)
            // 2. Hoặc là Owner của Family đó
            bool isSelf = member.UserId == userId;
            bool isOwner = false;

            if (member.FamilyId != null)
            {
                var requester = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == member.FamilyId && m.UserId == userId)).FirstOrDefault();
                if (requester != null && requester.Role == "Owner") isOwner = true;
            }

            if (!isSelf && !isOwner) return ApiResponse<MemberResponse>.Fail("Bạn không có quyền sửa thông tin này.", 403);

            // Update logic
            member.FullName = request.FullName;
            if (request.DateOfBirth.HasValue) member.DateOfBirth = request.DateOfBirth.Value;
            member.Gender = request.Gender;

            // Chỉ Owner mới được quyền đổi Role người khác (VD: Thăng chức vợ lên làm Owner)
            //if (isOwner && !string.IsNullOrEmpty(request.Role))
            //{
            //    member.Role = request.Role;
            //}

            _unitOfWork.Repository<Members>().Update(member);
            await _unitOfWork.CompleteAsync();

            return await GetMemberByIdAsync(memberId, userId);
        }

        public async Task<ApiResponse<bool>> RemoveMemberAsync(Guid memberId, Guid userId)
        {
            var memberToRemove = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (memberToRemove == null) return ApiResponse<bool>.Fail("Member not found", 404);

            // Check quyền
            bool isSelf = memberToRemove.UserId == userId; // Tự rời nhóm
            bool isOwner = false; // Chủ kick

            if (memberToRemove.FamilyId != null)
            {
                var requester = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == memberToRemove.FamilyId && m.UserId == userId)).FirstOrDefault();
                if (requester != null && requester.Role == "Owner") isOwner = true;
            }

            if (!isSelf && !isOwner) return ApiResponse<bool>.Fail("Không có quyền thực hiện.", 403);

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

        // --- Helper: Tạo QR Code ---
        private string GenerateQrCode(string content)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImage = qrCode.GetGraphic(20);
                    return "data:image/png;base64," + Convert.ToBase64String(qrCodeImage);
                }
            }
        }
        public async Task<ApiResponse<MemberQrResponse>> GetIdentityQrAsync(Guid memberId)
        {
            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (member == null) return ApiResponse<MemberQrResponse>.Fail("Thành viên không tồn tại.", 404);

            // Check quyền: 
            // 1. Nếu member này chưa có UserId (mồ côi/phụ thuộc) -> Ai giữ MemberId đều xem được (Logic public)
            // 2. Nếu member này đã có UserId -> Phải là chính chủ hoặc Owner family mới xem được.
            // (Ở đây mình demo logic đơn giản nhất: check tồn tại)

            // LOGIC TÁI TẠO MÃ:
            // Nếu IdentityCode đang null (do đã join family rồi bị xóa code, hoặc chưa có), thì sinh mã mới.
            if (string.IsNullOrEmpty(member.IdentityCode))
            {
                member.IdentityCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                _unitOfWork.Repository<Members>().Update(member);
                await _unitOfWork.CompleteAsync();
            }

            // Tạo ảnh QR từ IdentityCode (chỉ xử lý trên RAM, không lưu DB)
            var qrBase64 = GenerateQrCode(member.IdentityCode);

            return ApiResponse<MemberQrResponse>.Ok(new MemberQrResponse
            {
                MemberId = member.MemberId,
                FullName = member.FullName,
                IdentityCode = member.IdentityCode,
                QrCodeBase64 = qrBase64
            }, "Lấy mã QR thành công.");
        }

        // ... Helper GenerateQrCode giữ nguyên ...

        public async Task<ApiResponse<bool>> JoinFamilyUnifiedAsync(Guid? userId, JoinFamilyRequest request)
        {
            // 1. Tìm Family theo Code
            var family = (await _unitOfWork.Repository<Families>()
                .FindAsync(f => f.JoinCode == request.JoinCode)).FirstOrDefault();

            if (family == null) return ApiResponse<bool>.Fail("Mã gia đình không chính xác.", 404);
            if (family.Type == FamilyType.Personal) return ApiResponse<bool>.Fail("Không thể tham gia hồ sơ cá nhân.", 403);

            // --- TRƯỜNG HỢP A: NGƯỜI DÙNG ĐÃ ĐĂNG NHẬP (CÓ USER ID) ---
            if (userId.HasValue)
            {
                // Kiểm tra đã trong nhóm chưa
                var exists = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == family.FamilyId && m.UserId == userId.Value)).Any();

                if (exists) return ApiResponse<bool>.Ok(true, "Bạn đã là thành viên của gia đình này.");

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
                    Role = "Member",
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

                if (member == null) return ApiResponse<bool>.Fail("Hồ sơ thành viên không tồn tại.", 404);

                // Nếu đã có nhà rồi thì thôi (trừ khi chính là nhà này)
                if (member.FamilyId != null)
                {
                    if (member.FamilyId == family.FamilyId) return ApiResponse<bool>.Ok(true, "Đã ở trong gia đình này.");
                    return ApiResponse<bool>.Fail("Hồ sơ này đã thuộc về gia đình khác.", 409);
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


    }
}