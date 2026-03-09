using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using Share.Constants;
using static MediMateRepository.Model.Families;

namespace MediMateService.Services.Implementations
{
    public class FamilyService : IFamilyService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FamilyService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // --- LOGIC 1: CHẾ ĐỘ CÁ NHÂN ---
        public async Task<ApiResponse<FamilyResponse>> CreatePersonalFamilyAsync(Guid userId)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<FamilyResponse>.Fail("User not found", 404);
            }

            // 1. Tạo Family với Type = Personal
            var family = new Families
            {
                FamilyId = Guid.NewGuid(),
                FamilyName = $"Hồ sơ cá nhân của {user.FullName}", // Tên tự động
                CreateBy = userId,
                Type = FamilyType.Personal, // Đánh dấu là cá nhân
                JoinCode = GenerateJoinCode(),
                CreatedAt = DateTime.Now
            };

            // 2. Tạo Member (Copy info từ User)
            var member = new Members
            {
                MemberId = Guid.NewGuid(),
                FamilyId = family.FamilyId,
                UserId = userId,
                FullName = user.FullName,
                DateOfBirth = user.DateOfBirth ?? DateTime.Now,
                Gender = user.Gender ?? "Other",
                AvatarUrl = user.AvatarUrl,
                Role = Roles.Owner,
                IsActive = true
            };

            await _unitOfWork.Repository<Families>().AddAsync(family);
            await _unitOfWork.Repository<Members>().AddAsync(member);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<FamilyResponse>.Ok(MapToResponse(family, 1));
        }

        // --- LOGIC 2: CHẾ ĐỘ GIA ĐÌNH ---
        public async Task<ApiResponse<FamilyResponse>> CreateSharedFamilyAsync(Guid userId, CreateSharedFamilyRequest request)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<FamilyResponse>.Fail("User not found", 404);
            }

            // 1. Tạo Family với Type = Shared
            var family = new Families
            {
                FamilyId = Guid.NewGuid(),
                FamilyName = request.FamilyName, // Tên do user nhập
                CreateBy = userId,
                Type = FamilyType.Shared, // Đánh dấu là gia đình
                JoinCode = GenerateJoinCode(),
                CreatedAt = DateTime.Now
            };

            // 2. Tạo Member (Người tạo là Owner)
            var member = new Members
            {
                MemberId = Guid.NewGuid(),
                FamilyId = family.FamilyId,
                UserId = userId,
                FullName = user.FullName, // Mặc định lấy tên user, sau này có thể sửa biệt danh trong gia đình
                DateOfBirth = user.DateOfBirth ?? DateTime.Now,
                Gender = user.Gender ?? "Other",
                AvatarUrl = user.AvatarUrl,
                Role = Roles.Owner,
                IsActive = true
            };

            await _unitOfWork.Repository<Families>().AddAsync(family);
            await _unitOfWork.Repository<Members>().AddAsync(member);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<FamilyResponse>.Ok(MapToResponse(family, 1));
        }

        public async Task<ApiResponse<IEnumerable<FamilyResponse>>> GetMyFamiliesAsync(Guid callerId)
        {
            IEnumerable<Members> membersList;

            // 1. Thử tìm theo UserId (Trường hợp là User/Bố Mẹ)
            // Một User có thể tham gia nhiều Family -> List Members
            membersList = await _unitOfWork.Repository<Members>().FindAsync(m => m.UserId == callerId);

            // 2. Nếu không thấy (hoặc list rỗng), thử tìm theo MemberId (Trường hợp là Dependent)
            if (!membersList.Any())
            {
                var member = await _unitOfWork.Repository<Members>().GetByIdAsync(callerId);
                if (member != null)
                {
                    membersList = new List<Members> { member };
                }
            }

            // Nếu vẫn không thấy ai -> Trả về rỗng
            if (membersList == null || !membersList.Any())
            {
                return ApiResponse<IEnumerable<FamilyResponse>>.Ok(new List<FamilyResponse>(), "Không tìm thấy gia đình nào.");
            }

            var result = new List<FamilyResponse>();

            // 3. Duyệt danh sách để lấy thông tin Family
            foreach (var mem in membersList)
            {
                if (mem.FamilyId.HasValue) // Chỉ xử lý nếu đã join family
                {
                    var family = await _unitOfWork.Repository<Families>().GetByIdAsync(mem.FamilyId.Value);
                    if (family != null)
                    {
                        // Đếm số thành viên
                        var count = (await _unitOfWork.Repository<Members>()
                            .FindAsync(m => m.FamilyId == family.FamilyId)).Count();

                        result.Add(new FamilyResponse
                        {
                            FamilyId = family.FamilyId,
                            FamilyName = family.FamilyName,
                            Type = family.Type.ToString(),
                            JoinCode = family.JoinCode,
                            IsOpenJoin = family.IsOpenJoin,
                            MemberCount = count,
                            CreatedAt = family.CreatedAt
                        });
                    }
                }
            }

            return ApiResponse<IEnumerable<FamilyResponse>>.Ok(result.DistinctBy(f => f.FamilyId));
            // DistinctBy để tránh trường hợp 1 user có 2 member record trong cùng 1 family (lỗi data)
        }

        // Helper functions
        private FamilyResponse MapToResponse(Families family, int count)
        {
            return new FamilyResponse
            {
                FamilyId = family.FamilyId,
                FamilyName = family.FamilyName,
                Type = family.Type.ToString(), // Trả về "Personal" hoặc "Shared"
                JoinCode = family.JoinCode,
                IsOpenJoin = family.IsOpenJoin,
                MemberCount = count,
                CreatedAt = family.CreatedAt
            };
        }

        public async Task<ApiResponse<FamilyResponse>> GetFamilyByIdAsync(Guid familyId, Guid userId)
        {
            var family = await _unitOfWork.Repository<Families>().GetByIdAsync(familyId);
            if (family == null)
            {
                return ApiResponse<FamilyResponse>.Fail("Gia đình không tồn tại.", 404);
            }

            // Kiểm tra xem User có phải là thành viên của gia đình này không
            var isMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId && m.UserId == userId)).Any();

            if (!isMember)
            {
                return ApiResponse<FamilyResponse>.Fail("Bạn không có quyền xem gia đình này.", 403);
            }

            // Đếm số thành viên
            var count = (await _unitOfWork.Repository<Members>().FindAsync(m => m.FamilyId == familyId)).Count();

            return ApiResponse<FamilyResponse>.Ok(MapToResponse(family, count));
        }

        public async Task<ApiResponse<FamilyResponse>> UpdateFamilyAsync(Guid familyId, Guid userId, UpdateFamilyRequest request)
        {
            var family = await _unitOfWork.Repository<Families>().GetByIdAsync(familyId);

            if (family == null)
            {
                return ApiResponse<FamilyResponse>.Fail("Gia đình không tồn tại.", 404);
            }

            // Kiểm tra quyền: Chỉ người tạo (CreateBy) hoặc Owner mới được sửa
            // (Nếu logic của bạn dùng bảng Members để phân quyền Owner thì query bảng Members ở đây)
            if (family.CreateBy != userId)
            {
                return ApiResponse<FamilyResponse>.Fail("Bạn không có quyền chỉnh sửa thông tin gia đình này.", 403);
            }

            // 1. Cập nhật Tên (nếu có gửi lên)
            if (!string.IsNullOrEmpty(request.FamilyName))
            {
                family.FamilyName = request.FamilyName;
            }

            // 2. Cập nhật Trạng thái Mở/Đóng Join Code (nếu có gửi lên)
            // .HasValue kiểm tra xem client có gửi trường này không
            if (request.IsOpenJoin.HasValue)
            {
                family.IsOpenJoin = request.IsOpenJoin.Value;
            }

            _unitOfWork.Repository<Families>().Update(family);
            await _unitOfWork.CompleteAsync();

            // Trả về thông tin mới nhất
            return await GetFamilyByIdAsync(familyId, userId);
        }

        public async Task<ApiResponse<bool>> DeleteFamilyAsync(Guid familyId, Guid userId)
        {
            var family = await _unitOfWork.Repository<Families>().GetByIdAsync(familyId);
            if (family == null)
            {
                return ApiResponse<bool>.Fail("Family not found", 404);
            }

            if (family.CreateBy != userId)
            {
                return ApiResponse<bool>.Fail("Chỉ chủ gia đình mới được xóa.", 403);
            }

            // Soft Delete: Xóa Family thì giải phóng tất cả thành viên (Set FamilyId = null)
            // Hoặc xóa hẳn bản ghi tùy nghiệp vụ của bạn. Ở đây tôi chọn giải phóng thành viên.
            var members = await _unitOfWork.Repository<Members>().FindAsync(m => m.FamilyId == familyId);
            foreach (var member in members)
            {
                member.FamilyId = null; // Mồ côi
                _unitOfWork.Repository<Members>().Update(member);
            }

            _unitOfWork.Repository<Families>().Remove(family); // Xóa Family
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Đã giải tán gia đình thành công.");
        }

        private string GenerateJoinCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 6).Select(s => s[new Random().Next(s.Length)]).ToArray());
        }
    }
}