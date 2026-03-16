using MediMate.Models.Doctors;
using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using Share.Constants;

namespace MediMate.Controllers
{
    [Route("api/v1/management/doctors")]
    [ApiController]
    [Authorize]
    public class ManagementDoctorController : ControllerBase
    {
        private readonly IDoctorService _doctorService;

        public ManagementDoctorController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        [HttpGet]
        //[Authorize(Roles = $"{Roles.Admin},{Roles.DoctorManager}")]
        [Authorize]
        public async Task<IActionResult> GetDoctors([FromQuery] GetDoctorsRequest request, [FromQuery] string? status = null)
        {
            var data = await _doctorService.GetDoctorsAsync(request.Specialty, status);
            var response = data.Select(MapResponse).ToList();
            return Ok(ApiResponse<List<ManagementDoctorResponse>>.Ok(response, "Lấy danh sách bác sĩ thành công."));
        }

        [HttpGet("{doctorId}")]
        //[Authorize(Roles = $"{Roles.Admin},{Roles.DoctorManager},{Roles.Doctor}")]
        [Authorize]
        public async Task<IActionResult> GetDoctorById(Guid doctorId)
        {
            var data = await _doctorService.GetDoctorByIdAsync(doctorId);
            return Ok(ApiResponse<ManagementDoctorResponse>.Ok(MapResponse(data), "Lấy chi tiết bác sĩ thành công."));
        }


        [HttpPost("{doctorId}/verify")]
        //[Authorize(Roles = Roles.DoctorManager)]
        [Authorize]
        public async Task<IActionResult> Verify(Guid doctorId)
        {
            var data = await _doctorService.VerifyDoctorAsync(doctorId);
            return Ok(ApiResponse<ManagementDoctorResponse>.Ok(MapResponse(data), "Xác minh bằng cấp thành công."));
        }

        [HttpPost("{doctorId}/approve")]
        //[Authorize(Roles = Roles.DoctorManager)]
        [Authorize]
        public async Task<IActionResult> Approve(Guid doctorId)
        {
            var data = await _doctorService.ApproveDoctorAsync(doctorId);
            return Ok(ApiResponse<ManagementDoctorResponse>.Ok(MapResponse(data), "Bác sĩ đã được phê duyệt."));
        }

        [HttpPost("{doctorId}/reject")]
        //[Authorize(Roles = $"{Roles.Admin},{Roles.DoctorManager}")]
        [Authorize]
        public async Task<IActionResult> Reject(Guid doctorId, [FromBody] RejectDoctorRequest request)
        {
            var data = await _doctorService.RejectDoctorAsync(doctorId, request.Reason);
            return Ok(ApiResponse<ManagementDoctorResponse>.Ok(MapResponse(data), "Đã từ chối hồ sơ bác sĩ."));
        }

        //[HttpPost("{doctorId}/availability")]
        //[Authorize(Roles = $"{Roles.Admin},{Roles.DoctorManager}")]
        //public async Task<IActionResult> AddAvailability(Guid doctorId, [FromBody] CreateDoctorAvailabilityRequest request)
        //{
        //    var data = await _doctorService.AddAvailabilityAsync(doctorId, new CreateDoctorAvailabilityDto
        //    {
        //        DayOfWeek = request.DayOfWeek,
        //        StartTime = request.StartTime,
        //        EndTime = request.EndTime
        //    });
        //    return Ok(ApiResponse<DoctorAvailabilityResponse>.Ok(MapAvailabilityResponse(data), "Thêm lịch làm việc thành công."));
        //}

        //[HttpPut("{doctorId}/availability/{availabilityId}")]
        //[Authorize(Roles = $"{Roles.Admin},{Roles.DoctorManager}")]
        //public async Task<IActionResult> UpdateAvailability(Guid doctorId, Guid availabilityId, [FromBody] UpdateDoctorAvailabilityRequest request)
        //{
        //    var data = await _doctorService.UpdateAvailabilityAsync(doctorId, availabilityId, new UpdateDoctorAvailabilityDto
        //    {
        //        DayOfWeek = request.DayOfWeek,
        //        StartTime = request.StartTime,
        //        EndTime = request.EndTime,
        //        IsActive = request.IsActive
        //    });
        //    return Ok(ApiResponse<DoctorAvailabilityResponse>.Ok(MapAvailabilityResponse(data), "Cập nhật lịch làm việc thành công."));
        //}

        //[HttpDelete("{doctorId}/availability/{availabilityId}")]
        //[Authorize(Roles = $"{Roles.Admin},{Roles.DoctorManager}")]
        //public async Task<IActionResult> DeleteAvailability(Guid doctorId, Guid availabilityId)
        //{
        //    await _doctorService.DeleteAvailabilityAsync(doctorId, availabilityId);
        //    return Ok(ApiResponse<bool>.Ok(true, "Xóa lịch làm việc thành công."));
        //}

        private static ManagementDoctorResponse MapResponse(DoctorDto dto) => new()
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
            RejectionReason = dto.RejectionReason,
            IsOnline = dto.IsOnline,
            LastSeenAt = dto.LastSeenAt,
            CreatedAt = dto.CreatedAt,
            UserId = dto.UserId
        };

        private static DoctorAvailabilityResponse MapAvailabilityResponse(DoctorAvailabilityDto dto) => new()
        {
            DoctorAvailabilityId = dto.DoctorAvailabilityId,
            DoctorId = dto.DoctorId,
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            IsActive = dto.IsActive
        };
    }
}
