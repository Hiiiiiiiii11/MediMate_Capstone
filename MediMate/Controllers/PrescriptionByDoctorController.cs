using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Constants;
using System;
using System.Threading.Tasks;

namespace MediMate.Controllers
{
    [Route("api/v1/doctor-prescriptions")]
    [ApiController]
    [Authorize] // Phải đăng nhập mới được xài các API này
    public class PrescriptionByDoctorController : ControllerBase
    {
        private readonly IPrescriptionByDoctorService _prescriptionService;
        private readonly ICurrentUserService _currentUserService;

        public PrescriptionByDoctorController(
            IPrescriptionByDoctorService prescriptionService,
            ICurrentUserService currentUserService)
        {
            _prescriptionService = prescriptionService;
            _currentUserService = currentUserService;
        }

        [HttpPost("doctors/{doctorId}")]
        [Authorize(Roles = Roles.Doctor)]
        public async Task<IActionResult> Create(Guid doctorId, [FromBody] CreatePrescriptionByDoctorRequest request)
        {
            try
            {
                var response = await _prescriptionService.CreateAsync(doctorId, _currentUserService.UserId, request);
                if (!response.Success) return StatusCode(response.Code, response);
                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var response = await _prescriptionService.GetByIdAsync(id, _currentUserService.UserId);
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("sessions/{sessionId}")]
        public async Task<IActionResult> GetBySessionId(Guid sessionId)
        {
            try
            {
                var response = await _prescriptionService.GetBySessionIdAsync(sessionId, _currentUserService.UserId);
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("members/{memberId}")]
        public async Task<IActionResult> GetByMemberId(Guid memberId)
        {
            try
            {
                var response = await _prescriptionService.GetByMemberIdAsync(memberId, _currentUserService.UserId);
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Doctor)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePrescriptionByDoctorRequest request)
        {
            try
            {
                var response = await _prescriptionService.UpdateAsync(id, _currentUserService.UserId, request);
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