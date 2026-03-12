using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using System;
using System.Threading.Tasks;

namespace MediMateApi.Controllers
{
    [Route("api/v1/chatbot")]
    [ApiController]
    [Authorize] // Bắt buộc phải có Token (Bearer)
    public class ChatbotController : ControllerBase
    {
        private readonly IChatBotService _chatbotService;
        private readonly ICurrentUserService _currentUserService;

        public ChatbotController(IChatBotService chatbotService, ICurrentUserService currentUserService)
        {
            _chatbotService = chatbotService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Lấy danh sách các phiên chat (Sessions) của một thành viên
        /// </summary>
        [HttpGet("members/{memberId}/sessions")]
        public async Task<IActionResult> GetMemberSessions(Guid memberId)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                var result = await _chatbotService.GetMemberSessionsAsync(memberId, currentUserId);

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
            /// Gửi tin nhắn cho Bot AI (Tạo phiên mới nếu SessionId null)
            /// </summary>
            [HttpPost("members/{memberId}/messages")]
        public async Task<IActionResult> SendMessage(Guid memberId, [FromBody] SendMessageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.Fail("Dữ liệu đầu vào không hợp lệ.", 400));
                }

                var currentUserId = _currentUserService.UserId;
                var result = await _chatbotService.SendMessageAsync(memberId, currentUserId, request);

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
        /// Lấy toàn bộ lịch sử tin nhắn trong 1 phiên chat cụ thể
        /// </summary>
        [HttpGet("sessions/{sessionId}/messages")]
        public async Task<IActionResult> GetSessionMessages(Guid sessionId)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                var result = await _chatbotService.GetSessionMessagesAsync(sessionId, currentUserId);

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
        /// Xóa (ẩn) một phiên chat
        /// </summary>
        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> DeleteSession(Guid sessionId)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                var result = await _chatbotService.DeleteSessionAsync(sessionId, currentUserId);

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