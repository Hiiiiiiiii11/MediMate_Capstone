using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IVersionService
    {
        Task<ApiResponse<IEnumerable<VersionDto>>> GetAllVersionsAsync(string? platform = null);
        Task<ApiResponse<VersionDto>> GetVersionByIdAsync(Guid versionId);

        // Hàm chuyên dụng cho Mobile App kiểm tra cập nhật
        Task<ApiResponse<VersionDto>> CheckLatestVersionAsync(string platform);

        Task<ApiResponse<VersionDto>> CreateVersionAsync(CreateVersionDto request);
        Task<ApiResponse<VersionDto>> UpdateVersionAsync(Guid versionId, UpdateVersionDto request);
        Task<ApiResponse<bool>> DeleteVersionAsync(Guid versionId);
    }
}
