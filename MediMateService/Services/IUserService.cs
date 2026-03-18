using MediMateService.DTOs;
using Share.Common;


namespace MediMateService.Services
{
    public interface IUserService
    {
        Task<ApiResponse<PagedResult<UserProfileResponse>>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10);
        
        // Admin tạo Doctor Manager
        Task<ApiResponse<UserProfileResponse>> CreateDoctorManagerAsync(CreateDoctorManagerDto request);

        // Lấy thông tin cá nhân
        Task<ApiResponse<UserProfileResponse>> GetProfileAsync(Guid userId);

        // Cập nhật thông tin
        Task<ApiResponse<UserProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);

        // Đổi mật khẩu
        Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

        // 1. Khóa tài khoản (Unactive)
        Task<ApiResponse<bool>> DeactivateUserAsync(Guid userId);
        Task<ApiResponse<bool>> ActivateUserAsync(Guid userId);

        // 2. Xóa tài khoản vĩnh viễn
        Task<ApiResponse<bool>> DeleteUserAsync(Guid userId, DeleteAccountRequest request);
    }
}
