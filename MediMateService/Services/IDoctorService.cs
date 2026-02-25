using MediMateService.DTOs;

namespace MediMateService.Services
{
    public interface IDoctorService
    {
        Task<List<DoctorDto>> GetPublicDoctorsAsync(string? specialty = null);
        Task<DoctorDto> GetPublicDoctorByIdAsync(Guid doctorId);
        Task<List<DoctorAvailabilityDto>> GetPublicAvailabilityByDoctorAsync(Guid doctorId);

        Task<List<DoctorDto>> GetDoctorsAsync(string? specialty = null);
        Task<DoctorDto> GetDoctorByIdAsync(Guid doctorId);
        Task<List<DoctorAvailabilityDto>> GetAvailabilityByDoctorAsync(Guid doctorId);
        Task<List<DoctorAvailabilityExceptionDto>> GetExceptionsByDoctorAsync(Guid doctorId);

        Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto request);
        Task<DoctorDto> UpdateDoctorAsync(Guid doctorId, UpdateDoctorDto request);
        Task<DoctorDto> ChangeDoctorStatusAsync(Guid doctorId, ChangeDoctorStatusDto request);
        Task<DoctorDto> VerifyDoctorLicenseAsync(Guid doctorId, VerifyDoctorLicenseDto request);
        Task<DoctorDto> ApproveDoctorAsync(Guid doctorId, ApproveDoctorDto request);
        Task<DoctorAvailabilityDto> AddAvailabilityAsync(Guid doctorId, CreateDoctorAvailabilityDto request);
        Task<DoctorAvailabilityDto> UpdateAvailabilityAsync(Guid doctorId, Guid availabilityId, UpdateDoctorAvailabilityDto request);
        Task DeleteAvailabilityAsync(Guid doctorId, Guid availabilityId);
    }
}
