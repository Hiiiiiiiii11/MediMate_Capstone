using MediMateService.DTOs;
using MediMateService.Services;
using MediMate.Models.Doctors;
using MediMate.Models.Ratings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using Share.Constants;

namespace MediMate.Controllers
{
    [Route("api/v1/doctors")]
    [ApiController]
    public class DoctorController : ControllerBase
    {
        private readonly IDoctorService _doctorService;
        private readonly IRatingService _ratingService;
        private readonly IUploadPhotoService _uploadPhotoService;

        public DoctorController(IDoctorService doctorService, IRatingService ratingService, IUploadPhotoService uploadPhotoService)
        {
            _doctorService = doctorService;
            _ratingService = ratingService;
            _uploadPhotoService = uploadPhotoService;
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

        //[HttpGet("{doctorId}/availability")]
        //[AllowAnonymous]
        //public async Task<IActionResult> GetAvailability(Guid doctorId)
        //{
        //    var data = await _doctorService.GetPublicAvailabilityByDoctorAsync(doctorId);
        //    var response = data.Select(MapAvailabilityResponse).ToList();
        //    return Ok(ApiResponse<List<DoctorAvailabilityResponse>>.Ok(response, "Lấy lịch làm việc bác sĩ thành công."));
        //}

        [HttpGet("{doctorId}/reviews")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviews(Guid doctorId)
        {
            var data = await _ratingService.GetDoctorReviewsAsync(doctorId);
            var response = data.Select(MapReviewResponse).ToList();
            return Ok(ApiResponse<List<DoctorReviewResponse>>.Ok(response, "Lấy đánh giá bác sĩ thành công."));
        }

    
        [HttpGet("me")]
        [Authorize(Roles = Roles.Doctor)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            var data = await _doctorService.GetMyProfileAsync(userId);
            return Ok(ApiResponse<DoctorResponse>.Ok(MapDoctorResponse(data), "Lấy hồ sơ cá nhân thành công."));
        }

     

        [HttpPost("me/submit")]
        [Authorize(Roles = Roles.Doctor)]
        public async Task<IActionResult> SubmitProfile([FromForm] SubmitDoctorRequest request)
        {
            var userId = GetCurrentUserId();
            var doc = await _doctorService.GetMyProfileAsync(userId);
            
            string? licenseImageUrl = doc.LicenseImage;
            if (request.LicenseImage != null)
            {
                var uploadResult = await _uploadPhotoService.UploadPhotoAsync(request.LicenseImage);
                licenseImageUrl = uploadResult.OriginalUrl;
            }

            var data = await _doctorService.SubmitPendingAsync(doc.DoctorId, new SubmitDoctorDto
            {
                FullName = request.FullName,
                Specialty = request.Specialty,
                CurrentHospitalName = request.CurrentHospitalName,
                LicenseNumber = request.LicenseNumber,
                LicenseImage = licenseImageUrl,
                YearsOfExperience = request.YearsOfExperience,
                Bio = request.Bio
            });
            return Ok(ApiResponse<DoctorResponse>.Ok(MapDoctorResponse(data), "Đã nộp hồ sơ, chờ xét duyệt."));
        }

        [HttpPatch("me/online")]
        [Authorize(Roles = Roles.Doctor)]
        public async Task<IActionResult> Heartbeat()
        {
            var userId = GetCurrentUserId();
            var doc = await _doctorService.GetMyProfileAsync(userId);

            await _doctorService.HeartbeatAsync(doc.DoctorId);
            return Ok(ApiResponse<bool>.Ok(true, "Heartbeat updated."));
        }

        public class ActivateDoctorRequest
        {
            public Guid DoctorId { get; set; }
            public int VerifyCode { get; set; }
        }

        [HttpPost("activate")]
        public async Task<IActionResult> Activate([FromBody] ActivateDoctorRequest request)
        {
            var data = await _doctorService.ActivateDoctorAsync(request.DoctorId, request.VerifyCode);
            return Ok(ApiResponse<DoctorResponse>.Ok(MapDoctorResponse(data), "Kích hoạt tài khoản thành công."));
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (Guid.TryParse(userIdClaim, out var userId)) return userId;
            throw new UnauthorizedAccessException("Không tìm thấy UserId trong token.");
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
                LicenseImage = dto.LicenseImage,
                YearsOfExperience = dto.YearsOfExperience,
                Bio = dto.Bio,
                AverageRating = dto.AverageRating,
                Status = dto.Status,
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
