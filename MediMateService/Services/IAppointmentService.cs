using MediMateService.DTOs;

namespace MediMateService.Services
{
    public interface IAppointmentService
    {
        Task<AppointmentDto> CreateAppointmentAsync(Guid userId, CreateAppointmentDto request);
        Task<AppointmentDto> CancelAppointmentAsync(Guid appointmentId, Guid userId, CancelAppointmentDto request);
        Task<List<AppointmentDto>> GetAppointmentsAsync(Guid userId);
    }
}
