using MediMate.Models.Consultations;
using MediMate.Models.Ratings;
using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using Share.Constants;

namespace MediMate.Controllers
{
    [Route("api/v1/consultations")]
    [ApiController]
    public class ConsultationController : ControllerBase
    {
        private readonly IRatingService _ratingService;
        private readonly IConsultationService _consultationService;
        private readonly ICurrentUserService _currentUserService;

        public ConsultationController(
            IRatingService ratingService,
            IConsultationService consultationService,
            ICurrentUserService currentUserService)
        {
            _ratingService = ratingService;
            _consultationService = consultationService;
            _currentUserService = currentUserService;
        }

        [HttpGet("{appointmentId}")]
        [Authorize]
        public async Task<IActionResult> GetConsultation(Guid appointmentId)
        {
            var userId = _currentUserService.UserId;
            var data = await _consultationService.GetByAppointmentIdAsync(appointmentId, userId);
            return Ok(ApiResponse<ConsultationSessionResponse>.Ok(MapConsultationResponse(data), "Lấy thông tin phiên tư vấn thành công."));
        }

        [HttpPost("{sessionId}/end")]
        [Authorize]
        public async Task<IActionResult> EndConsultation(Guid sessionId, [FromBody] EndConsultationRequest request)
        {
            var userId = _currentUserService.UserId;
            var data = await _consultationService.EndSessionAsync(sessionId, userId, new EndConsultationDto
            {
                EndedAt = request.EndedAt
            });

            return Ok(ApiResponse<ConsultationSessionResponse>.Ok(MapConsultationResponse(data), "Kết thúc phiên tư vấn thành công."));
        }

        [HttpPost("{sessionId}/prescription")]
        [Authorize]
        public async Task<IActionResult> AttachPrescription(Guid sessionId, [FromBody] AttachPrescriptionRequest request)
        {
            var userId = _currentUserService.UserId;
            var data = await _consultationService.AttachPrescriptionAsync(sessionId, userId, new AttachPrescriptionDto
            {
                PrescriptionId = request.PrescriptionId
            });

            return Ok(ApiResponse<ConsultationSessionResponse>.Ok(MapConsultationResponse(data), "Gắn đơn thuốc cho phiên tư vấn thành công."));
        }

        [HttpPost("{sessionId}/rating")]
        [Authorize(Roles = Roles.User)]
        public async Task<IActionResult> CreateRating(Guid sessionId, [FromBody] CreateRatingRequest request)
        {
            var data = await _ratingService.CreateRatingAsync(sessionId, new CreateRatingDto
            {
                Score = request.Score,
                Comment = request.Comment
            });

            return Ok(ApiResponse<RatingResponse>.Ok(MapRatingResponse(data), "Đánh giá phiên khám thành công."));
        }

        private static RatingResponse MapRatingResponse(RatingDto dto)
        {
            return new RatingResponse
            {
                RatingId = dto.RatingId,
                SessionId = dto.SessionId,
                DoctorId = dto.DoctorId,
                MemberId = dto.MemberId,
                Score = dto.Score,
                Comment = dto.Comment,
                CreatedAt = dto.CreatedAt
            };
        }

        private static ConsultationSessionResponse MapConsultationResponse(ConsultationSessionDto dto)
        {
            return new ConsultationSessionResponse
            {
                SessionId = dto.SessionId,
                AppointmentId = dto.AppointmentId,
                DoctorId = dto.DoctorId,
                MemberId = dto.MemberId,
                StartedAt = dto.StartedAt,
                EndedAt = dto.EndedAt,
                Status = dto.Status,
                DoctorNotes = dto.DoctorNotes
            };
        }
    }
}
