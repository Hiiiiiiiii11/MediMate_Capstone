using MediMateRepository.Data;
using MediMateRepository.Model;
using Microsoft.EntityFrameworkCore;
using Share.Constants;

namespace MediMateRepository.Repositories.Implementations
{
    public class DoctorRepository : IDoctorRepository
    {
        private readonly MediMateDbContext _context;

        public DoctorRepository(MediMateDbContext context)
        {
            _context = context;
        }

        public async Task<List<Doctors>> GetAllDoctorsAsync()
        {
            return await _context.Doctors.Include(d => d.User).ToListAsync();
        }

        public async Task<List<Doctors>> GetPublicDoctorsAsync()
        {
            return await _context.Doctors
                .Include(d => d.User)
                .Where(d => d.Status == DoctorStatuses.Active)
                .ToListAsync();
        }

        public async Task<Doctors?> GetDoctorByIdAsync(Guid doctorId)
        {
            return await _context.Doctors.Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId);
        }

        public async Task<Doctors?> GetPublicDoctorByIdAsync(Guid doctorId)
        {
            return await _context.Doctors.Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId && d.Status == DoctorStatuses.Active);
        }

        public async Task AddDoctorAsync(Doctors doctor)
        {
            await _context.Doctors.AddAsync(doctor);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateDoctorAsync(Doctors doctor)
        {
            _context.Doctors.Update(doctor);
            await _context.SaveChangesAsync();
        }

        public async Task<List<DoctorAvailability>> GetAvailabilityByDoctorIdAsync(Guid doctorId)
        {
            return await _context.DoctorAvailabilities
                .Where(a => a.DoctorId == doctorId)
                .ToListAsync();
        }

        public async Task<List<DoctorAvailability>> GetAllAvailabilityAsync()
        {
            return await _context.DoctorAvailabilities.ToListAsync();
        }

        public async Task<DoctorAvailability?> GetAvailabilityByIdAsync(Guid doctorId, Guid availabilityId)
        {
            return await _context.DoctorAvailabilities
                .FirstOrDefaultAsync(a => a.DoctorId == doctorId && a.DoctorAvailabilityId == availabilityId);
        }

        public async Task AddAvailabilityAsync(DoctorAvailability availability)
        {
            await _context.DoctorAvailabilities.AddAsync(availability);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAvailabilityAsync(DoctorAvailability availability)
        {
            _context.DoctorAvailabilities.Update(availability);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAvailabilityAsync(DoctorAvailability availability)
        {
            _context.DoctorAvailabilities.Remove(availability);
            await _context.SaveChangesAsync();
        }

 

        public async Task<List<DoctorAvailabilityExceptions>> GetExceptionsByDoctorIdAsync(Guid doctorId)
        {
            return await _context.Set<DoctorAvailabilityExceptions>()
                .Where(e => e.DoctorId == doctorId)
                .ToListAsync();
        }

        public async Task<List<DoctorAvailabilityExceptions>> GetExceptionsByDoctorAndDateAsync(Guid doctorId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            return await _context.Set<DoctorAvailabilityExceptions>()
                .Where(e => e.DoctorId == doctorId && DateOnly.FromDateTime(e.Date) == dateOnly)
                .ToListAsync();
        }
    }
}
