using MediMateRepository.Model;
using MediMateRepository.Model.Mock;

namespace MediMateRepository.Repositories
{
    public class MockDoctorRepository : IMockDoctorRepository
    {
        public Task<List<Doctors>> GetAllDoctorsAsync() => Task.FromResult(DoctorMockData.Doctors.ToList());

        public Task<Doctors?> GetDoctorByIdAsync(Guid doctorId) => Task.FromResult(DoctorMockData.Doctors.FirstOrDefault(d => d.DoctorId == doctorId));

        public Task<List<DoctorAvailability>> GetAvailabilityByDoctorIdAsync(Guid doctorId) => Task.FromResult(DoctorMockData.Availability.Where(a => a.DoctorId == doctorId).ToList());

        public Task<List<DoctorAvailability>> GetAllAvailabilityAsync() => Task.FromResult(DoctorMockData.Availability.ToList());

        public Task<List<DoctorAvailabilityExceptions>> GetExceptionsByDoctorIdAsync(Guid doctorId) => Task.FromResult(DoctorMockData.Exceptions.Where(e => e.DoctorId == doctorId).ToList());

        public Task<List<DoctorAvailabilityExceptions>> GetExceptionsByDoctorAndDateAsync(Guid doctorId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            var list = DoctorMockData.Exceptions.Where(e => e.DoctorId == doctorId && DateOnly.FromDateTime(e.Date) == dateOnly).ToList();
            return Task.FromResult(list);
        }
    }
}
