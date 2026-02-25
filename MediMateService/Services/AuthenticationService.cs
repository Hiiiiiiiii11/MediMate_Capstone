using MediMateService.DTOs;
using Share.Common;

namespace MediMateService.Services
{
    public interface IAuthenticationService
    {
        Task<ApiResponse<AutheticationResponse>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<AutheticationResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<string>> LoginDependentByQrAsync(DependentQrLoginRequest request);
    }
}
