using MediMateRepository.Model;
using MediMateRepository.Repositories;// Chứa ApiResponse
using MediMateService.DTOs;
using Share.Common;

namespace MediMateService.Services.Implementations
{
    public class HealthService : IHealthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public HealthService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<HealthProfileResponse>> GetHealthProfileAsync(Guid memberId, Guid userId)
        {
            // 1. Validate quyền truy cập (Check xem User có quyền xem Member này không)
            if (!await _currentUserService.CheckAccess(memberId, userId))
            {
                return ApiResponse<HealthProfileResponse>.Fail("Không có quyền truy cập.", 403);
            }

            // 2. Lấy Profile (Include Conditions)
            // Lưu ý: GenericRepository cần hỗ trợ Include. Nếu chưa, dùng code thuần hoặc thêm param include
            var profile = (await _unitOfWork.Repository<HealthProfiles>()
                .FindAsync(p => p.MemberId == memberId, includeProperties: "Conditions")).FirstOrDefault();

            if (profile == null)
            {
                // Nếu chưa có thì trả về object rỗng hoặc tạo mới tùy logic
                return ApiResponse<HealthProfileResponse>.Fail("Chưa có hồ sơ sức khỏe.", 404);
            }

            return ApiResponse<HealthProfileResponse>.Ok(MapToResponse(profile));
        }
        public async Task<ApiResponse<HealthProfileResponse>> CreateHealthProfileAsync(Guid memberId, Guid userId, CreateHealthProfileRequest request)
        {
            // 1. Check quyền truy cập (Dùng lại hàm CheckAccess đã viết)
            if (!await _currentUserService.CheckAccess(memberId, userId))
            {
                return ApiResponse<HealthProfileResponse>.Fail("Bạn không có quyền tạo hồ sơ cho thành viên này.", 403);
            }

            // 2. Kiểm tra xem đã có hồ sơ chưa (Quan hệ 1-1)
            var existingProfile = (await _unitOfWork.Repository<HealthProfiles>()
                .FindAsync(p => p.MemberId == memberId)).FirstOrDefault();

            if (existingProfile != null)
            {
                return ApiResponse<HealthProfileResponse>.Fail("Thành viên này đã có hồ sơ sức khỏe. Vui lòng sử dụng chức năng cập nhật.", 409);
            }

            // 3. Tạo mới
            var newProfile = new HealthProfiles
            {
                HealthProfileId = Guid.NewGuid(),
                MemberId = memberId,
                BloodType = request.BloodType ?? string.Empty,
                Height = request.Height,
                Weight = request.Weight,
                InsuranceNumber = request.InsuranceNumber ?? string.Empty,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<HealthProfiles>().AddAsync(newProfile);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<HealthProfileResponse>.Ok(MapToResponse(newProfile), "Tạo hồ sơ sức khỏe thành công.");
        }
        public async Task<ApiResponse<HealthProfileResponse>> UpdateHealthProfileAsync(Guid memberId, Guid userId, UpdateHealthProfileRequest request)
        {
            if (!await _currentUserService.CheckAccess(memberId, userId))
            {
                return ApiResponse<HealthProfileResponse>.Fail("Access Denied", 403);
            }

            var profile = (await _unitOfWork.Repository<HealthProfiles>()
                .FindAsync(p => p.MemberId == memberId)).FirstOrDefault();

            // Nếu chưa có profile thì TẠO MỚI
            if (profile == null)
            {
                return ApiResponse<HealthProfileResponse>.Fail(" Hồ sơ sức khỏe không tồn tại.", 404);
            }

            // Cập nhật dữ liệu
            if (!string.IsNullOrEmpty(request.BloodType))
            {
                profile.BloodType = request.BloodType;
            }

            if (!string.IsNullOrEmpty(request.InsuranceNumber))
            {
                profile.InsuranceNumber = request.InsuranceNumber;
            }

            if (request.Height > 0)
            {
                profile.Height = request.Height;
            }

            if (request.Weight > 0)
            {
                profile.Weight = request.Weight;
            }

            profile.UpdatedAt = DateTime.Now;

            _unitOfWork.Repository<HealthProfiles>().Update(profile);
            await _unitOfWork.CompleteAsync();

            return await GetHealthProfileAsync(memberId, userId);
        }

        public async Task<ApiResponse<bool>> AddConditionAsync(Guid memberId, Guid userId, AddConditionRequest request)
        {
            if (!await _currentUserService.CheckAccess(memberId, userId))
            {
                return ApiResponse<bool>.Fail("Access Denied", 403);
            }

            // Phải đảm bảo Profile đã tồn tại
            var profile = (await _unitOfWork.Repository<HealthProfiles>().FindAsync(p => p.MemberId == memberId)).FirstOrDefault();
            if (profile == null)
            {
                return ApiResponse<bool>.Fail("Vui lòng cập nhật thông tin cơ bản trước khi thêm bệnh lý.", 400);
            }

            var condition = new HealthConditions
            {
                ConditionId = Guid.NewGuid(),
                HealthProfileId = profile.HealthProfileId,
                ConditionName = request.ConditionName,
                Description = request.Description,
                DiagnosedDate = request.DiagnosedDate,
                Status = request.Status
            };

            await _unitOfWork.Repository<HealthConditions>().AddAsync(condition);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Thêm tình trạng bệnh thành công.");
        }

        public async Task<ApiResponse<bool>> RemoveConditionAsync(Guid conditionId, Guid userId)
        {
            var condition = await _unitOfWork.Repository<HealthConditions>().GetByIdAsync(conditionId);
            if (condition == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy dữ liệu.", 404);
            }

            // Truy ngược lại để check quyền (Condition -> Profile -> Member -> Family -> User)
            var profile = await _unitOfWork.Repository<HealthProfiles>().GetByIdAsync(condition.HealthProfileId);
            if (!await _currentUserService.CheckAccess(profile.MemberId, userId))
            {
                return ApiResponse<bool>.Fail("Access Denied", 403);
            }

            _unitOfWork.Repository<HealthConditions>().Remove(condition);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Đã xóa.");
        }

        // Helper: Map Entity -> Response
        private HealthProfileResponse MapToResponse(HealthProfiles p)
        {
            return new HealthProfileResponse
            {
                HealthProfileId = p.HealthProfileId,
                MemberId = p.MemberId,
                BloodType = p.BloodType,
                Height = p.Height,
                Weight = p.Weight,
                InsuranceNumber = p.InsuranceNumber,
                Conditions = p.Conditions.Select(c => new HealthConditionDto
                {
                    ConditionId = c.ConditionId,
                    ConditionName = c.ConditionName,
                    Description = c.Description,
                    DiagnosedDate = c.DiagnosedDate,
                    Status = c.Status
                }).ToList()
            };
        }

        public async Task<ApiResponse<IEnumerable<FamilyHealthSummaryResponse>>> GetHealthProfilesByFamilyIdAsync(Guid familyId, Guid userId)
        {
            // 1. Check quyền: User có thuộc Family này không?
            var currentUserMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId && m.UserId == userId)).FirstOrDefault();

            if (currentUserMember == null)
            {
                return ApiResponse<IEnumerable<FamilyHealthSummaryResponse>>.Fail("Bạn không thuộc gia đình này.", 403);
            }

            // 2. Lấy tất cả Member trong Family kèm theo HealthProfile và Conditions
            // Sử dụng includeProperties chuỗi string mà chúng ta đã sửa ở GenericRepository
            var members = await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId, includeProperties: "HealthProfile,HealthProfile.Conditions");

            var result = new List<FamilyHealthSummaryResponse>();

            foreach (var m in members)
            {
                var summary = new FamilyHealthSummaryResponse
                {
                    MemberId = m.MemberId,
                    FullName = m.FullName,
                    AvatarUrl = m.AvatarUrl,
                    HasProfile = m.HealthProfile != null
                };

                if (m.HealthProfile != null)
                {
                    summary.BloodType = m.HealthProfile.BloodType;

                    // Tính BMI
                    if (m.HealthProfile.Height > 0)
                    {
                        double h = m.HealthProfile.Height / 100.0;
                        summary.BMI = Math.Round(m.HealthProfile.Weight / (h * h), 2);
                    }

                    // Lấy danh sách bệnh đang Active
                    var activeConditions = m.HealthProfile.Conditions
                        .Where(c => c.Status == "Active")
                        .ToList();

                    summary.ActiveConditionsCount = activeConditions.Count;
                    summary.ConditionNames = activeConditions.Select(c => c.ConditionName).ToList();
                }

                result.Add(summary);
            }

            return ApiResponse<IEnumerable<FamilyHealthSummaryResponse>>.Ok(result);
        }

        // --- 2. LẤY CHI TIẾT BỆNH ÁN ---
        public async Task<ApiResponse<HealthConditionDto>> GetConditionByIdAsync(Guid conditionId, Guid userId)
        {
            var condition = await _unitOfWork.Repository<HealthConditions>().GetByIdAsync(conditionId);
            if (condition == null)
            {
                return ApiResponse<HealthConditionDto>.Fail("Không tìm thấy thông tin bệnh lý.", 404);
            }

            // Check quyền truy cập ngược từ Condition -> Profile -> Member -> Family
            var profile = await _unitOfWork.Repository<HealthProfiles>().GetByIdAsync(condition.HealthProfileId);
            return !await _currentUserService.CheckAccess(profile.MemberId, userId)
                ? ApiResponse<HealthConditionDto>.Fail("Không có quyền truy cập.", 403)
                : ApiResponse<HealthConditionDto>.Ok(new HealthConditionDto
                {
                    ConditionId = condition.ConditionId,
                    ConditionName = condition.ConditionName,
                    Description = condition.Description,
                    DiagnosedDate = condition.DiagnosedDate,
                    Status = condition.Status
                });
        }

        // --- 3. CẬP NHẬT BỆNH ÁN (Partial Update) ---
        public async Task<ApiResponse<bool>> UpdateConditionAsync(Guid conditionId, Guid userId, UpdateConditionRequest request)
        {
            var condition = await _unitOfWork.Repository<HealthConditions>().GetByIdAsync(conditionId);
            if (condition == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy thông tin bệnh lý.", 404);
            }

            // Check quyền
            var profile = await _unitOfWork.Repository<HealthProfiles>().GetByIdAsync(condition.HealthProfileId);
            if (!await _currentUserService.CheckAccess(profile.MemberId, userId))
            {
                return ApiResponse<bool>.Fail("Không có quyền chỉnh sửa.", 403);
            }

            // --- LOGIC GIỮ GIÁ TRỊ CŨ NẾU NULL ---

            if (!string.IsNullOrEmpty(request.ConditionName))
            {
                condition.ConditionName = request.ConditionName;
            }

            if (!string.IsNullOrEmpty(request.Description))
            {
                condition.Description = request.Description;
            }

            if (request.DiagnosedDate.HasValue)
            {
                condition.DiagnosedDate = request.DiagnosedDate.Value;
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                condition.Status = request.Status;
            }

            _unitOfWork.Repository<HealthConditions>().Update(condition);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Cập nhật bệnh án thành công.");
        }
    }
}