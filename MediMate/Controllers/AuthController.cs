using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Share.Common;
using Share.Jwt;
using System.Security.Claims;

namespace MediMate.Controllers
{
    [Route("api/v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly JwtSettings _jwtSettings;

        public AuthController(IAuthenticationService authenticationService, IOptions<JwtSettings> jwtSettings)
        {
            _authenticationService = authenticationService;
            _jwtSettings = jwtSettings.Value;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authenticationService.RegisterAsync(request);
                return !result.Success ? StatusCode(result.Code, result) : (IActionResult)Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "L?i h? th?ng: " + ex.Message });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            try
            {
                var result = await _authenticationService.VerifyOtpAsync(request);
                return !result.Success ? StatusCode(result.Code, result) : (IActionResult)Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "L?i h? th?ng: " + ex.Message });
            }
        }

        [HttpPost("login/user")]
        public async Task<IActionResult> LoginUser([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authenticationService.LoginUserAsync(request);
                if (!result.Success)
                {
                    return StatusCode(result.Code, result);
                }

                var token = result.Data?.AccessToken;
                SetAuthCookie(token);

                return Ok(ApiResponse<object>.Ok(null, "Đăng nhập thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "L?i h? th?ng: " + ex.Message });
            }
        }

        [HttpPost("login/remain")]
        public async Task<IActionResult> LoginRemain([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authenticationService.LoginRemainingAsync(request);
                if (!result.Success)
                {
                    return StatusCode(result.Code, result);
                }

                var token = result.Data?.AccessToken;
                SetAuthCookie(token);

                return Ok(ApiResponse<object>.Ok(null, "Đăng nhập thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "L?i h? th?ng: " + ex.Message });
            }
        }
        [HttpPost("login-dependent")]
        public async Task<IActionResult> LoginDependent([FromBody] DependentQrLoginRequest request)
        {
            try
            {
                var result = await _authenticationService.LoginDependentByQrAsync(request);

                if (!result.Success)
                {
                    return StatusCode(result.Code, result);
                }

                return Ok(result); // Tr? v? JWT Token
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "L?i h? th?ng: " + ex.Message });
            }
        }
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // 1. T?m ID c?a ng�?i d�ng t? trong JWT Token
            // H? tr? c? 3 lo?i key claim: NameIdentifier, "Id" (c?a User), "MemberId" (c?a Dependent)
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("Id")?.Value
                       ?? User.FindFirst("MemberId")?.Value;

            if (idClaim == null || !Guid.TryParse(idClaim, out Guid accountId))
            {
                return Unauthorized(ApiResponse<bool>.Fail("Token kh�ng h?p l?.", 401));
            }

            // 2. T?m Role �? bi?t l� User hay Dependent
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value
                         ?? User.FindFirst("Role")?.Value
                         ?? "User";

            // 3. X? l? clear DB
            var result = await _authenticationService.LogoutAsync(accountId, roleClaim);

            return !result.Success ? StatusCode(result.Code, result) : (IActionResult)Ok(result);
        }

        private void SetAuthCookie(string token)
        {
            Response.Cookies.Append("token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours)
            });
        }
    }
}