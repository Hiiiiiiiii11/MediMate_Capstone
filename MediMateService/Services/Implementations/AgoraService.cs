using AgoraIO.Rtc;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using Share.Common;
using System;
// Lưu ý: Nếu chữ RtcTokenBuilder bị gạch đỏ, bạn bấm Alt + Enter để Visual Studio tự thêm using của thư viện vào nhé.

namespace MediMateService.Services.Implementations
{
    public class AgoraService : IAgoraService
    {
        private readonly string _appId;
        private readonly string _appCertificate;
        private readonly IUnitOfWork _unitOfWork;

        public AgoraService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            // Đọc App ID và Certificate từ biến môi trường (.env)
            _appId = Environment.GetEnvironmentVariable("AGORA_APP_ID") ?? "";
            _appCertificate = Environment.GetEnvironmentVariable("AGORA_APP_CERTIFICATE") ?? "";
        }

        public async Task<ApiResponse<string>> GenerateRtcTokenAsync(Guid sessionId, uint uid, string role = "publisher")
        {
            try
            {
                // ==========================================
                // 1. KIỂM TRA TRẠNG THÁI PHIÊN KHÁM TỪ DB
                // ==========================================
                var session = await _unitOfWork.Repository<ConsultationSessions>().GetByIdAsync(sessionId);

                if (session == null)
                {
                    return ApiResponse<string>.Fail("Không tìm thấy phiên khám.", 404);
                }

                if (session.Status == "Cancelled" || session.Status == "Completed")
                {
                    return ApiResponse<string>.Fail("Phiên khám đã kết thúc hoặc bị hủy. Không thể tham gia gọi video.", 400);
                }

                // ==========================================
                // 2. TẠO TOKEN NẾU HỢP LỆ
                // ==========================================
                if (string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_appCertificate))
                {
                    return ApiResponse<string>.Fail("Thiếu cấu hình Agora App ID hoặc Certificate.", 500);
                }

                string channelName = sessionId.ToString();

                // Cài đặt Token hết hạn sau 1 tiếng (3600 giây)
                uint expirationTimeInSeconds = 3600;
                uint currentTimeStamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                uint privilegeExpiredTs = currentTimeStamp + expirationTimeInSeconds;

                // Xác định quyền: Nếu truyền vào "publisher" thì isPublisher = true (Được phép bật Camera/Mic)
                bool isPublisher = role.ToLower() == "publisher";

                var builder = new RtcTokenBuilder();
                string token = builder.BuildToken(
                    _appId,
                    _appCertificate,
                    channelName,
                    isPublisher,
                    privilegeExpiredTs
                );

                return ApiResponse<string>.Ok(token, "Tạo Token gọi Video thành công.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail($"Lỗi tạo token Agora: {ex.Message}", 500);
            }
        }
    }
}