using MediMateService.DTOs;

namespace MediMateService.Services
{
    public interface IDoctorService
    {
        // Public endpoints
        Task<List<DoctorDto>> GetPublicDoctorsAsync(string? specialty = null);
        Task<DoctorDto> GetPublicDoctorByIdAsync(Guid doctorId);
        //Task<List<DoctorAvailabilityDto>> GetPublicAvailabilityByDoctorAsync(Guid doctorId);

        // Management - read
        Task<List<DoctorDto>> GetDoctorsAsync(string? specialty = null, string? status = null);
        Task<DoctorDto> GetDoctorByIdAsync(Guid doctorId);
        //Task<List<DoctorAvailabilityDto>> GetAvailabilityByDoctorAsync(Guid doctorId);
        //Task<List<DoctorAvailabilityExceptionDto>> GetExceptionsByDoctorAsync(Guid doctorId);

        // Admin: tạo hồ sơ bác sĩ (Inactive)
        Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto request);

        // Doctor: tự xem và cập nhật hồ sơ (dựa vào UserId từ token)
        Task<DoctorDto> GetMyProfileAsync(Guid userId);
        Task<DoctorDto> UpdateMyProfileAsync(Guid userId, UpdateDoctorDto request);

        // Status transitions
        Task<DoctorDto> SubmitPendingAsync(Guid doctorId, SubmitDoctorDto dto);  // Inactive → Pending
        Task<DoctorDto> VerifyDoctorAsync(Guid doctorId);                         // Pending → Verified
        Task<DoctorDto> ApproveDoctorAsync(Guid doctorId);                        // Verified → Approved
        Task<DoctorDto> ActivateDoctorAsync(Guid doctorId, int verifyCode);                       // Approved → Active  (+sync User.IsActive=true)
/*        Task<DoctorDto> RejectDoctorAsync(Guid doctorId, string? reason);  */       // any → Rejected     (+sync User.IsActive=false)

        // Heartbeat (online status)
        Task HeartbeatAsync(Guid doctorId);

        // Availability
        //Task<DoctorAvailabilityDto> AddAvailabilityAsync(Guid doctorId, CreateDoctorAvailabilityDto request);
        //Task<DoctorAvailabilityDto> UpdateAvailabilityAsync(Guid doctorId, Guid availabilityId, UpdateDoctorAvailabilityDto request);
        //Task DeleteAvailabilityAsync(Guid doctorId, Guid availabilityId);
    }
}
