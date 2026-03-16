using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IRagBaseConfigService
    {
        Task<ApiResponse<RagBaseConfigDto>> CreateConfigAsync(CreateRagBaseConfigRequest request);
        Task<ApiResponse<RagBaseConfigDto>> GetConfigAsync();
        Task<ApiResponse<RagBaseConfigDto>> UpdateConfigAsync(UpdateRagBaseConfigRequest request);
    }
}
