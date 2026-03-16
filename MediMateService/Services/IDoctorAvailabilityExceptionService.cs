using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IDoctorAvailabilityExceptionService
    {
        Task<ApiResponse<DoctorAvailabilityExceptionDto>> CreateAsync(Guid doctorId, Guid currentUserId, CreateDoctorAvailabilityExceptionRequest request);
        Task<ApiResponse<IEnumerable<DoctorAvailabilityExceptionDto>>> GetByDoctorIdAsync(Guid doctorId);
        Task<ApiResponse<DoctorAvailabilityExceptionDto>> GetByIdAsync(Guid exceptionId);
        Task<ApiResponse<DoctorAvailabilityExceptionDto>> UpdateAsync(Guid exceptionId, Guid currentUserId, UpdateDoctorAvailabilityExceptionRequest request);
        Task<ApiResponse<bool>> DeleteAsync(Guid exceptionId, Guid currentUserId);
    }
}
