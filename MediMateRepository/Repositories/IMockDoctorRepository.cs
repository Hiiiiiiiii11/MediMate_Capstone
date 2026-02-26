using MediMateRepository.Model;

namespace MediMateRepository.Repositories
{
    public interface IMockDoctorRepository
    {
        Task<List<Doctors>> GetAllDoctorsAsync();
        Task<List<Doctors>> GetPublicDoctorsAsync();
        Task<Doctors?> GetDoctorByIdAsync(Guid doctorId);
        Task<Doctors?> GetPublicDoctorByIdAsync(Guid doctorId);
        Task AddDoctorAsync(Doctors doctor);
        Task UpdateDoctorAsync(Doctors doctor);
        Task<List<DoctorAvailability>> GetAvailabilityByDoctorIdAsync(Guid doctorId);
        Task<List<DoctorAvailability>> GetAllAvailabilityAsync();
        Task<DoctorAvailability?> GetAvailabilityByIdAsync(Guid doctorId, Guid availabilityId);
        Task AddAvailabilityAsync(DoctorAvailability availability);
        Task UpdateAvailabilityAsync(DoctorAvailability availability);
        Task DeleteAvailabilityAsync(DoctorAvailability availability);
        Task<List<DoctorAvailabilityExceptions>> GetExceptionsByDoctorIdAsync(Guid doctorId);
        Task<List<DoctorAvailabilityExceptions>> GetExceptionsByDoctorAndDateAsync(Guid doctorId, DateTime date);
    }
}
