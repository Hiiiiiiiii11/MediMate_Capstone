using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using Share.Constants;
using System;
using System.Threading.Tasks;

namespace MediMate.Controllers
{
    [Route("api/v1/doctor-availability-exceptions")]
    [ApiController]
    public class DoctorAvailabilityExceptionController : ControllerBase
    {
        private readonly IDoctorAvailabilityExceptionService _exceptionService;
        private readonly ICurrentUserService _currentUserService;

        public DoctorAvailabilityExceptionController(
            IDoctorAvailabilityExceptionService exceptionService,
            ICurrentUserService currentUserService)
        {
            _exceptionService = exceptionService;
            _currentUserService = currentUserService;
        }

        [HttpPost("doctors/{doctorId}")]
        //[Authorize(Roles = Roles.Doctor)]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<DoctorAvailabilityExceptionDto>), 201)]
        public async Task<IActionResult> Create(Guid doctorId, [FromBody] CreateDoctorAvailabilityExceptionRequest request)
        {
            try
            {
                var response = await _exceptionService.CreateAsync(doctorId, _currentUserService.UserId, request);
                if (!response.Success) return StatusCode(response.Code, response);
                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("doctors/{doctorId}")]
        [AllowAnonymous] // Cho phép ai cũng được xem để Frontend còn biết ngày nào bác sĩ nghỉ mà khóa ô chọn giờ lại
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DoctorAvailabilityExceptionDto>>), 200)]
        public async Task<IActionResult> GetByDoctorId(Guid doctorId)
        {
            try
            {
                var response = await _exceptionService.GetByDoctorIdAsync(doctorId);
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<DoctorAvailabilityExceptionDto>), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var response = await _exceptionService.GetByIdAsync(id);
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
        [ProducesResponseType(typeof(ApiResponse<DoctorAvailabilityExceptionDto>), 200)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDoctorAvailabilityExceptionRequest request)
        {
            try
            {
                var response = await _exceptionService.UpdateAsync(id, _currentUserService.UserId, request);
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
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var response = await _exceptionService.DeleteAsync(id, _currentUserService.UserId);
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