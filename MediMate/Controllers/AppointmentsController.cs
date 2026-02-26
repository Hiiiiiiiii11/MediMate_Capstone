using MediMate.Models.Appointments;
using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;

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

        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request)
        {
            var userId = _currentUserService.UserId;
            var data = await _appointmentService.CreateAppointmentAsync(userId, new CreateAppointmentDto
            {
                DoctorId = request.DoctorId,
                MemberId = request.MemberId,
                AvailabilityId = request.AvailabilityId,
                AppointmentDate = request.AppointmentDate,
                IsPremiumUser = request.IsPremiumUser
            });

            return Ok(ApiResponse<AppointmentResponse>.Ok(MapAppointmentResponse(data), "Đặt lịch khám thành công."));
        }

        [HttpPut("{appointmentId}/cancel")]
        public async Task<IActionResult> CancelAppointment(Guid appointmentId, [FromBody] CancelAppointmentRequest request)
        {
            var userId = _currentUserService.UserId;
            var data = await _appointmentService.CancelAppointmentAsync(appointmentId, userId, new CancelAppointmentDto
            {
                Reason = request.Reason
            });

            return Ok(ApiResponse<AppointmentResponse>.Ok(MapAppointmentResponse(data), "Hủy lịch hẹn thành công."));
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments()
        {
            var userId = _currentUserService.UserId;
            var data = await _appointmentService.GetAppointmentsAsync(userId);
            var response = data.Select(MapAppointmentResponse).ToList();
            return Ok(ApiResponse<List<AppointmentResponse>>.Ok(response, "Lấy danh sách lịch hẹn thành công."));
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
