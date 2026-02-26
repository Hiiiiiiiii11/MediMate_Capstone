using MediMateService.DTOs;
using MediMateService.Services;
using MediMate.Models.Doctors;
using MediMate.Models.Ratings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;

namespace MediMate.Controllers
{
    [Route("api/v1/doctors")]
    [ApiController]
    public class DoctorController : ControllerBase
    {
        private readonly IDoctorService _doctorService;
        private readonly IRatingService _ratingService;

        public DoctorController(IDoctorService doctorService, IRatingService ratingService)
        {
            _doctorService = doctorService;
            _ratingService = ratingService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetDoctors([FromQuery] GetDoctorsRequest request)
        {
            var data = await _doctorService.GetPublicDoctorsAsync(request.Specialty);
            var response = data.Select(MapDoctorResponse).ToList();
            return Ok(ApiResponse<List<DoctorResponse>>.Ok(response, "Lấy danh sách bác sĩ thành công."));
        }

        [HttpGet("{doctorId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDoctorById(Guid doctorId)
        {
            var data = await _doctorService.GetPublicDoctorByIdAsync(doctorId);
            var response = MapDoctorResponse(data);
            return Ok(ApiResponse<DoctorResponse>.Ok(response, "Lấy chi tiết bác sĩ thành công."));
        }

        [HttpGet("{doctorId}/availability")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailability(Guid doctorId)
        {
            var data = await _doctorService.GetPublicAvailabilityByDoctorAsync(doctorId);
            var response = data.Select(MapAvailabilityResponse).ToList();
            return Ok(ApiResponse<List<DoctorAvailabilityResponse>>.Ok(response, "Lấy lịch làm việc bác sĩ thành công."));
        }

        [HttpGet("{doctorId}/reviews")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviews(Guid doctorId)
        {
            var data = await _ratingService.GetDoctorReviewsAsync(doctorId);
            var response = data.Select(MapReviewResponse).ToList();
            return Ok(ApiResponse<List<DoctorReviewResponse>>.Ok(response, "Lấy đánh giá bác sĩ thành công."));
        }

        private static DoctorResponse MapDoctorResponse(DoctorDto dto)
        {
            return new DoctorResponse
            {
                DoctorId = dto.DoctorId,
                FullName = dto.FullName,
                Specialty = dto.Specialty,
                CurrentHospitalName = dto.CurrentHospitalName,
                LicenseNumber = dto.LicenseNumber,
                YearsOfExperience = dto.YearsOfExperience,
                Bio = dto.Bio,
                AverageRating = dto.AverageRating,
                IsVerified = dto.IsVerified,
                CreatedAt = dto.CreatedAt,
                UserId = dto.UserId
            };
        }

        private static DoctorAvailabilityResponse MapAvailabilityResponse(DoctorAvailabilityDto dto)
        {
            return new DoctorAvailabilityResponse
            {
                DoctorAvailabilityId = dto.DoctorAvailabilityId,
                DoctorId = dto.DoctorId,
                DayOfWeek = dto.DayOfWeek,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsActive = dto.IsActive
            };
        }

        private static DoctorReviewResponse MapReviewResponse(DoctorReviewDto dto)
        {
            return new DoctorReviewResponse
            {
                RatingId = dto.RatingId,
                SessionId = dto.SessionId,
                MemberId = dto.MemberId,
                Score = dto.Score,
                Comment = dto.Comment,
                CreatedAt = dto.CreatedAt
            };
        }
    }
}
