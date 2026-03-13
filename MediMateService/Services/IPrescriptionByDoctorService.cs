using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IPrescriptionByDoctorService
    {
        Task<ApiResponse<PrescriptionByDoctorDto>> CreateAsync(Guid doctorId, Guid currentUserId, CreatePrescriptionByDoctorRequest request);
        Task<ApiResponse<PrescriptionByDoctorDto>> GetByIdAsync(Guid prescriptionId, Guid currentUserId);
        Task<ApiResponse<IEnumerable<PrescriptionByDoctorDto>>> GetBySessionIdAsync(Guid sessionId, Guid currentUserId);
        Task<ApiResponse<IEnumerable<PrescriptionByDoctorDto>>> GetByMemberIdAsync(Guid memberId, Guid currentUserId);
        Task<ApiResponse<PrescriptionByDoctorDto>> UpdateAsync(Guid prescriptionId, Guid currentUserId, UpdatePrescriptionByDoctorRequest request);
    }
}
