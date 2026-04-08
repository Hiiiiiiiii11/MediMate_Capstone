using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IDoctorDocumentService
    {
        // Dành cho Bác sĩ
        Task<ApiResponse<DoctorDocumentDto>> CreateAsync(Guid doctorId, Guid currentUserId, CreateDoctorDocumentRequest request);
        Task<ApiResponse<DoctorDocumentDto>> UpdateAsync(Guid documentId, Guid currentUserId, UpdateDoctorDocumentRequest request);
        Task<ApiResponse<bool>> DeleteAsync(Guid documentId, Guid currentUserId);

        // Dùng chung
        Task<ApiResponse<PagedResult<DoctorDocumentDto>>> GetAllAsync(DoctorDocumentFilter filter);
        Task<ApiResponse<IEnumerable<DoctorDocumentDto>>> GetByDoctorIdAsync(Guid doctorId);
        Task<ApiResponse<DoctorDocumentDto>> GetByIdAsync(Guid documentId);

        // Dành cho Admin / DoctorManager
        Task<ApiResponse<DoctorDocumentDto>> ReviewDocumentAsync(Guid documentId, string reviewerName, ReviewDoctorDocumentRequest request);
    }
}
