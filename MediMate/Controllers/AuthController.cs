using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Share.Common;
using Share.Jwt;
using System.IdentityModel.Tokens.Jwt;
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
        [ProducesResponseType(typeof(ApiResponse<AutheticationResponse>), 200)]
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
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            try
            {
                var result = await _authenticationService.VerifyOtpAsync(request);
                if (!result.Success)
                {
                    return StatusCode(result.Code, result);
                }

                var token = result.Data?.AccessToken;
                SetAuthCookie(token);

                return Ok(ApiResponse<object>.Ok(new { token }, "Kích hoạt và đăng nhập thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost("login/user")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
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

                return Ok(ApiResponse<object>.Ok(new { token }, "Đăng nhập thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "L?i h? th?ng: " + ex.Message });
            }
        }

        [HttpPost("login/remain")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
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

                return Ok(ApiResponse<object>.Ok(new { token }, "Đăng nhập thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "L?i h? th?ng: " + ex.Message });
            }
        }
        [HttpPost("login-dependent")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> LoginDependent([FromBody] DependentQrLoginRequest request)
        {
            try
            {
                var result = await _authenticationService.LoginDependentByQrAsync(request);

                if (!result.Success)
                {
                    return StatusCode(result.Code, result);
                }
                var token = result.Data?.AccessToken ?? "";
                SetAuthCookie(token);
                return Ok(ApiResponse<object>.Ok(new { token }, "Đăng nhập thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "L?i h? th?ng: " + ex.Message });
            }
        }
        [AllowAnonymous] // Cho phép gọi kể cả khi không có token hoặc token lỗi
        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> Logout()
        {
            // 1. Lấy token từ Header hoặc Cookie
            string token = Request.Headers["Authorization"].ToString()?.Replace("Bearer ", "")?.Trim() ?? "";
            if (string.IsNullOrEmpty(token))
            {
                token = Request.Cookies["token"]?.Trim() ?? "";
            }

            Guid? accountId = null;
            string roleClaim = "User";

            // 2. Cố gắng lấy AccountId để xóa FCM Token
            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    try
                    {
                        // Tự giải mã token bỏ qua việc token đã hết hạn hay chưa
                        var jwtToken = handler.ReadJwtToken(token);
                        var idClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "Id" || c.Type == "MemberId")?.Value;

                        if (Guid.TryParse(idClaim, out Guid parsedId))
                        {
                            accountId = parsedId;
                        }

                        roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "Role")?.Value ?? "User";
                    }
                    catch
                    {
                        // Nếu cấu trúc token bị lỗi nặng, bỏ qua việc lấy AccountId
                    }
                }
            }

            // 3. Gọi Service xử lý (DB & Blacklist)
            var result = await _authenticationService.LogoutAsync(accountId, roleClaim, token);

            // 4. LUÔN LUÔN xóa Cookie trên trình duyệt/thiết bị dù token có lỗi hay không
            Response.Cookies.Delete("token", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });

            return Ok(result); // Luôn trả về 200 OK để Frontend dọn dẹp data
        }

        private void SetAuthCookie(string token)
        {
            Response.Cookies.Append("token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTime.Now.AddHours(_jwtSettings.ExpirationHours)
            });
        }
    }
}