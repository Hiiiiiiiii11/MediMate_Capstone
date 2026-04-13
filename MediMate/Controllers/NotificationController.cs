using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MediMate.Controllers
{
    [Route("api/v1/notifications")]
    [ApiController]
    [Authorize] // Bắt buộc đăng nhập mới xem được thông báo
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;

        public NotificationController(INotificationService notificationService, ICurrentUserService currentUserService)
        {
            _notificationService = notificationService;
            _currentUserService = currentUserService;
        }

        // 1. Lấy danh sách thông báo của User đang đăng nhập
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<NotificationDto>>), 200)]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = _currentUserService.UserId;
            var result = await _notificationService.GetUserNotificationsAsync(userId);

            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }



        // 2. Đánh dấu 1 thông báo là đã đọc
        [HttpPut("{notificationId}/read")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            var userId = _currentUserService.UserId;
            var result = await _notificationService.MarkAsReadAsync(notificationId, userId);

            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }

        // 3. Đánh dấu tất cả là đã đọc
        [HttpPut("read-all")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _currentUserService.UserId;
            var result = await _notificationService.MarkAllAsReadAsync(userId);

            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }


        [HttpGet("member/{memberId}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<NotificationDto>>), 200)]
        public async Task<IActionResult> GetMemberNotifications(Guid memberId)
        {
            // Ép cứng truyền vào tham số memberId, tham số userId = null
            var result = await _notificationService.GetUserNotificationsAsync(userId: null, memberId: memberId);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }

        [HttpPut("member/{memberId}/{notificationId}/read")]
        public async Task<IActionResult> MarkMemberAsRead(Guid memberId, Guid notificationId)
        {
            var result = await _notificationService.MarkAsReadAsync(notificationId, userId: null, memberId: memberId);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }

        [HttpPut("member/{memberId}/read-all")]
        public async Task<IActionResult> MarkAllMemberAsRead(Guid memberId)
        {
            var result = await _notificationService.MarkAllAsReadAsync(userId: null, memberId: memberId);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }


    }
}