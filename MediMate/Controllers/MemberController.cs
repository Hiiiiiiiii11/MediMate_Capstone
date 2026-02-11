using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediMate.Controllers
{
    [Route("api/v1/members")]
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

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAllMembers()
        {
            try
            {
                var result = await _memberService.GetAllMember();
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log lỗi ex ở đây nếu cần
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost("join-by-joincode")]
        [AllowAnonymous] // Mở cửa cho tất cả
        public async Task<IActionResult> JoinFamily([FromBody] JoinFamilyRequest request)
        {
            Guid? userId = null;

            // Tự check Token thủ công (vì AllowAnonymous sẽ bỏ qua Authorize Middleware)
            try
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    // Lấy Claim thủ công để an toàn
                    var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (Guid.TryParse(userIdString, out var parsedId)) userId = parsedId;
                }
            }
            catch
            {
                // Không có token hoặc token lỗi -> Coi như là Guest (Dependent)
                userId = null;
            }

            var result = await _memberService.JoinFamilyUnifiedAsync(userId, request);

            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
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

        [HttpGet("{memberId}/qr/viewQr")]
        public async Task<IActionResult> GetMemberQr(Guid memberId)
        {
            // userId lấy từ Token, dùng để check quyền nếu cần

            var result = await _memberService.GetIdentityQrAsync(memberId);

            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
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
        public async Task<IActionResult> UpdateMember(Guid id, [FromForm] UpdateMemberRequest request)
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
