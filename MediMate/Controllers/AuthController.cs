using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using System.Security.Claims;

namespace MediMate.Controllers
{
    [Route("api/v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;

        public AuthController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authenticationService.RegisterAsync(request);
                if (!result.Success)
                {
                    return StatusCode(result.Code, result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
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
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
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
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [HttpPost("login-dependent")]
        public async Task<IActionResult> LoginDependent([FromBody] DependentQrLoginRequest request)
        {
            try
            {
                var result = await _authenticationService.LoginDependentByQrAsync(request);

                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result); // Trả về JWT Token
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [Authorize] 
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // 1. Tìm ID của người dùng từ trong JWT Token
            // Hỗ trợ cả 3 loại key claim: NameIdentifier, "Id" (của User), "MemberId" (của Dependent)
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("Id")?.Value
                       ?? User.FindFirst("MemberId")?.Value;

            if (idClaim == null || !Guid.TryParse(idClaim, out Guid accountId))
            {
                return Unauthorized(ApiResponse<bool>.Fail("Token không hợp lệ.", 401));
            }

            // 2. Tìm Role để biết là User hay Dependent
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value
                         ?? User.FindFirst("Role")?.Value 
                         ?? "User";

            // 3. Xử lý clear DB
            var result = await _authenticationService.LogoutAsync(accountId, roleClaim);

            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
    }
}