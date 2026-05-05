using MediMateService.DTOs;
using Share.Common;

namespace MediMateService.Services
{
    public interface IClinicService
    {
        // ─── Clinic CRUD ────────────────────────────────────────────
        Task<ClinicDto> CreateClinicAsync(CreateClinicDto dto);
        Task<ClinicDto> GetClinicByIdAsync(Guid clinicId);
        Task<IReadOnlyList<ClinicDto>> GetAllClinicsAsync(bool? isActive = null);
        Task<ClinicDto> UpdateClinicAsync(Guid clinicId, UpdateClinicDto dto);
        Task DeleteClinicAsync(Guid clinicId);

        // ─── ClinicDoctor Management ────────────────────────────────
        Task<ClinicDoctorDto> AddDoctorToClinicAsync(Guid clinicId, AddDoctorToClinicDto dto);
        Task<ClinicDoctorDto> UpdateClinicDoctorAsync(Guid clinicDoctorId, UpdateClinicDoctorDto dto);
        Task RemoveDoctorFromClinicAsync(Guid clinicDoctorId);
        Task<IReadOnlyList<ClinicDoctorDto>> GetDoctorsByClinicAsync(Guid clinicId);

        // ─── ClinicContract Management ──────────────────────────────
        Task<ClinicContractDto> CreateContractAsync(CreateClinicContractDto dto);
        Task<IReadOnlyList<ClinicContractDto>> GetContractsByClinicAsync(Guid clinicId);
        Task<ClinicContractDto> UpdateContractStatusAsync(Guid contractId, string status);
    }
}
