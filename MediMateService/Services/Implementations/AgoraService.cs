using AgoraIO.Rtc;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Hubs;
using Microsoft.AspNetCore.SignalR;
using Share.Common;

namespace MediMateService.Services.Implementations
{
    public class AgoraService : IAgoraService
    {
        private readonly string _appId;
        private readonly string _appCertificate;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<MediMateHub> _hubContext;

        public AgoraService(IUnitOfWork unitOfWork, IHubContext<MediMateHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _appId = Environment.GetEnvironmentVariable("AGORA_APP_ID") ?? "";
            _appCertificate = Environment.GetEnvironmentVariable("AGORA_APP_CERTIFICATE") ?? "";
        }

        // ─────────────────────────────────────────────────────────────────
        // EXISTING: Tạo token cho Doctor / Member bình thường
        // ─────────────────────────────────────────────────────────────────
        public async Task<ApiResponse<string>> GenerateRtcTokenAsync(Guid sessionId, uint uid, string role = "publisher")
        {
            try
            {
                var session = await _unitOfWork.Repository<ConsultationSessions>().GetByIdAsync(sessionId);
                if (session == null)
                    return ApiResponse<string>.Fail("Không tìm thấy phiên khám.", 404);

                if (session.Status == "Cancelled" || session.Status == "Completed")
                    return ApiResponse<string>.Fail("Phiên khám đã kết thúc hoặc bị hủy.", 400);

                if (string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_appCertificate))
                    return ApiResponse<string>.Fail("Thiếu cấu hình Agora App ID hoặc Certificate.", 500);

                string channelName = sessionId.ToString();
                uint expirationTimeInSeconds = 3600;
                uint currentTimeStamp = (uint)DateTimeOffset.Now.ToUnixTimeSeconds();
                uint privilegeExpiredTs = currentTimeStamp + expirationTimeInSeconds;

                bool isPublisher = role.ToLower() == "publisher";
                var builder = new RtcTokenBuilder();
                string token = builder.BuildToken(
                    _appId, _appCertificate, channelName,
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