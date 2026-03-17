using MediMateRepository.Model;
using MediMateService.DTOs;
using Share.Common;

namespace MediMateService.Services
{
    public interface IAuthenticationService
    {
        Task<ApiResponse<AutheticationResponse>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<AutheticationResponse>> LoginUserAsync(LoginRequest request);
        Task<ApiResponse<AutheticationResponse>> LoginRemainingAsync(LoginRequest request);  
        Task<ApiResponse<string>> LoginDependentByQrAsync(DependentQrLoginRequest request);
        Task<ApiResponse<AutheticationResponse>> VerifyOtpAsync(VerifyOtpRequest request);
        string GenerateJwtTokenForDependent(Members member, string typeLogin);
        Task<ApiResponse<bool>> LogoutAsync(Guid accountId, string role);
    }
}
