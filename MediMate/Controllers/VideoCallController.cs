using MediMateRepository.Model;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using System;

namespace MediMate.Controllers
{
    [Route("api/v1/video-call")]
    [ApiController]
    [Authorize] // Bắt buộc đăng nhập mới được gọi video
    public class VideoCallController : ControllerBase
    {
        private readonly IAgoraService _agoraService;

        public VideoCallController(IAgoraService agoraService)
        {
            _agoraService = agoraService;
        }

        [HttpGet("token/{sessionId}")]
        public async Task<IActionResult> GetToken(Guid sessionId, [FromQuery] string role = "publisher")
        {
            // Để Agora tự động cấp UID ngẫu nhiên
            uint uid = 0;

            // Gọi thẳng xuống Service (Service tự lo việc check DB)
            var result = await _agoraService.GenerateRtcTokenAsync(sessionId, uid, role);

            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);


        }
    }
}