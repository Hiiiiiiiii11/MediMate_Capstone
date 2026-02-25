using MediMateService.DTOs;

namespace MediMateService.Services
{
    public interface IDoctorService
    {
        Task<List<DoctorDto>> GetDoctorsAsync(string? specialty = null);
        Task<DoctorDto> GetDoctorByIdAsync(Guid doctorId);
        Task<List<DoctorAvailabilityDto>> GetAvailabilityByDoctorAsync(Guid doctorId);
        Task<List<DoctorAvailabilityExceptionDto>> GetExceptionsByDoctorAsync(Guid doctorId);
    }
}
