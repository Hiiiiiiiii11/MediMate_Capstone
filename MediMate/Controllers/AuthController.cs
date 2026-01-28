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
            var result = await _authenticationService.RegisterAsync(request);
            if (!result.Success)
            {
                return StatusCode(result.Code, result); // Trả về lỗi 400/409 kèm message
            }
            return Ok(result); // Trả về 200 kèm data
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authenticationService.LoginAsync(request);
            if (!result.Success)
            {
                return StatusCode(result.Code, result);
            }
            return Ok(result);
        }
    }
}