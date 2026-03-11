using MediMateRepository.Model;

namespace MediMateRepository.Repositories
{
    public interface IAppointmentRepository
    {
        Task AddAppointmentAsync(Appointments appointment);
        Task<Appointments?> GetAppointmentByIdAsync(Guid appointmentId);
        Task<List<Appointments>> GetAppointmentsByMemberIdAsync(Guid memberId);
        Task<List<Appointments>> GetAppointmentsByDoctorIdAsync(Guid doctorId);
        Task UpdateAppointmentAsync(Appointments appointment);
        Task<ConsultationSessions?> GetSessionByAppointmentIdAsync(Guid appointmentId);
        Task<ConsultationSessions?> GetSessionByIdAsync(Guid sessionId);
        Task AddSessionAsync(ConsultationSessions session);
        Task UpdateSessionAsync(ConsultationSessions session);
    }
}
