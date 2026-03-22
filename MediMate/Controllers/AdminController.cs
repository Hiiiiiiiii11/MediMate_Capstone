using MediMate.Models.Doctors;
using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using Share.Constants;

namespace MediMate.Controllers
{
    [Route("api/v1/admin")]
    [ApiController]
    //[Authorize(Roles = Roles.Admin)]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IDoctorService _doctorService;
        private readonly IUserService _userService;

        public AdminController(IDoctorService doctorService, IUserService userService)
        {
            _doctorService = doctorService;
            _userService = userService;
        }

        [HttpPost("doctors")]
        [ProducesResponseType(typeof(ApiResponse<ManagementDoctorResponse>), 200)]
        public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorRequest request)
        {
            var data = await _doctorService.CreateDoctorAsync(new CreateDoctorDto
            {
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                FullName = request.FullName
            });
            return Ok(ApiResponse<ManagementDoctorResponse>.Ok(MapResponse(data), "Tạo hồ sơ bác sĩ thành công."));
        }

        [HttpPost("doctor-managers")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), 200)]
        public async Task<IActionResult> CreateDoctorManager([FromBody] CreateDoctorManagerRequest request)
        {
            var data = await _userService.CreateDoctorManagerAsync(new CreateDoctorManagerDto
            {
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                FullName = request.FullName
            });
            return Ok(data);
        }

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
    }
}
