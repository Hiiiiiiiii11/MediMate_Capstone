using MediMate.Models.Appointments;
using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using System.Security.Claims;

namespace MediMate.Controllers
{
    [Route("api/v1/appointments")]
    [ApiController]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ICurrentUserService _currentUserService;

        public AppointmentsController(IAppointmentService appointmentService, ICurrentUserService currentUserService)
        {
            _appointmentService = appointmentService;
            _currentUserService = currentUserService;
        }

        [HttpGet("doctors/{doctorId}/available-slots")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<AvailableSlotDto>>), 200)]
        public async Task<IActionResult> GetAvailableSlots(Guid doctorId, [FromQuery] DateTime date)
        {
            try
            {
                var response = await _appointmentService.GetAvailableSlotsAsync(doctorId, date);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), 201)]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request)
        {
            try
            {
                // 1. Lấy UserId từ Token JWT
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("Id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
                {
                    return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ hoặc đã hết hạn.", 401));
                }

                // 2. Chuyển đổi Request từ API sang DTO của Service
                var createDto = new CreateAppointmentDto
                {
                    DoctorId = request.DoctorId,
                    MemberId = request.MemberId,
                    AvailabilityId = request.AvailabilityId,
                    AppointmentDate = request.AppointmentDate,
                    AppointmentTime = request.AppointmentTime, // Map giờ khám vào đây

                };

                // 3. Gọi Service xử lý nghiệp vụ (Kiểm tra trùng lịch, trừ lượt, v.v.)
                var result = await _appointmentService.CreateAppointmentAsync(userId, createDto);

                return StatusCode(201, ApiResponse<AppointmentDto>.Ok(result, "Đặt lịch khám thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "L?i h? th?ng: " + ex.Message });
            }

        }
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentResponse>>), 200)]
        public async Task<IActionResult> GetAppointments()
        {
            var userId = _currentUserService.UserId;
            var data = await _appointmentService.GetAppointmentsAsync(userId);
            var response = data.Select(MapAppointmentResponse).ToList();
            return Ok(ApiResponse<List<AppointmentResponse>>.Ok(response, "Lấy danh sách lịch hẹn thành công."));
        }
        [HttpGet("doctors/{doctorId}")]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentDto>>), 200)]
        public async Task<IActionResult> GetAppointmentsByDoctor(Guid doctorId)
        {
            try
            {
                var result = await _appointmentService.GetAppointmentsByDoctorIdAsync(doctorId);
                return Ok(ApiResponse<List<AppointmentDto>>.Ok(result, "Lấy danh sách lịch khám của bác sĩ thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Lỗi hệ thống: " + ex.Message, 500));
            }
        }

        [HttpGet("doctors/me")]
        public async Task<IActionResult> GetAppointmentsByCurrentDoctor()
        {
            var userId = _currentUserService.UserId;
            var result = await _appointmentService.GetAppointmentsByDoctorUserIdAsync(userId);
            return Ok(ApiResponse<List<AppointmentDto>>.Ok(result, "Lấy danh sách lịch khám của bác sĩ thành công."));
        }

        [HttpPut("{appointmentId}/cancel")]
        [ProducesResponseType(typeof(ApiResponse<AppointmentResponse>), 200)]
        public async Task<IActionResult> CancelAppointment(Guid appointmentId, [FromBody] CancelAppointmentRequest request)
        {
            var userId = _currentUserService.UserId;
            var data = await _appointmentService.CancelAppointmentAsync(appointmentId, userId, new CancelAppointmentDto
            {
                Reason = request.Reason
            });

            return Ok(ApiResponse<AppointmentResponse>.Ok(MapAppointmentResponse(data), "Hủy lịch hẹn thành công."));
        }

       
        [HttpPut("{appointmentId}/status")]
        [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), 200)]
        public async Task<IActionResult> UpdateAppointmentStatus(Guid appointmentId, [FromBody] UpdateAppointmentRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("Id")?.Value;
                Guid userId = Guid.Parse(userIdClaim!);

                var updateDto = new UpdateAppointmentDto
                {
                    Status = request.Status
                };

                var result = await _appointmentService.UpdateAppointmentAsync(appointmentId, userId, updateDto);
                return Ok(ApiResponse<AppointmentDto>.Ok(result, "Cập nhật trạng thái lịch hẹn thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(400, ApiResponse<object>.Fail(ex.Message, 400));
            }
        }

        private static AppointmentResponse MapAppointmentResponse(AppointmentDto dto)
        {
            return new AppointmentResponse
            {
                AppointmentId = dto.AppointmentId,
                DoctorId = dto.DoctorId,
                MemberId = dto.MemberId,
                AvailabilityId = dto.AvailabilityId,
                AppointmentDate = dto.AppointmentDate,
                Status = dto.Status,
                CancelReason = dto.CancelReason,
                CreatedAt = dto.CreatedAt
            };
        }
    }
}
