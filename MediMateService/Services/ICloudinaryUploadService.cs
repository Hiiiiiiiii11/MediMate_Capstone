using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface ICloudinaryUploadService
    {
        Task<ApiResponse<PagedResult<DoctorDocumentDto>>> GetDocumentsWithPaginationAsync(DoctorDocumentFilter filter);
        Task<ApiResponse<PagedResult<PrescriptionImageDetailDto>>> GetPrescriptionImagesPaginatedAsync(PrescriptionImageFilter filter, Guid currentUserId);

    }
}
