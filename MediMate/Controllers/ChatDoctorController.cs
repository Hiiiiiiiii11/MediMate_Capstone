using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;

namespace MediMate.Controllers
{
    [Route("api/v1/chatdoctor")]
    [ApiController]
    [Authorize]
    public class ChatDoctorController : Controller
    {
        private readonly IChatDoctorService _chatDoctorService;
        private readonly ICurrentUserService _currentUserService;

        public ChatDoctorController(IChatDoctorService chatDoctorService, ICurrentUserService currentUserService)
        {
            _chatDoctorService = chatDoctorService;
            _currentUserService = currentUserService;
        }
        [HttpGet("sessions/{sessionId}/messages")]
        public async Task<IActionResult> GetSessionMessages(Guid sessionId)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;

                // Tự động nhận diện xem người gọi API là Bác sĩ hay Bệnh nhân qua Token Role
                bool isDoctorRequest = User.IsInRole("Doctor");

                var result = await _chatDoctorService.GetSessionMessagesAsync(sessionId, currentUserId, isDoctorRequest);

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

        [HttpPost("sessions/{sessionId}/messages")]
        public async Task<IActionResult> SendMessage(Guid sessionId, [FromForm] SendChatDoctorRequest request)
        {
            try
            {
                // Kiểm tra nội dung rỗng (nếu không có cả text và không có cả file thì báo lỗi)
                if (string.IsNullOrWhiteSpace(request.Content) && request.AttachmentFile == null)
                {
                    return BadRequest(ApiResponse<object>.Fail("Tin nhắn không được để trống.", 400));
                }

                var currentUserId = _currentUserService.UserId;
                bool isDoctorRequest = User.IsInRole("Doctor");

                var result = await _chatDoctorService.SendMessageAsync(sessionId, currentUserId, request, isDoctorRequest);

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
        [HttpPut("sessions/{sessionId}/messages/read")]
        public async Task<IActionResult> MarkMessagesAsRead(Guid sessionId)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                bool isDoctorRequest = User.IsInRole("Doctor");

                var result = await _chatDoctorService.MarkMessagesAsReadAsync(sessionId, currentUserId, isDoctorRequest);

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
    }
}
