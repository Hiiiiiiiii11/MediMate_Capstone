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
                    return StatusCode(result.Code, result); // Trả về lỗi 400/409 kèm message
                }
                return Ok(result); // Trả về 200 kèm data
            }
            catch (Exception ex)
            {
                // Log lỗi ex ở đây nếu cần
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authenticationService.LoginAsync(request);
                if (!result.Success)
                {
                    return StatusCode(result.Code, result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log lỗi ex ở đây nếu cần
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
            
        }
    }
}