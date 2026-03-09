using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/prescriptions")]
[ApiController]
[Authorize]
public class PrescriptionController : ControllerBase
{
    private readonly IPrescriptionService _prescriptionService;
    private readonly ICurrentUserService _currentUserService;

    public PrescriptionController(IPrescriptionService prescriptionService, ICurrentUserService currentUserService)
    {
        _prescriptionService = prescriptionService;
        _currentUserService = currentUserService;
    }

    // POST: api/v1/prescriptions/member/{memberId}
    // Lưu đơn thuốc mới (sau khi UI đã OCR xong)
    [HttpPost("member/{memberId}")]
    public async Task<IActionResult> CreatePrescription(Guid memberId, [FromBody] CreatePrescriptionRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _prescriptionService.CreatePrescriptionAsync(memberId, userId, request);

            if (!result.Success) return StatusCode(result.Code, result);
            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    // GET: api/v1/prescriptions/member/{memberId}
    // Lấy danh sách đơn thuốc của member
    [HttpGet("member/{memberId}")]
    public async Task<IActionResult> GetByMember(Guid memberId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _prescriptionService.GetPrescriptionsByMemberAsync(memberId, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    // GET: api/v1/prescriptions/{id}
    // Xem chi tiết đơn thuốc
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _prescriptionService.GetPrescriptionByIdAsync(id, userId);

            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }
        [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePrescription(Guid id, [FromBody] UpdatePrescriptionRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _prescriptionService.UpdatePrescriptionAsync(id, userId, request);

            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    // DELETE: api/v1/prescriptions/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePrescription(Guid id)
    {
        try { 
        var userId = _currentUserService.UserId;
        var result = await _prescriptionService.DeletePrescriptionAsync(id, userId);

        if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    // POST: api/v1/prescriptions/{id}/images
    // Upload thêm ảnh cho đơn thuốc
    [HttpPost("{id}/images")]
    public async Task<IActionResult> AddImage(Guid id, IFormFile file)
    {
        try { 
        var userId = _currentUserService.UserId;
        var result = await _prescriptionService.AddImageToPrescriptionAsync(id, userId, file);

        if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }
}