using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;

namespace MediMateApi.Controllers
{
    [Route("api/notification-settings")]
    [ApiController]
    [Authorize]
    public class NotificationSettingController : ControllerBase
    {
        private readonly INotificationSettingService _settingService;
        private readonly ICurrentUserService _currentUserService;

        public NotificationSettingController(INotificationSettingService settingService, ICurrentUserService currentUserService)
        {
            _settingService = settingService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Xem cấu hình thông báo của một thành viên
        /// </summary>
        [HttpGet("members/{memberId}")]
        public async Task<IActionResult> GetSetting(Guid memberId)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                var result = await _settingService.GetSettingByMemberIdAsync(memberId, currentUserId);

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

            /// <summary>
            /// Cập nhật cấu hình thông báo (Chỉ cần gửi những trường muốn đổi)
            /// </summary>
            [HttpPut("members/{memberId}")]
        public async Task<IActionResult> UpdateSetting(Guid memberId, [FromBody] UpdateNotificationSettingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ.", 400));
                }

                var currentUserId = _currentUserService.UserId;
                var result = await _settingService.UpdateSettingAsync(memberId, currentUserId, request);

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