using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IDoctorAvailabilityService
    {
        Task<ApiResponse<DoctorAvailabilityDto>> CreateAsync(Guid doctorId, Guid currentUserId, CreateDoctorAvailabilityRequest request);
        Task<ApiResponse<IEnumerable<DoctorAvailabilityDto>>> GetByDoctorIdAsync(Guid doctorId);
        Task<ApiResponse<DoctorAvailabilityDto>> GetByIdAsync(Guid availabilityId);
        Task<ApiResponse<DoctorAvailabilityDto>> UpdateAsync(Guid availabilityId, Guid currentUserId, UpdateDoctorAvailabilityRequest request);
        Task<ApiResponse<bool>> DeleteAsync(Guid availabilityId, Guid currentUserId);
    }
}
