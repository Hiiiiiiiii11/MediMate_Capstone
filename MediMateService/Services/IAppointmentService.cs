using MediMateService.DTOs;
using Share.Common;

namespace MediMateService.Services
{
    public interface IAppointmentService
    {
        Task<AppointmentDto> CreateAppointmentAsync(Guid userId, CreateAppointmentDto request);
        Task<AppointmentDto> CancelAppointmentAsync(Guid appointmentId, Guid userId, CancelAppointmentDto request);
        Task<List<AppointmentDto>> GetAppointmentsAsync(Guid userId);
        Task<ApiResponse<List<AvailableSlotDto>>> GetAvailableSlotsAsync(Guid doctorId, DateTime date);
        Task<AppointmentDto> UpdateAppointmentAsync(Guid appointmentId, Guid userId, UpdateAppointmentDto request);
        Task<List<AppointmentDto>> GetAppointmentsByDoctorIdAsync(Guid doctorId);
    }
}
