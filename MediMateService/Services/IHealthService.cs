using MediMateService.DTOs;
using Share.Common;

namespace MediMateService.Services
{
    public interface IHealthService
    {
        // Lấy hồ sơ sức khỏe của 1 thành viên
        Task<ApiResponse<HealthProfileResponse>> GetHealthProfileAsync(Guid memberId, Guid userId);
        //create healthprofile
        Task<ApiResponse<HealthProfileResponse>> CreateHealthProfileAsync(Guid memberId, Guid userId, CreateHealthProfileRequest request);

        // Cập nhật thông tin cơ bản (Chiều cao, cân nặng...)
        Task<ApiResponse<HealthProfileResponse>> UpdateHealthProfileAsync(Guid memberId, Guid userId, UpdateHealthProfileRequest request);

        // Thêm tình trạng bệnh
        Task<ApiResponse<bool>> AddConditionAsync(Guid memberId, Guid userId, AddConditionRequest request);

        // Xóa tình trạng bệnh
        Task<ApiResponse<bool>> RemoveConditionAsync(Guid conditionId, Guid userId);
        Task<ApiResponse<IEnumerable<FamilyHealthSummaryResponse>>> GetHealthProfilesByFamilyIdAsync(Guid familyId, Guid userId);

        // 2. Lấy chi tiết 1 bệnh án (để hiển thị lên form sửa)
        Task<ApiResponse<HealthConditionDto>> GetConditionByIdAsync(Guid conditionId, Guid userId);

        // 3. Cập nhật bệnh án (Giữ giá trị cũ nếu không truyền)
        Task<ApiResponse<bool>> UpdateConditionAsync(Guid conditionId, Guid userId, UpdateConditionRequest request);
    }
}