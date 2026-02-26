using MediMateRepository.Model;
using MediMateRepository.Model.Mock;

namespace MediMateRepository.Repositories.Implementations
{
    public class MockAppointmentRepository : IMockAppointmentRepository
    {
        public Task AddAppointmentAsync(Appointments appointment)
        {
            RatingMockData.Appointments.Add(appointment);
            return Task.CompletedTask;
        }

        public Task<Appointments?> GetAppointmentByIdAsync(Guid appointmentId)
        {
            var appointment = RatingMockData.Appointments.FirstOrDefault(a => a.AppointmentId == appointmentId);
            return Task.FromResult(appointment);
        }

        public Task<List<Appointments>> GetAppointmentsByMemberIdAsync(Guid memberId)
        {
            var list = RatingMockData.Appointments
                .Where(a => a.MemberId == memberId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();
            return Task.FromResult(list);
        }

        public Task<List<Appointments>> GetAppointmentsByDoctorIdAsync(Guid doctorId)
        {
            var list = RatingMockData.Appointments
                .Where(a => a.DoctorId == doctorId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();
            return Task.FromResult(list);
        }

        public Task UpdateAppointmentAsync(Appointments appointment)
        {
            return Task.CompletedTask;
        }

        public Task<ConsultationSessions?> GetSessionByAppointmentIdAsync(Guid appointmentId)
        {
            var session = RatingMockData.Sessions.FirstOrDefault(s => s.AppointmentId == appointmentId);
            return Task.FromResult(session);
        }

        public Task<ConsultationSessions?> GetSessionByIdAsync(Guid sessionId)
        {
            var session = RatingMockData.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
            return Task.FromResult(session);
        }

        public Task AddSessionAsync(ConsultationSessions session)
        {
            RatingMockData.Sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task UpdateSessionAsync(ConsultationSessions session)
        {
            return Task.CompletedTask;
        }
    }
}
