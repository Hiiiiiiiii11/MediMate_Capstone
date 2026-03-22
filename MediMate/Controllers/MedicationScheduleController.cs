using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediMateService.DTOs;
using MediMateService.Services;
using System;
using System.Threading.Tasks;
using Share.Common;

namespace MediMate.Controllers
{
    [Route("api/v1")]
    [ApiController]
    [Authorize] // Bắt buộc phải có JWT Token (User hoặc Dependent đều được)
    public class MedicationSchedulesController : ControllerBase
    {
        private readonly IMedicationSchedulesService _scheduleService;
        private readonly ICurrentUserService _currentUserService;

        public MedicationSchedulesController(
            IMedicationSchedulesService scheduleService,
            ICurrentUserService currentUserService
            )
        {
            _scheduleService = scheduleService;
            _currentUserService = currentUserService;

        }

        // ==========================================
        // QUẢN LÝ LỊCH (SCHEDULES)
        // ==========================================

        //hàm này sẽ tự động nhận diện caller là User hay Dependent dựa vào claim trong token, nên không cần truyền memberId hay familyId qua query nữa

        [HttpPost("members/{memberId}/schedules")]
        [ProducesResponseType(typeof(ApiResponse<ScheduleResponse>), 200)]
        public async Task<IActionResult> CreateSchedule(Guid memberId, [FromBody] CreateScheduleRequest request)
        {
            try
            {
                var callerId = _currentUserService.UserId;
                var result = await _scheduleService.CreateScheduleAsync(memberId, callerId, request);

                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("members/{memberId}/schedules")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ScheduleResponse>>), 200)]
        public async Task<IActionResult> GetMemberSchedules(Guid memberId)
        {
            try
            {
                var callerId = _currentUserService.UserId;
                var result = await _scheduleService.GetMemberSchedulesAsync(memberId, callerId);
                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("families/{familyId}/schedules")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ScheduleResponse>>), 200)]
        public async Task<IActionResult> GetFamilySchedules(Guid familyId)
        {
            try
            {
                var callerId = _currentUserService.UserId;
                var result = await _scheduleService.GetFamilySchedulesAsync(familyId, callerId);
                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("schedules/{scheduleId}")]
        [ProducesResponseType(typeof(ApiResponse<ScheduleResponse>), 200)]
        public async Task<IActionResult> UpdateSchedule(Guid scheduleId, [FromBody] UpdateScheduleRequest request)
        {
            try
            {
                var callerId = _currentUserService.UserId;
                var result = await _scheduleService.UpdateScheduleAsync(scheduleId, callerId, request);

                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpDelete("schedules/{scheduleId}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> DeleteSchedule(Guid scheduleId)
        {
            try
            {
                var callerId = _currentUserService.UserId;
                var result = await _scheduleService.DeleteScheduleAsync(scheduleId, callerId);

                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // ==========================================
        // QUẢN LÝ NHẮC NHỞ (REMINDERS) & LOGS
        // ==========================================

        //[HttpGet("reminders/my-daily")]
        //public async Task<IActionResult> GetMyDailyReminders([FromQuery] DateTime date)
        //{
        //    try
        //    {
        //        var callerId = _currentUserService.UserId;

        //        // Vì tự xem của mình, memberId và currentUserId đều là callerId
        //        var result = await _scheduleService.GetDailyRemindersAsync(callerId, callerId, date);

        //        if (!result.Success) return StatusCode(result.Code, result);
        //        return Ok(result);

        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
        //    }
        //}
        [HttpGet("members/{memberId}/reminders/daily")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReminderDailyResponse>>), 200)]
        public async Task<IActionResult> GetDailyReminders(Guid memberId, [FromQuery] DateTime date)
        {
            var callerId = _currentUserService.UserId; // Có thể là User(Bố/Mẹ)
            var result = await _scheduleService.GetDailyRemindersAsync(memberId, callerId, date);

            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }

        [HttpGet("families/{familyId}/reminders/daily")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReminderDailyResponse>>), 200)]
        public async Task<IActionResult> GetFamilyDailyReminders(Guid familyId, [FromQuery] DateTime date)
        {
            try
            {
                var callerId = _currentUserService.UserId;
                var result = await _scheduleService.GetFamilyDailyRemindersAsync(familyId, callerId, date);

                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("reminders/{reminderId}/action")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> MarkReminderAction(Guid reminderId, [FromBody] MedicationActionRequest request)
        {
            try
            {
                var callerId = _currentUserService.UserId;
                var result = await _scheduleService.MarkReminderActionAsync(reminderId, callerId, request);

                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}