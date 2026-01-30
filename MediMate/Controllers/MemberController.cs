using MediMateService.DTOs;
using MediMateService.Services;
using MediMateService.Services.MediMateService.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediMate.Controllers
{
    [Route("api/v1")]
    [ApiController]
    public class MemberController : ControllerBase
    {
        private readonly IMemberService _memberService;
        private readonly ICurrentUserService _currentUserService;

        public MemberController(IMemberService memberService, ICurrentUserService currentUserService)
        {
            _memberService = memberService;
            _currentUserService = currentUserService;
        }

        // API: Tạo hồ sơ phụ thuộc -> Nhận QR
        [HttpPost("init-dependent")]
        public async Task<IActionResult> InitDependent([FromBody] InitDependentRequest request)
        {
            try
            {
                var result = await _memberService.InitDependentProfileAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log lỗi ex ở đây nếu cần
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("family/{familyId}")]
        public async Task<IActionResult> GetMembersByFamily(Guid familyId)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _memberService.GetMembersByFamilyIdAsync(familyId, userId);
                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log lỗi ex ở đây nếu cần
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
            
        }

        // GET: api/v1/members/{id} -> Chi tiết 1 thành viên
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMemberById(Guid id)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _memberService.GetMemberByIdAsync(id, userId);
                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log lỗi ex ở đây nếu cần
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
                
        }

        // PUT: api/v1/members/{id} -> Sửa thông tin
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMember(Guid id, [FromBody] UpdateMemberRequest request)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _memberService.UpdateMemberAsync(id, userId, request);
                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log lỗi ex ở đây nếu cần
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // DELETE: api/v1/members/{id} -> Xóa/Rời nhóm
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveMember(Guid id)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _memberService.RemoveMemberAsync(id, userId);
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
}
