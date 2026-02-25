using MediMateRepository.Model;

namespace MediMateRepository.Repositories
{
    public interface IMockDoctorRepository
    {
        Task<List<Doctors>> GetAllDoctorsAsync();
        Task<Doctors?> GetDoctorByIdAsync(Guid doctorId);
        Task<List<DoctorAvailability>> GetAvailabilityByDoctorIdAsync(Guid doctorId);
        Task<List<DoctorAvailability>> GetAllAvailabilityAsync();
        Task<List<DoctorAvailabilityExceptions>> GetExceptionsByDoctorIdAsync(Guid doctorId);
        Task<List<DoctorAvailabilityExceptions>> GetExceptionsByDoctorAndDateAsync(Guid doctorId, DateTime date);
    }
}
