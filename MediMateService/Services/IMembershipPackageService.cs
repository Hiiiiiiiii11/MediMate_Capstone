using MediMateService.DTOs;
using Share.Common;

namespace MediMateService.Services
{
    public interface IMembershipPackageService
    {
        Task<ApiResponse<List<MembershipPackageDto>>> GetAllAsync();
        Task<ApiResponse<MembershipPackageDto>> GetByIdAsync(Guid packageId);
        Task<ApiResponse<MembershipPackageDto>> CreateAsync(CreateMembershipPackageDto dto);
        Task<ApiResponse<MembershipPackageDto>> UpdateAsync(Guid packageId, UpdateMembershipPackageDto dto);
        Task<ApiResponse<bool>> DeleteAsync(Guid packageId);
    }
}
