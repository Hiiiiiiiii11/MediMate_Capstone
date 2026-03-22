using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;

namespace MediMate.Controllers
{
    [Route("api/v1/medicationlogs")]
    [ApiController]
    public class MedicationLogController : ControllerBase
    {
        private readonly IMedicationLogService _medicationLogService;
        private readonly ICurrentUserService _currentUserService;

        public MedicationLogController(IMedicationLogService medicationLogService, ICurrentUserService currentUserService)
        {
            _medicationLogService = medicationLogService;
            _currentUserService = currentUserService;
        }

        [Authorize]
        [HttpPost("action")]
        [ProducesResponseType(typeof(ApiResponse<MedicationLogResponse>), 200)]
        public async Task<IActionResult> LogMedicationAction([FromBody] LogMedicationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.Fail("Dữ liệu đầu vào không hợp lệ.", 400));
                }

                var userId = _currentUserService.UserId;

                var result = await _medicationLogService.LogMedicationActionAsync(request, userId);

                // Tùy thuộc vào thiết kế ApiResponse của bạn, ở đây trả về đúng Http Status Code
                if (!result.Success)
                {
                    return StatusCode(result.Code, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log lỗi ex ở đây nếu cần
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [Authorize]
        [HttpGet("member/{memberId}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<MedicationLogResponse>>), 200)]
        public async Task<IActionResult> GetMemberLogs(Guid memberId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _medicationLogService.GetMemberLogsAsync(memberId, userId, startDate, endDate);

                if (!result.Success) return StatusCode(result.Code, result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log lỗi ex ở đây nếu cần
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        /// <summary>
        /// Lấy lịch sử uống thuốc của TOÀN BỘ Gia đình (Dành cho Dashboard của Chủ hộ)
        /// </summary>
        [Authorize]
        [HttpGet("family/{familyId}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<MedicationLogResponse>>), 200)]
        public async Task<IActionResult> GetFamilyLogs(Guid familyId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _medicationLogService.GetFamilyLogsAsync(familyId, userId, startDate, endDate);

                if (!result.Success) return StatusCode(result.Code, result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log lỗi ex ở đây nếu cần
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Thống kê tỷ lệ tuân thủ (Adherence Rate) của một Đơn thuốc/Lịch thuốc
        /// </summary>
        [Authorize]
        [HttpGet("stats/{scheduleId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetAdherenceStats(Guid scheduleId)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _medicationLogService.GetAdherenceStatsAsync(scheduleId, userId);

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
