using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IDoctorContractService
    {
        Task<ApiResponse<DoctorContractResponse>> CreateAsync(CreateContractRequest request);
        Task<ApiResponse<DoctorContractResponse>> UpdateAsync(Guid id, UpdateContractRequest request);
        Task<ApiResponse<DoctorContractResponse>> GetByIdAsync(Guid id);
        Task<ApiResponse<IEnumerable<DoctorContractResponse>>> GetAllAsync();
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
