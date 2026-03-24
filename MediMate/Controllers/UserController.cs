using MediMateRepository.Model;
using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;


namespace MediMate.Controllers
{
    [Route("api/v1/users")]
    [ApiController]
    
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;

        public UserController(IUserService userService,ICurrentUserService currentUserService )
        {
            _userService = userService;
            _currentUserService = currentUserService;
        }


        [HttpGet]
        // [Authorize(Roles = Roles.Admin)] // Bỏ comment nếu muốn chỉ Admin mới xem được
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserProfileResponse>>), 200)]
        public async Task<IActionResult> GetAllUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _userService.GetAllUsersAsync(pageNumber, pageSize);

                // Luôn trả về 200 OK kèm data (kể cả list rỗng)
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log lỗi ex ở đây nếu cần
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), 200)]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _userService.GetProfileAsync(userId);

                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log lỗi ex ở đây nếu cần
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [Authorize]
        [HttpPut("me")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), 200)]
        public async Task<IActionResult> UpdateMyProfile([FromForm] UpdateProfileRequest request)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _userService.UpdateProfileAsync(userId, request);

                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log lỗi ex ở đây nếu cần
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [Authorize]
        [HttpPut("me/change-password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _userService.ChangePasswordAsync(userId, request);

                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log lỗi ex ở đây nếu cần
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [Authorize]
        [HttpPut("admin/deactivate")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> DeactivateMyAccount(Guid userId)
        {
            try
            {
                // Lấy ID từ Token (thông qua service hoặc helper cũ)
                //var userId = _currentUserService.UserId;
                var result = await _userService.DeactivateUserAsync(userId);

                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [Authorize]
        [HttpPut("admin/activate")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> ActivateMyAccount(Guid userId)
        {
            try
            {
                // Lấy ID từ Token (thông qua service hoặc helper cũ)
                //var userId = _currentUserService.UserId;
                var result = await _userService.ActivateUserAsync(userId);

                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // DELETE: api/v1/users/me
        [Authorize]
        [HttpDelete("me")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> DeleteMyAccount([FromBody] DeleteAccountRequest request)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _userService.DeleteUserAsync(userId, request);

                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}