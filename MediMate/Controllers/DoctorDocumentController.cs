using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using Share.Constants;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MediMate.Controllers
{
    [Route("api/v1/doctor-documents")]
    [ApiController]
    public class DoctorDocumentController : ControllerBase
    {
        private readonly IDoctorDocumentService _documentService;
        private readonly ICurrentUserService _currentUserService;

        public DoctorDocumentController(
            IDoctorDocumentService documentService,
            ICurrentUserService currentUserService)
        {
            _documentService = documentService;
            _currentUserService = currentUserService;
        }

        [HttpPost("doctors/{doctorId}")]
        //[Authorize(Roles = Roles.Doctor)]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<DoctorDocumentDto>), 201)]
        public async Task<IActionResult> Create(Guid doctorId, [FromBody] CreateDoctorDocumentRequest request)
        {
            try
            {
                var response = await _documentService.CreateAsync(doctorId, _currentUserService.UserId, request);
                if (!response.Success)
                {
                    return StatusCode(response.Code, response);
                }
                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("doctors/{doctorId}")]
        //[Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.DoctorManager}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DoctorDocumentDto>>), 200)]
        public async Task<IActionResult> GetByDoctorId(Guid doctorId)
        {
            try
            {
                var response = await _documentService.GetByDoctorIdAsync(doctorId);
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        //[Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.DoctorManager}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<DoctorDocumentDto>), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var response = await _documentService.GetByIdAsync(id);
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
        [ProducesResponseType(typeof(ApiResponse<DoctorDocumentDto>), 200)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDoctorDocumentRequest request)
        {
            try
            {
                var response = await _documentService.UpdateAsync(id, _currentUserService.UserId, request);
                if (!response.Success)
                    return StatusCode(response.Code, response);
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
                var response = await _documentService.DeleteAsync(id, _currentUserService.UserId);
                if (!response.Success)
                    return StatusCode(response.Code, response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // ==========================================
        // API DÀNH RIÊNG CHO ADMIN / MANAGER DUYỆT
        // ==========================================
        [HttpPatch("{id}/review")]
        //[Authorize(Roles = $"{Roles.DoctorManager}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<DoctorDocumentDto>), 200)]
        public async Task<IActionResult> ReviewDocument(Guid id, [FromBody] ReviewDoctorDocumentRequest request)
        {
            try
            {
                // Lấy tên Admin đang đăng nhập từ Token (Fullname hoặc Email) để lưu lịch sử duyệt
                var reviewerName = User.FindFirst("FullName")?.Value
                                   ?? User.FindFirst(ClaimTypes.Name)?.Value
                                   ?? "Admin System";

                var response = await _documentService.ReviewDocumentAsync(id, reviewerName, request);
                if (!response.Success)
                    return StatusCode(response.Code, response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}