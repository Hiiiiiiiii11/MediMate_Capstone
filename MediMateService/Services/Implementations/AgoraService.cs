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

        // ─────────────────────────────────────────────────────────────────
        // NEW: Tạo token cho Guardian (Người giám hộ) — cuộc gọi 3 bên
        // ─────────────────────────────────────────────────────────────────
        public async Task<ApiResponse<GuardianJoinResponse>> GenerateGuardianTokenAsync(Guid sessionId, Guid guardianUserId)
        {
            try
            {
                var session = (await _unitOfWork.Repository<ConsultationSessions>()
                    .FindAsync(s => s.ConsultanSessionId == sessionId,
                               includeProperties: "Member,Doctor")).FirstOrDefault();

                if (session == null)
                    return ApiResponse<GuardianJoinResponse>.Fail("Không tìm thấy phiên khám.", 404);

                if (session.Status == "Ended" || session.Status == "Cancelled")
                    return ApiResponse<GuardianJoinResponse>.Fail("Phiên khám đã kết thúc.", 400);

                // ── Kiểm tra quyền Guardian ──────────────────────────────
                if (!session.GuardianUserId.HasValue || session.GuardianUserId.Value != guardianUserId)
                    return ApiResponse<GuardianJoinResponse>.Fail("Bạn không có quyền tham gia với vai trò người giám hộ.", 403);

                if (string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_appCertificate))
                    return ApiResponse<GuardianJoinResponse>.Fail("Thiếu cấu hình Agora.", 500);

                // ── Tạo UID cho Guardian từ Guid ─────────────────────────
                // Dùng hash để đảm bảo consistent mỗi lần gọi
                uint guardianUid = (uint)Math.Abs(guardianUserId.GetHashCode());

                string channelName = sessionId.ToString();
                uint expirationTimeInSeconds = 3600;
                uint privilegeExpiredTs = (uint)DateTimeOffset.Now.ToUnixTimeSeconds() + expirationTimeInSeconds;

                var builder = new RtcTokenBuilder();
                string token = builder.BuildToken(
                    _appId, _appCertificate, channelName,
                    true,
                    privilegeExpiredTs
                );

                // ── Cập nhật GuardianJoined = true ────────────────────────
                session.GuardianJoined = true;
                _unitOfWork.Repository<ConsultationSessions>().Update(session);
                await _unitOfWork.CompleteAsync();

                // ── Notify Doctor qua SignalR: có người giám hộ vào ──────
                await _hubContext.Clients.Group($"User_{session.Doctor.UserId}")
                    .SendAsync("GuardianJoined", new
                    {
                        sessionId      = sessionId,
                        guardianName   = "Người giám hộ",
                        memberName     = session.Member?.FullName
                    });

                var response = new GuardianJoinResponse
                {
                    Token       = token,
                    Uid         = guardianUid,
                    ChannelName = channelName,
                    SessionId   = sessionId,
                    MemberName  = session.Member?.FullName ?? "",
                    DoctorName  = session.Doctor?.FullName ?? ""
                };

                return ApiResponse<GuardianJoinResponse>.Ok(response, "Tham gia cuộc gọi với vai trò người giám hộ thành công.");
            }
            catch (Exception ex)
            {
                return ApiResponse<GuardianJoinResponse>.Fail($"Lỗi: {ex.Message}", 500);
            }
        }
    }
}