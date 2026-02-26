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
    }
}