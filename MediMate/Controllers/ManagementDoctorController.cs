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
    [Authorize(Roles = Roles.Admin + "," + Roles.DoctorManager)]
    public class ManagementDoctorController : ControllerBase
    {
        private readonly IDoctorService _doctorService;

        public ManagementDoctorController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctors([FromQuery] GetDoctorsRequest request)
        {
            var data = await _doctorService.GetDoctorsAsync(request.Specialty);
            var response = data.Select(MapDoctorResponse).ToList();
            return Ok(ApiResponse<List<ManagementDoctorResponse>>.Ok(response, "Lấy danh sách bác sĩ nội bộ thành công."));
        }

        [HttpPost]
        public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorRequest request)
        {
            var data = await _doctorService.CreateDoctorAsync(new CreateDoctorDto
            {
                FullName = request.FullName,
                Specialty = request.Specialty,
                CurrentHospitalName = request.CurrentHospitalName,
                LicenseNumber = request.LicenseNumber,
                YearsOfExperience = request.YearsOfExperience,
                Bio = request.Bio,
                UserId = request.UserId
            });

            return Ok(ApiResponse<ManagementDoctorResponse>.Ok(MapDoctorResponse(data), "Tạo hồ sơ bác sĩ thành công."));
        }

        [HttpPut("{doctorId}")]
        public async Task<IActionResult> UpdateDoctor(Guid doctorId, [FromBody] UpdateDoctorRequest request)
        {
            var data = await _doctorService.UpdateDoctorAsync(doctorId, new UpdateDoctorDto
            {
                FullName = request.FullName,
                Specialty = request.Specialty,
                CurrentHospitalName = request.CurrentHospitalName,
                LicenseNumber = request.LicenseNumber,
                YearsOfExperience = request.YearsOfExperience,
                Bio = request.Bio
            });

            return Ok(ApiResponse<ManagementDoctorResponse>.Ok(MapDoctorResponse(data), "Cập nhật hồ sơ bác sĩ thành công."));
        }

        [HttpPatch("{doctorId}/status")]
        public async Task<IActionResult> ChangeStatus(Guid doctorId, [FromBody] ChangeDoctorStatusRequest request)
        {
            var data = await _doctorService.ChangeDoctorStatusAsync(doctorId, new ChangeDoctorStatusDto
            {
                IsActive = request.IsActive
            });

            return Ok(ApiResponse<ManagementDoctorResponse>.Ok(MapDoctorResponse(data), "Cập nhật trạng thái bác sĩ thành công."));
        }

        [HttpPost("{doctorId}/verify-license")]
        public async Task<IActionResult> VerifyLicense(Guid doctorId, [FromBody] VerifyDoctorLicenseRequest request)
        {
            var data = await _doctorService.VerifyDoctorLicenseAsync(doctorId, new VerifyDoctorLicenseDto
            {
                IsVerified = request.IsVerified
            });

            return Ok(ApiResponse<ManagementDoctorResponse>.Ok(MapDoctorResponse(data), "Cập nhật xác minh giấy phép thành công."));
        }

        [HttpPost("{doctorId}/approve")]
        public async Task<IActionResult> ApproveDoctor(Guid doctorId, [FromBody] ApproveDoctorRequest request)
        {
            var data = await _doctorService.ApproveDoctorAsync(doctorId, new ApproveDoctorDto
            {
                Action = request.Action,
                Reason = request.Reason
            });

            return Ok(ApiResponse<ManagementDoctorResponse>.Ok(MapDoctorResponse(data), "Cập nhật duyệt bác sĩ thành công."));
        }

        [HttpPost("{doctorId}/availability")]
        public async Task<IActionResult> AddAvailability(Guid doctorId, [FromBody] CreateDoctorAvailabilityRequest request)
        {
            var data = await _doctorService.AddAvailabilityAsync(doctorId, new CreateDoctorAvailabilityDto
            {
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            });

            return Ok(ApiResponse<DoctorAvailabilityResponse>.Ok(MapAvailabilityResponse(data), "Thêm lịch làm việc thành công."));
        }

        [HttpPut("{doctorId}/availability/{availabilityId}")]
        public async Task<IActionResult> UpdateAvailability(Guid doctorId, Guid availabilityId, [FromBody] UpdateDoctorAvailabilityRequest request)
        {
            var data = await _doctorService.UpdateAvailabilityAsync(doctorId, availabilityId, new UpdateDoctorAvailabilityDto
            {
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                IsBooked = request.IsBooked
            });

            return Ok(ApiResponse<DoctorAvailabilityResponse>.Ok(MapAvailabilityResponse(data), "Cập nhật lịch làm việc thành công."));
        }

        [HttpDelete("{doctorId}/availability/{availabilityId}")]
        public async Task<IActionResult> DeleteAvailability(Guid doctorId, Guid availabilityId)
        {
            await _doctorService.DeleteAvailabilityAsync(doctorId, availabilityId);
            return Ok(ApiResponse<bool>.Ok(true, "Xóa lịch làm việc thành công."));
        }

        private static ManagementDoctorResponse MapDoctorResponse(DoctorDto dto)
        {
            return new ManagementDoctorResponse
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
                IsActive = dto.IsActive,
                ApprovalStatus = dto.ApprovalStatus,
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
                IsBooked = dto.IsBooked
            };
        }
    }
}
