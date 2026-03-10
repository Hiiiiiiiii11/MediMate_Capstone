using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediMate.Controllers
{
    [Route("api/v1/")]
    [ApiController]
    public class FamilyActivityLogController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;
        private readonly ICurrentUserService _currentUserService;

        public FamilyActivityLogController(IActivityLogService activityLogService, ICurrentUserService currentUserService1)
        {
            _activityLogService = activityLogService;
            _currentUserService = currentUserService1;
        }
        [Authorize]
        [HttpGet("families/{familyId}/activity-logs")]
        public async Task<IActionResult> GetFamilyActivityLogs(Guid familyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _activityLogService.GetFamilyActivitiesAsync(familyId, userId, page, pageSize);
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
