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
        [ProducesResponseType(typeof(ApiResponse<AppointmentPaymentResponseDto>), 201)]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request)
        {
            try
            {
                // 1. Lấy UserId từ Token JWT
                var userId = _currentUserService.UserId;

                // 2. Chuyển đổi Request từ API sang DTO của Service
                var createDto = new CreateAppointmentDto
                {
                    DoctorId = request.DoctorId,
                    MemberId = request.MemberId,
                    AvailabilityId = request.AvailabilityId,
                    AppointmentDate = request.AppointmentDate,
                    AppointmentTime = request.AppointmentTime, 
                };

                // 3. Gọi Service xử lý nghiệp vụ (Kiểm tra trùng lịch, trừ lượt, v.v.)
                var result = await _appointmentService.CreateAppointmentAsync(userId, createDto);

                return StatusCode(201, ApiResponse<AppointmentPaymentResponseDto>.Ok(result, "Tạo đơn đặt lịch thành công. Vui lòng thanh toán để xác nhận."));
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

        [HttpGet("members/me")]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentResponse>>), 200)]
        public async Task<IActionResult> GetAppointmentsByCurrentMember()
        {
            var userId = _currentUserService.UserId;
            var result = await _appointmentService.GetAppointmentsByMemberIdAsync(userId);
            return Ok(ApiResponse<List<AppointmentDto>>.Ok(result, "Lấy danh sách lịch hẹn của thành viên thành công."));
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
                var userId = _currentUserService.UserId;

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

        [HttpGet("detail/{appointmentId}")]
        [ProducesResponseType(typeof(ApiResponse<AppointmentDetailDto>), 200)]
        public async Task<IActionResult> GetAppointmentDetail(Guid appointmentId)
        {
            try
            {
                var result = await _appointmentService.GetAppointmentDetailAsync(appointmentId);
                if (!result.Success) return StatusCode(result.Code, result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<AppointmentDetailDto>.Fail($"Lỗi hệ thống: {ex.Message}", 500));
            }
        }

        private static AppointmentResponse MapAppointmentResponse(AppointmentDto dto)
        {
            return new AppointmentResponse
            {
                AppointmentId = dto.AppointmentId,
                DoctorId = dto.DoctorId,
                ClinicId = dto.ClinicId,
                MemberId = dto.MemberId,
                MemberName = dto.MemberName,
                AvailabilityId = dto.AvailabilityId,
                AppointmentDate = dto.AppointmentDate,
                AppointmentTime = dto.AppointmentTime,
                Status = dto.Status,
                PaymentStatus = dto.PaymentStatus,
                CancelReason = dto.CancelReason,
                CreatedAt = dto.CreatedAt
            };
        }

        // ─────────────────────────────────────────────────────────────────
        // REFUND MANAGEMENT (ADMIN)
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Admin: Lấy danh sách các lịch hẹn cần hoàn tiền (PaymentStatus == "Refunded").</summary>
        [HttpGet("refunds")]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentDto>>), 200)]
        public async Task<IActionResult> GetRefundableAppointments()
        {
            try
            {
                var result = await _appointmentService.GetRefundableAppointmentsAsync();
                return Ok(ApiResponse<List<AppointmentDto>>.Ok(result, "Lấy danh sách cần hoàn tiền thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<AppointmentDto>>.Fail($"Lỗi hệ thống: {ex.Message}", 500));
            }
        }
        [HttpPut("{appointmentId:guid}/update-payment-status")]
        [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), 200)]
        public async Task<IActionResult> UpdatePaymentStatus(Guid appointmentId, [FromBody] UpdatePaymentStatusRequest request)
        {
            try
            {
                var result = await _appointmentService.UpdateAppointmentPaymentStatusAsync(appointmentId, request.Status);
                return Ok(ApiResponse<AppointmentDto>.Ok(result, "Cập nhật trạng thái thanh toán thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<AppointmentDto>.Fail($"Lỗi hệ thống: {ex.Message}", 500));
            }
        }

        /// <summary>Admin: Đánh dấu đã hoàn tất hoàn tiền (chuyển trạng thái sang "RefundCompleted"), kèm ảnh chứng minh chuyển khoản.</summary>
        [HttpPut("{appointmentId:guid}/complete-refund")]
        [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), 200)]
        public async Task<IActionResult> CompleteRefund(Guid appointmentId, [FromForm] IFormFile? transferImage = null)
        {
            try
            {
                var result = await _appointmentService.CompleteRefundAsync(appointmentId, transferImage);
                return Ok(ApiResponse<AppointmentDto>.Ok(result, "Đã cập nhật trạng thái hoàn tiền thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<AppointmentDto>.Fail($"Lỗi hệ thống: {ex.Message}", 500));
            }
        }
    }
}
