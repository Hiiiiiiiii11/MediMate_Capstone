using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Constants;
using System;
using System.Threading.Tasks;

namespace MediMate.Controllers
{
    [Route("api/v1/doctor-availabilities")]
    [ApiController]
    public class DoctorAvailabilityController : ControllerBase
    {
        private readonly IDoctorAvailabilityService _availabilityService;
        private readonly ICurrentUserService _currentUserService;

        public DoctorAvailabilityController(
            IDoctorAvailabilityService availabilityService,
            ICurrentUserService currentUserService)
        {
            _availabilityService = availabilityService;
            _currentUserService = currentUserService;
        }

        [HttpPost("doctors/{doctorId}")]
        //[Authorize(Roles = Roles.Doctor)]
        [Authorize]
        public async Task<IActionResult> Create(Guid doctorId, [FromBody] CreateDoctorAvailabilityRequest request)
        {
            try
            {
                var response = await _availabilityService.CreateAsync(doctorId, _currentUserService.UserId, request);
                if (!response.Success) return StatusCode(response.Code, response);
                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // Bất kỳ ai (User, Doctor, Admin) cũng có thể xem lịch để đặt khám
        [HttpGet("doctors/{doctorId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByDoctorId(Guid doctorId)
        {
            try
            {
                var response = await _availabilityService.GetByDoctorIdAsync(doctorId);
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var response = await _availabilityService.GetByIdAsync(id);
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        //[Authorize(Roles = Roles.Doctor)]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDoctorAvailabilityRequest request)
        {
            try
            {
                var response = await _availabilityService.UpdateAsync(id, _currentUserService.UserId, request);
                if (!response.Success) return StatusCode(response.Code, response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = Roles.Doctor)]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var response = await _availabilityService.DeleteAsync(id, _currentUserService.UserId);
                if (!response.Success) return StatusCode(response.Code, response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}