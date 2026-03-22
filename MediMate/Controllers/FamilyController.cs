using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;

[Route("api/v1/families")]
[ApiController]
[Authorize]
public class FamilyController : ControllerBase
{
    private readonly IFamilyService _familyService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMemberService _memberService;
    // ... Constructor ...
    public FamilyController(IFamilyService familyService, ICurrentUserService currentUserService, IMemberService memberService)
    {
        _familyService = familyService;
        _currentUserService = currentUserService;
        _memberService = memberService;
    }
    // 1. API: Tạo chế độ cá nhân (Không cần body request)
    [HttpPost("personal")]
    [ProducesResponseType(typeof(ApiResponse<FamilyResponse>), 200)]
    public async Task<IActionResult> CreatePersonal()
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _familyService.CreatePersonalFamilyAsync(userId);
            return result.Success ? Ok(result) : StatusCode(result.Code, result);
        }
        catch (Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    // 2. API: Tạo gia đình (Cần nhập tên gia đình)
    [HttpPost("shared")]
    [ProducesResponseType(typeof(ApiResponse<FamilyResponse>), 200)]
    public async Task<IActionResult> CreateShared([FromBody] CreateSharedFamilyRequest request)
        {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _familyService.CreateSharedFamilyAsync(userId, request);
            return result.Success ? Ok(result) : StatusCode(result.Code, result);
        }
        catch (Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });

        }
    }
    // 3. API: Lấy danh sách (Sẽ trả về cả 2 loại, FE tự filter dựa vào field 'type')
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FamilyResponse>>), 200)]
    public async Task<IActionResult> GetMyFamilies()
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _familyService.GetMyFamiliesAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });

        }
    }
 
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FamilyResponse>), 200)]
    public async Task<IActionResult> GetFamilyById(Guid id)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _familyService.GetFamilyByIdAsync(id, userId);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
       
    }

    // PUT: api/v1/families/{id}
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FamilyResponse>), 200)]
    public async Task<IActionResult> UpdateFamily(Guid id, [FromBody] UpdateFamilyRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _familyService.UpdateFamilyAsync(id, userId, request);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }

    }

    // DELETE: api/v1/families/{id}
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> DeleteFamily(Guid id)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var result = await _familyService.DeleteFamilyAsync(id, userId);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    [HttpGet("{id}/subscription")]
    [ProducesResponseType(typeof(ApiResponse<FamilySubscriptionResponse>), 200)]
    public async Task<IActionResult> GetMyFamilySubscription(Guid id)
    {
        try
        {
            var result = await _familyService.GetFamilySubscriptionAsync(id);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }
}