using MediMateService.DTOs;

namespace MediMateService.Services
{
    public interface IAgoraRecordingService
    {

        /// <summary>
        /// Lấy URL video của phiên khám (có kiểm tra quyền: chỉ Doctor và Owner mới xem được).
        /// </summary>
        Task<string?> GetRecordingUrlAsync(Guid sessionId, Guid callerUserId);

        /// <summary>
        /// Upload video thủ công từ Web/App (nếu Agora Cloud Recording không khả dụng).
        /// </summary>
        Task<string?> UploadManualRecordingAsync(Guid sessionId, Stream videoStream);
    }
}
