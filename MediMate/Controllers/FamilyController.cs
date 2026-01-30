using MediMateService.DTOs;
using MediMateService.Services;
using MediMateService.Services.MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [HttpPost("add-member-qr")]
    public async Task<IActionResult> AddMemberByQr([FromBody] AddMemberByIdentityRequest request)
    {
        try {
            var ownerId = _currentUserService.UserId;
            var result = await _memberService.AddMemberByIdentityQrAsync(ownerId, request);

            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
        catch(Exception ex)
        {
            // Log lỗi ex ở đây nếu cần
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        }

        
    }
    [HttpGet("{id}")]
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
}