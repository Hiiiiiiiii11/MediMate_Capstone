using MediMateRepository.Model;
using MediMateRepository.Model.Mock;
using Share.Constants;

namespace MediMateRepository.Repositories.Implementations
{
    public class MockDoctorRepository : IMockDoctorRepository
    {
        public Task<List<Doctors>> GetAllDoctorsAsync()
        {
            return Task.FromResult(DoctorMockData.Doctors.ToList());
        }

        public Task<List<Doctors>> GetPublicDoctorsAsync()
        {
            var list = DoctorMockData.Doctors
                .Where(d => string.Equals(d.Status, DoctorStatuses.Approved, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult(list);
        }

        public Task<Doctors?> GetDoctorByIdAsync(Guid doctorId)
        {
            return Task.FromResult(DoctorMockData.Doctors.FirstOrDefault(d => d.DoctorId == doctorId));
        }

        public Task<Doctors?> GetPublicDoctorByIdAsync(Guid doctorId)
        {
            return Task.FromResult(DoctorMockData.Doctors.FirstOrDefault(d =>
                d.DoctorId == doctorId && string.Equals(d.Status, DoctorStatuses.Approved, StringComparison.OrdinalIgnoreCase)));
        }

        public Task AddDoctorAsync(Doctors doctor)
        {
            DoctorMockData.Doctors.Add(doctor);
            return Task.CompletedTask;
        }

        public Task UpdateDoctorAsync(Doctors doctor)
        {
            return Task.CompletedTask;
        }

        public Task<List<DoctorAvailability>> GetAvailabilityByDoctorIdAsync(Guid doctorId)
        {
            return Task.FromResult(DoctorMockData.Availability.Where(a => a.DoctorId == doctorId).ToList());
        }

        public Task<List<DoctorAvailability>> GetAllAvailabilityAsync()
        {
            return Task.FromResult(DoctorMockData.Availability.ToList());
        }

        public Task<DoctorAvailability?> GetAvailabilityByIdAsync(Guid doctorId, Guid availabilityId)
        {
            var item = DoctorMockData.Availability.FirstOrDefault(a =>
                a.DoctorId == doctorId && a.DoctorAvailabilityId == availabilityId);
            return Task.FromResult(item);
        }

        public Task AddAvailabilityAsync(DoctorAvailability availability)
        {
            DoctorMockData.Availability.Add(availability);
            return Task.CompletedTask;
        }

        public Task UpdateAvailabilityAsync(DoctorAvailability availability)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAvailabilityAsync(DoctorAvailability availability)
        {
            DoctorMockData.Availability.Remove(availability);
            return Task.CompletedTask;
        }

        public Task<List<DoctorAvailabilityExceptions>> GetExceptionsByDoctorIdAsync(Guid doctorId)
        {
            return Task.FromResult(DoctorMockData.Exceptions.Where(e => e.DoctorId == doctorId).ToList());
        }

        public Task<List<DoctorAvailabilityExceptions>> GetExceptionsByDoctorAndDateAsync(Guid doctorId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            var list = DoctorMockData.Exceptions.Where(e => e.DoctorId == doctorId && DateOnly.FromDateTime(e.Date) == dateOnly).ToList();
            return Task.FromResult(list);
        }
    }
}
