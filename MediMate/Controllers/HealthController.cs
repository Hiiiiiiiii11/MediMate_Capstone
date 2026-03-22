using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;

[Route("api/v1/health")]
[ApiController]
[Authorize]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;
    private readonly ICurrentUserService _currentUserService;

    public HealthController(IHealthService healthService, ICurrentUserService currentUserService)
    {
        _healthService = healthService;
        _currentUserService = currentUserService;
    }

    // GET: api/v1/health/member/{memberId}
    [HttpGet("member/{memberId}")]
    [ProducesResponseType(typeof(ApiResponse<HealthProfileResponse>), 200)]
    public async Task<IActionResult> GetProfile(Guid memberId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _healthService.GetHealthProfileAsync(memberId, userId);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }
    // POST: create member healthprofile
    [HttpPost("member/{memberId}")]
    [ProducesResponseType(typeof(ApiResponse<HealthProfileResponse>), 201)]
    public async Task<IActionResult> CreateProfile(Guid memberId, [FromBody] CreateHealthProfileRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _healthService.CreateHealthProfileAsync(memberId, userId, request);

            if (!result.Success) return StatusCode(result.Code, result);
            // Trả về 201 Created là chuẩn nhất cho hàm tạo
            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }
    // PUT: api/v1/health/member/{memberId} -> Cập nhật chiều cao/cân nặng
    [HttpPut("member/{memberId}")]
    [ProducesResponseType(typeof(ApiResponse<HealthProfileResponse>), 200)]
    public async Task<IActionResult> UpdateProfile(Guid memberId, [FromBody] UpdateHealthProfileRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _healthService.UpdateHealthProfileAsync(memberId, userId, request);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    // POST: api/v1/health/member/{memberId}/conditions -> Thêm bệnh
    [HttpPost("member/{memberId}/conditions")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> AddCondition(Guid memberId, [FromBody] AddConditionRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _healthService.AddConditionAsync(memberId, userId, request);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    // DELETE: api/v1/health/conditions/{conditionId} -> Xóa bệnh
    [HttpDelete("conditions/{conditionId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> RemoveCondition(Guid conditionId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _healthService.RemoveConditionAsync(conditionId, userId);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }
    [HttpGet("family/{familyId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FamilyHealthSummaryResponse>>), 200)]
    public async Task<IActionResult> GetFamilyHealthSummary(Guid familyId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _healthService.GetHealthProfilesByFamilyIdAsync(familyId, userId);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    // 2. GET: api/v1/health/conditions/{conditionId}
    // Xem chi tiết 1 bệnh án
    [HttpGet("conditions/{conditionId}")]
    [ProducesResponseType(typeof(ApiResponse<HealthConditionDto>), 200)]
    public async Task<IActionResult> GetConditionDetail(Guid conditionId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _healthService.GetConditionByIdAsync(conditionId, userId);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    // 3. PUT: api/v1/health/conditions/{conditionId}
    // Cập nhật bệnh án
    [HttpPut("conditions/{conditionId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> UpdateCondition(Guid conditionId, [FromBody] UpdateConditionRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _healthService.UpdateConditionAsync(conditionId, userId, request);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }
}