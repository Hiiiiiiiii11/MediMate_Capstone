using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IRagBaseDocumentService
    {
        Task<ApiResponse<RagBaseDocumentDto>> CreateAsync(CreateRagBaseDocumentRequest request);
        Task<ApiResponse<IEnumerable<RagBaseDocumentDto>>> GetByCollectionIdAsync(Guid collectionId);
        Task<ApiResponse<IEnumerable<RagBaseDocumentDto>>> GetAllDocument();
        Task<ApiResponse<RagBaseDocumentDto>> GetByIdAsync(Guid documentId);
        Task<ApiResponse<RagBaseDocumentDto>> UpdateAsync(Guid documentId, UpdateRagBaseDocumentRequest request);
        Task<ApiResponse<bool>> DeleteAsync(Guid documentId);
    }
}
