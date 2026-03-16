using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IRagBaseCollectionService
    {
        Task<ApiResponse<RagBaseCollectionDto>> CreateAsync(CreateRagBaseCollectionRequest request);
        Task<ApiResponse<IEnumerable<RagBaseCollectionDto>>> GetAllAsync();
        Task<ApiResponse<RagBaseCollectionDto>> GetByIdAsync(Guid collectionId);
        Task<ApiResponse<RagBaseCollectionDto>> UpdateAsync(Guid collectionId, UpdateRagBaseCollectionRequest request);
        Task<ApiResponse<bool>> DeleteAsync(Guid collectionId);
    }
}
