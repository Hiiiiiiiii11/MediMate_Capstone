using MediMateService.DTOs;
using Share.Common;

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
        Task<ApiResponse<FamilySubscriptionResponse>> GetFamilySubscriptionAsync(Guid familyId);
        
        // Admin methods for family subscriptions
        Task<ApiResponse<PagedResult<AdminFamilySubscriptionResponse>>> GetAllFamilySubscriptionsAsync(AdminFamilySubscriptionFilter filter);
        Task<ApiResponse<bool>> UpdateFamilySubscriptionStatusAsync(Guid subscriptionId, string status);
        
        // Hủy gói đăng ký (chỉ chủ hộ), hoàn tiền nếu sử dụng chưa quá 10%
        Task<ApiResponse<bool>> CancelSubscriptionAsync(Guid subscriptionId, Guid userId);

        // Hoàn tất thủ tục hoàn tiền cho gói gia đình (dành cho Admin)
        Task<ApiResponse<bool>> CompleteRefundAsync(Guid subscriptionId, CompleteRefundRequest request);
    }

   
}