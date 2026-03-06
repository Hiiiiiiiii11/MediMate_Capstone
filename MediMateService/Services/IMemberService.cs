using MediMateService.DTOs; // Nhớ cài package QRCoder
using Share.Common;

namespace MediMateService.Services
{
    public interface IMemberService
    {
        Task<ApiResponse<IEnumerable<MemberResponse>>> GetAllMember();
        Task<ApiResponse<InitDependentResponse>> InitDependentProfileAsync(InitDependentRequest request, Guid? currentUserId = null);
        Task<ApiResponse<IEnumerable<MemberResponse>>> GetMembersByFamilyIdAsync(Guid familyId, Guid userId);

        // Get Member Detail
        Task<ApiResponse<MemberResponse>> GetMemberByIdAsync(Guid memberId, Guid userId);

        // Update Member Info
        Task<ApiResponse<MemberResponse>> UpdateMemberAsync(Guid memberId, Guid userId, UpdateMemberRequest request);

        // Delete/Remove Member (Soft delete or Remove from family)
        Task<ApiResponse<bool>> RemoveMemberAsync(Guid memberId, Guid userId);
        Task<ApiResponse<bool>> DeleteMemberAsync(Guid memberId, Guid userId);
        Task<ApiResponse<MemberQrResponse>> GenerateLoginQrForDependentAsync(Guid memberId, Guid currentUserId);
        Task<ApiResponse<MemberResponse>> AddUserMemberToFamilyAsync(AddUserMemberRequest request, Guid ownerUserId);
        Task<ApiResponse<MemberResponse>> CreateDependentMemberAsync(CreateDependentRequest request, Guid ownerUserId);
        Task<ApiResponse<bool>> JoinFamilyByJoinCodeAsync(JoinFamilyByCodeRequest request, Guid userId);
    }


}