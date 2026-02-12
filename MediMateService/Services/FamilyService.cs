using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using static MediMateRepository.Model.Families;

namespace MediMateService.Services
{
    public interface IFamilyService
    {
        // Chế độ 1: Tạo quản lý cá nhân
        Task<ApiResponse<FamilyResponse>> CreatePersonalFamilyAsync(Guid userId);

        // Chế độ 2: Tạo quản lý gia đình
        Task<ApiResponse<FamilyResponse>> CreateSharedFamilyAsync(Guid userId, CreateSharedFamilyRequest request);

        // Lấy danh sách
        Task<ApiResponse<IEnumerable<FamilyResponse>>> GetMyFamiliesAsync(Guid userId);
        // bổ dung member vào family
        Task<ApiResponse<FamilyResponse>> GetFamilyByIdAsync(Guid familyId, Guid userId);
        Task<ApiResponse<FamilyResponse>> UpdateFamilyAsync(Guid familyId, Guid userId, UpdateFamilyRequest request);
        Task<ApiResponse<bool>> DeleteFamilyAsync(Guid familyId, Guid userId);
    }

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
            if (user == null) return ApiResponse<FamilyResponse>.Fail("User not found", 404);

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
                DateOfBirth = user.DateOfBirth ?? DateTime.UtcNow,
                Gender = user.Gender ?? "Other",
                AvatarUrl = user.AvatarUrl,
                Role = "Owner",
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
            if (user == null) return ApiResponse<FamilyResponse>.Fail("User not found", 404);

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
                DateOfBirth = user.DateOfBirth ?? DateTime.UtcNow,
                Gender = user.Gender ?? "Other",
                AvatarUrl = user.AvatarUrl,
                Role = "Owner",
                IsActive = true
            };

            await _unitOfWork.Repository<Families>().AddAsync(family);
            await _unitOfWork.Repository<Members>().AddAsync(member);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<FamilyResponse>.Ok(MapToResponse(family, 1));
        }

        public async Task<ApiResponse<IEnumerable<FamilyResponse>>> GetMyFamiliesAsync(Guid userId)
        {
            // Lấy danh sách Family mà user tham gia
            var members = await _unitOfWork.Repository<Members>().FindAsync(m => m.UserId == userId);
            var result = new List<FamilyResponse>();

            foreach (var mem in members)
            {
                var family = await _unitOfWork.Repository<Families>().GetByIdAsync(mem.FamilyId);
                if (family != null)
                {
                    // Đếm số lượng thành viên thực tế (Optional)
                    var count = (await _unitOfWork.Repository<Members>()
                        .FindAsync(m => m.FamilyId == family.FamilyId)).Count();

                    result.Add(MapToResponse(family, count));
                }
            }
            return ApiResponse<IEnumerable<FamilyResponse>>.Ok(result);
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
                MemberCount = count,
                CreatedAt = family.CreatedAt
            };
        }

        public async Task<ApiResponse<FamilyResponse>> GetFamilyByIdAsync(Guid familyId, Guid userId)
        {
            var family = await _unitOfWork.Repository<Families>().GetByIdAsync(familyId);
            if (family == null) return ApiResponse<FamilyResponse>.Fail("Gia đình không tồn tại.", 404);

            // Kiểm tra xem User có phải là thành viên của gia đình này không
            var isMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId && m.UserId == userId)).Any();

            if (!isMember) return ApiResponse<FamilyResponse>.Fail("Bạn không có quyền xem gia đình này.", 403);

            // Đếm số thành viên
            var count = (await _unitOfWork.Repository<Members>().FindAsync(m => m.FamilyId == familyId)).Count();

            return ApiResponse<FamilyResponse>.Ok(MapToResponse(family, count));
        }

        public async Task<ApiResponse<FamilyResponse>> UpdateFamilyAsync(Guid familyId, Guid userId, UpdateFamilyRequest request)
        {
            var family = await _unitOfWork.Repository<Families>().GetByIdAsync(familyId);
            if (family == null) return ApiResponse<FamilyResponse>.Fail("Family not found", 404);

            // Chỉ người tạo (Owner) mới được sửa
            if (family.CreateBy != userId) return ApiResponse<FamilyResponse>.Fail("Bạn không có quyền chỉnh sửa.", 403);

            family.FamilyName = request.FamilyName;
            _unitOfWork.Repository<Families>().Update(family);
            await _unitOfWork.CompleteAsync();

            return await GetFamilyByIdAsync(familyId, userId);
        }

        public async Task<ApiResponse<bool>> DeleteFamilyAsync(Guid familyId, Guid userId)
        {
            var family = await _unitOfWork.Repository<Families>().GetByIdAsync(familyId);
            if (family == null) return ApiResponse<bool>.Fail("Family not found", 404);

            if (family.CreateBy != userId) return ApiResponse<bool>.Fail("Chỉ chủ gia đình mới được xóa.", 403);

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