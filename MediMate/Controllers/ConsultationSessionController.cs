using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using System.Security.Claims;

namespace MediMate.Controllers
{
    /// <summary>
    /// Quản lý vòng đời của ConsultationSession:
    /// join, mark doctor late, cancel no-show, end by user.
    /// </summary>
    [Route("api/v1/sessions")]
    [ApiController]
    [Authorize]
    public class ConsultationSessionController : ControllerBase
    {
        private readonly IConsultationService _consultationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAgoraRecordingService _agoraRecordingService;

        public ConsultationSessionController(
            IConsultationService consultationService,
            ICurrentUserService currentUserService,
            IAgoraRecordingService agoraRecordingService)
        {
            _consultationService = consultationService;
            _currentUserService = currentUserService;
            _agoraRecordingService = agoraRecordingService;
        }

        // ─────────────────────────────────────────────────────────
        // GET: Tất cả phiên tư vấn của bác sĩ đang đăng nhập (JWT → Doctor)
        // ─────────────────────────────────────────────────────────
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ConsultationSessionDto>>), 200)]
        public async Task<IActionResult> GetMySessionsAsDoctor()
        {
            var userId = _currentUserService.UserId;
            var result = await _consultationService.GetSessionsForCurrentDoctorAsync(userId);
            return Ok(ApiResponse<IEnumerable<ConsultationSessionDto>>.Ok(result, "Lấy danh sách phiên tư vấn thành công."));
        }

        // ─────────────────────────────────────────────────────────
        // GET: Lấy session theo appointmentId
        // ─────────────────────────────────────────────────────────
        [HttpGet("by-appointment/{appointmentId}")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationSessionDto>), 200)]
        public async Task<IActionResult> GetByAppointment(Guid appointmentId)
        {
            var userId = _currentUserService.UserId;
            var result = await _consultationService.GetByAppointmentIdAsync(appointmentId, userId);
            return Ok(ApiResponse<ConsultationSessionDto>.Ok(result, "Lấy thông tin phiên tư vấn thành công."));
        }

        // ─────────────────────────────────────────────────────────
        // PATCH: User hoặc Doctor join session
        //   role = "user" | "doctor"
        //   Khi cả 2 join → Status tự động chuyển sang InProgress (event-driven)
        // ─────────────────────────────────────────────────────────
        [HttpPost("{sessionId}/join")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationSessionDto>), 200)]
        public async Task<IActionResult> JoinSession(Guid sessionId, [FromBody] JoinSessionDto request)
        {
            var userId = _currentUserService.UserId;
            var result = await _consultationService.JoinSessionAsync(sessionId, userId, request.Role);
            return Ok(ApiResponse<ConsultationSessionDto>.Ok(result, "Tham gia phiên tư vấn thành công."));
        }

        // ─────────────────────────────────────────────────────────
        // PATCH: Bệnh nhân ghi nhận bác sĩ trễ hẹn
        //   Body: { "lateMinutes": 10 }
        //   Note được ghi: "Bác sĩ đi trễ 10 phút"
        // ─────────────────────────────────────────────────────────
        [HttpPost("{sessionId}/doctor-late")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationSessionDto>), 200)]
        public async Task<IActionResult> MarkDoctorLate(Guid sessionId, [FromBody] DoctorLateDto request)
        {
            var userId = _currentUserService.UserId;
            var result = await _consultationService.MarkDoctorLateAsync(sessionId, userId, request.LateMinutes);
            return Ok(ApiResponse<ConsultationSessionDto>.Ok(result, "Đã ghi nhận bác sĩ trễ hẹn."));
        }

        // ─────────────────────────────────────────────────────────
        // PATCH: Bệnh nhân huỷ phiên vì bác sĩ không tham gia (no-show)
        //   Note tự động: "Khách huỷ vì lý do không gặp bác sĩ"
        //   Appointment → Cancelled, lượt khám được hoàn trả
        // ─────────────────────────────────────────────────────────
        [HttpPut("{sessionId}/cancel-no-show")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationSessionDto>), 200)]
        public async Task<IActionResult> CancelNoShow(Guid sessionId)
        {
            var userId = _currentUserService.UserId;
            var result = await _consultationService.CancelNoShowAsync(sessionId, userId);
            return Ok(ApiResponse<ConsultationSessionDto>.Ok(result, "Đã huỷ phiên tư vấn do bác sĩ không tham gia."));
        }

        // ─────────────────────────────────────────────────────────
        // POST: Cả bệnh nhân và bác sĩ đều được kết thúc phiên meet
        //   Sau khi End: bác sĩ vẫn có thể gửi tin nhắn, bệnh nhân thì không
        //   Appointment → Completed
        // ─────────────────────────────────────────────────────────
        [HttpPost("{sessionId}/end")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationSessionDto>), 200)]
        public async Task<IActionResult> EndSession(Guid sessionId)
        {
            var userId = _currentUserService.UserId;
            var result = await _consultationService.EndSessionAsync(sessionId, userId);
            return Ok(ApiResponse<ConsultationSessionDto>.Ok(result, "Phiên tư vấn đã kết thúc thành công."));
        }

        // ─────────────────────────────────────────────────────────
        // POST: Bác sĩ gắn đơn thuốc vào session
        // ─────────────────────────────────────────────────────────
        [HttpPost("{sessionId}/attach-prescription")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationSessionDto>), 200)]
        public async Task<IActionResult> AttachPrescription(Guid sessionId, [FromBody] AttachPrescriptionDto request)
        {
            var userId = _currentUserService.UserId;
            var result = await _consultationService.AttachPrescriptionAsync(sessionId, userId, request);
            return Ok(ApiResponse<ConsultationSessionDto>.Ok(result, "Đã gắn đơn thuốc vào phiên tư vấn."));
        }

        // ─────────────────────────────────────────────────────────
        // GET: Xem URL video ghi lại phiên khám
        // Chỉ Bác sĩ phụ trách và Family Owner mới được xem
        // ─────────────────────────────────────────────────────────
        /// <summary>Lấy URL video ghi lại phiên khám. Chỉ Bác sĩ hoặc chủ hộ mới được xem.</summary>
        [HttpGet("{sessionId}/recording")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        public async Task<IActionResult> GetRecordingUrl(Guid sessionId)
        {
            var userId = _currentUserService.UserId;
            var url = await _agoraRecordingService.GetRecordingUrlAsync(sessionId, userId);
            if (url == null)
                return Ok(ApiResponse<string>.Ok(null, "Phiên này chưa có bản ghi hình."));
            return Ok(ApiResponse<string>.Ok(url, "Lấy URL video thành công."));
        }
        // ─────────────────────────────────────────────────────────
        // POST: Bác sĩ yêu cầu kết thúc phiên
        // ─────────────────────────────────────────────────────────
        [HttpPost("{sessionId}/request-end")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> RequestEndSession(Guid sessionId)
        {
            var userId = _currentUserService.UserId;
            await _consultationService.RequestEndSessionAsync(sessionId, userId);
            return Ok(ApiResponse<bool>.Ok(true, "Đã gửi yêu cầu kết thúc đến bệnh nhân."));
        }

        // ─────────────────────────────────────────────────────────
        // POST: Bác sĩ thử ghi hình lại nếu bị lỗi
        // ─────────────────────────────────────────────────────────
        [HttpPost("{sessionId}/retry-recording")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> RetryRecording(Guid sessionId)
        {
            var userId = _currentUserService.UserId;
            var started = await _consultationService.RetryRecordingAsync(sessionId, userId);
            if (started) return Ok(ApiResponse<bool>.Ok(true, "Đã gửi yêu cầu ghi hình lại thành công."));
            return BadRequest(ApiResponse<bool>.Fail("Không thể ghi hình lại. Vui lòng kiểm tra cấu hình."));
        }
    }
}
