using MediMateService.DTOs;
using Share.Common;

namespace MediMateService.Services
{
    public interface IAuthenticationService
    {
        Task<ApiResponse<AutheticationResponse>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<AutheticationResponse>> LoginUserAsync(LoginRequest request);
        Task<ApiResponse<AutheticationResponse>> LoginRemainingAsync(LoginRequest request);  
    }
}
