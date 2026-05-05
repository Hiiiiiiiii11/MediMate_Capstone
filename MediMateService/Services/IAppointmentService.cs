using MediMateService.DTOs;
using Microsoft.AspNetCore.Http;
using Share.Common;

namespace MediMateService.Services
{
    public interface IAppointmentService
    {
        Task<AppointmentPaymentResponseDto> CreateAppointmentAsync(Guid userId, CreateAppointmentDto request);
        Task<AppointmentDto> CancelAppointmentAsync(Guid appointmentId, Guid userId, CancelAppointmentDto request);
        Task<List<AppointmentDto>> GetAppointmentsAsync(Guid userId);
        Task<List<AppointmentDto>> GetAppointmentsByDoctorUserIdAsync(Guid userId);
        Task<ApiResponse<List<AvailableSlotDto>>> GetAvailableSlotsAsync(Guid doctorId, DateTime date);
        Task<AppointmentDto> UpdateAppointmentAsync(Guid appointmentId, Guid userId, UpdateAppointmentDto request);
        Task<List<AppointmentDto>> GetAppointmentsByDoctorIdAsync(Guid doctorId);
        Task<ApiResponse<AppointmentDetailDto>> GetAppointmentDetailAsync(Guid appointmentId);
        Task<List<AppointmentDto>> GetAppointmentsByMemberIdAsync(Guid memberId);

        // Refund Management cho Admin
        Task<List<AppointmentDto>> GetRefundableAppointmentsAsync();
        Task<AppointmentDto> CompleteRefundAsync(Guid appointmentId, IFormFile? transferImage);

        /// <summary>
        /// Webhook PayOS gọi sau khi thanh toán thành công.
        /// Cập nhật PaymentStatus = "Paid" và tự động sinh DoctorPayout (Hold).
        /// </summary>
        Task<AppointmentDto> UpdateAppointmentPaymentStatusAsync(Guid appointmentId, string paymentStatus);
        Task CheckAndCancelUnpaidAppointmentAsync(Guid appointmentId);
        Task DeleteUnpaidAppointmentAsync(Guid appointmentId);
    }
}
