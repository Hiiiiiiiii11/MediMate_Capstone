using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IAgoraService
    {
        Task<ApiResponse<string>> GenerateRtcTokenAsync(Guid sessionId, uint uid, string role = "publisher");

        /// <summary>
        /// Tạo Agora token cho Người giám hộ (Guardian/Owner) tham gia cuộc gọi 3 bên.
        /// Chỉ cấp token nếu guardianUserId khớp với session.GuardianUserId.
        /// </summary>

    }
}
