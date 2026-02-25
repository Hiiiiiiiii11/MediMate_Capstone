using MediMateRepository.Model;
using MediMateRepository.Model.Mock;

namespace MediMateRepository.Repositories.Implementations
{
    public class MockDoctorRepository : IMockDoctorRepository
    {
        public Task<List<Doctors>> GetAllDoctorsAsync()
        {
            return Task.FromResult(DoctorMockData.Doctors.ToList());
        }

        public Task<Doctors?> GetDoctorByIdAsync(Guid doctorId)
        {
            return Task.FromResult(DoctorMockData.Doctors.FirstOrDefault(d => d.DoctorId == doctorId));
        }

        public Task<List<DoctorAvailability>> GetAvailabilityByDoctorIdAsync(Guid doctorId)
        {
            return Task.FromResult(DoctorMockData.Availability.Where(a => a.DoctorId == doctorId).ToList());
        }

        public Task<List<DoctorAvailability>> GetAllAvailabilityAsync()
        {
            return Task.FromResult(DoctorMockData.Availability.ToList());
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
