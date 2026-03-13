using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IDoctorBankAccountService
    {
        Task<ApiResponse<DoctorBankAccountDto>> CreateAsync(Guid doctorId, Guid currentUserId, CreateDoctorBankAccountRequest request);
        Task<ApiResponse<IEnumerable<DoctorBankAccountDto>>> GetByDoctorIdAsync(Guid doctorId);
        Task<ApiResponse<DoctorBankAccountDto>> GetByIdAsync(Guid bankAccountId);
        Task<ApiResponse<DoctorBankAccountDto>> UpdateAsync(Guid bankAccountId, Guid currentUserId, UpdateDoctorBankAccountRequest request);
        Task<ApiResponse<bool>> DeleteAsync(Guid bankAccountId, Guid currentUserId);
    }
}
