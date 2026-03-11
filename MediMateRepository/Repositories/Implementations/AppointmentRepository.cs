using MediMateRepository.Data;
using MediMateRepository.Model;
using Microsoft.EntityFrameworkCore;

namespace MediMateRepository.Repositories.Implementations
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly MediMateDbContext _context;

        public AppointmentRepository(MediMateDbContext context)
        {
            _context = context;
        }

        public async Task AddAppointmentAsync(Appointments appointment)
        {
            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();
        }

        public async Task<Appointments?> GetAppointmentByIdAsync(Guid appointmentId)
        {
            return await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
        }

        public async Task<List<Appointments>> GetAppointmentsByMemberIdAsync(Guid memberId)
        {
            return await _context.Appointments
                .Where(a => a.MemberId == memberId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task<List<Appointments>> GetAppointmentsByDoctorIdAsync(Guid doctorId)
        {
            return await _context.Appointments
                .Where(a => a.DoctorId == doctorId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task UpdateAppointmentAsync(Appointments appointment)
        {
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();
        }

        public async Task<ConsultationSessions?> GetSessionByAppointmentIdAsync(Guid appointmentId)
        {
            return await _context.ConsultationSessions
                .FirstOrDefaultAsync(s => s.AppointmentId == appointmentId);
        }

        public async Task<ConsultationSessions?> GetSessionByIdAsync(Guid sessionId)
        {
            return await _context.ConsultationSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        public async Task AddSessionAsync(ConsultationSessions session)
        {
            await _context.ConsultationSessions.AddAsync(session);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSessionAsync(ConsultationSessions session)
        {
            _context.ConsultationSessions.Update(session);
            await _context.SaveChangesAsync();
        }
    }
}
