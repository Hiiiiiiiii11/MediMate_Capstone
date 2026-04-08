using MediMateRepository.Model;
using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using System;

namespace MediMate.Controllers
{
    [Route("api/v1/video-call")]
    [ApiController]
    [Authorize]
    public class VideoCallController : ControllerBase
    {
        private readonly IAgoraService _agoraService;
        private readonly ICurrentUserService _currentUserService;   

        public VideoCallController(IAgoraService agoraService, ICurrentUserService currentUserService)
        {
            _agoraService = agoraService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Lấy Agora token cho Doctor hoặc Member (user) thông thường.
        /// uid = 0 → Agora tự cấp UID ngẫu nhiên.
        /// </summary>
        [HttpGet("token/{sessionId}")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        public async Task<IActionResult> GetToken(Guid sessionId, [FromQuery] string role = "publisher")
        {
            uint uid = 0;
            var result = await _agoraService.GenerateRtcTokenAsync(sessionId, uid, role);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }

        /// <summary>
        /// Người giám hộ (Guardian/Owner) tham gia cuộc gọi khám bệnh của dependent member.
        /// Chỉ cho phép nếu session.GuardianUserId == currentUserId.
        /// </summary>
        [HttpPost("sessions/{sessionId}/guardian/join")]
        [ProducesResponseType(typeof(ApiResponse<GuardianJoinResponse>), 200)]
        public async Task<IActionResult> GuardianJoin(Guid sessionId)
        {
            // Lấy ID trực tiếp (Vì _currentUserService.UserId đã là kiểu Guid)
            Guid guardianUserId = _currentUserService.UserId;

            if (guardianUserId == Guid.Empty)
                return Unauthorized(ApiResponse<GuardianJoinResponse>.Fail("Không xác định được user.", 401));

            var result = await _agoraService.GenerateGuardianTokenAsync(sessionId, guardianUserId);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
    }
}