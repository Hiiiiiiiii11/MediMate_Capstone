using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Mvc;

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
            var result = await _authenticationService.LoginDependentByQrAsync(request);

            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result); // Trả về JWT Token
        }
    }
}