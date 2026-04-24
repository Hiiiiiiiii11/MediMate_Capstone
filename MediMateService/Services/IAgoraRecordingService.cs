using MediMateService.DTOs;

namespace MediMateService.Services
{
    public interface IAgoraRecordingService
    {
        /// <summary>
        /// Bắt đầu Agora Cloud Recording cho session (gọi sau khi cả 2 bên đã join).
        /// Lưu resourceId và sid vào ConsultationSessions để có thể dừng sau.
        /// </summary>
        Task<bool> StartRecordingAsync(Guid sessionId);

        /// <summary>
        /// Dừng Agora Cloud Recording, tải video từ Agora CDN và upload lên Cloudinary.
        /// Cập nhật RecordUrl và RecordingDuration vào ConsultationSessions.
        /// </summary>
        Task<AgoraRecordingResultDto> StopAndUploadRecordingAsync(Guid sessionId);

        /// <summary>
        /// Lấy URL video của phiên khám (có kiểm tra quyền: chỉ Doctor và Owner mới xem được).
        /// </summary>
        Task<string?> GetRecordingUrlAsync(Guid sessionId, Guid callerUserId);
    }
}
