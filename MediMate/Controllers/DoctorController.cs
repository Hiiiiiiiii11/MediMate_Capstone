using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using Share.Constants;

namespace MediMate.Controllers
{
    [Route("api/v1/doctors")]
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    public class DoctorController : ControllerBase
    {
        private readonly IDoctorService _doctorService;

        public DoctorController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctors([FromQuery] string? specialty = null)
        {
            var data = await _doctorService.GetDoctorsAsync(specialty);
            return Ok(ApiResponse<List<DoctorDto>>.Ok(data, "Lấy danh sách bác sĩ thành công."));
        }

        [HttpGet("{doctorId}")]
        public async Task<IActionResult> GetDoctorById(Guid doctorId)
        {
            var data = await _doctorService.GetDoctorByIdAsync(doctorId);
            return Ok(ApiResponse<DoctorDto>.Ok(data, "Lấy chi tiết bác sĩ thành công."));
        }

        [HttpGet("{doctorId}/availability")]
        public async Task<IActionResult> GetAvailability(Guid doctorId)
        {
            var data = await _doctorService.GetAvailabilityByDoctorAsync(doctorId);
            return Ok(ApiResponse<List<DoctorAvailabilityDto>>.Ok(data, "Lấy lịch làm việc bác sĩ thành công."));
        }

        [HttpGet("{doctorId}/exceptions")]
        public async Task<IActionResult> GetExceptions(Guid doctorId)
        {
            var data = await _doctorService.GetExceptionsByDoctorAsync(doctorId);
            return Ok(ApiResponse<List<DoctorAvailabilityExceptionDto>>.Ok(data, "Lấy ngoại lệ lịch bác sĩ thành công."));
        }
    }
}
