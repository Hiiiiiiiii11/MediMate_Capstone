using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IRagBaseEmbeddingService
    {
        Task<ApiResponse<RagBaseEmbeddingDto>> CreateAsync(CreateRagBaseEmbeddingRequest request);
        Task<ApiResponse<IEnumerable<RagBaseEmbeddingDto>>> GetByDocumentIdAsync(Guid documentId);
        Task<ApiResponse<RagBaseEmbeddingDto>> GetByIdAsync(Guid embeddingId);
        Task<ApiResponse<RagBaseEmbeddingDto>> UpdateAsync(Guid embeddingId, UpdateRagBaseEmbeddingRequest request);
        Task<ApiResponse<bool>> DeleteAsync(Guid embeddingId);
    }
}
