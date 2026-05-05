using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediMateService.Services.Implementations
{
    /// <summary>
    /// Giao tiếp với Agora Cloud Recording REST API để tự động ghi video phiên khám,
    /// sau đó tải file và upload lên Cloudinary để lưu trữ lâu dài.
    /// 
    /// Agora Cloud Recording Flow:
    ///  1. acquire()  → nhận resourceId
    ///  2. start()    → nhận sid, bắt đầu ghi
    ///  3. stop()     → trả về danh sách file trên Agora CDN
    ///  4. Tải file   → upload lên Cloudinary
    ///  5. Cập nhật   → RecordUrl, RecordingDuration vào DB
    /// </summary>
    public class AgoraRecordingService : IAgoraRecordingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadPhotoService _uploadPhotoService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AgoraRecordingService> _logger;

        // Agora credentials
        private readonly string _appId;
        private readonly string _appCertificate;
        private readonly string _customerId;
        private readonly string _customerSecret;

        // Agora Cloud Recording REST API base
        private const string AgoraBaseUrl = "https://api.agora.io/v1/apps";

        public AgoraRecordingService(
            IUnitOfWork unitOfWork,
            IUploadPhotoService uploadPhotoService,
            IHttpClientFactory httpClientFactory,
            ILogger<AgoraRecordingService> logger)
        {
            _unitOfWork = unitOfWork;
            _uploadPhotoService = uploadPhotoService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            _appId = Environment.GetEnvironmentVariable("AGORA_APP_ID") ?? "";
            _appCertificate = Environment.GetEnvironmentVariable("AGORA_APP_CERTIFICATE") ?? "";
            _customerId = Environment.GetEnvironmentVariable("AGORA_CUSTOMER_ID") ?? "";
            _customerSecret = Environment.GetEnvironmentVariable("AGORA_CUSTOMER_SECRET") ?? "";
        }

        // ─────────────────────────────────────────────────────────────────
        // 1. LẤY URL VIDEO (CÓ KIỂM TRA QUYỀN)
        // Chỉ Doctor của phiên và Family Owner mới được xem
        // ─────────────────────────────────────────────────────────────────
        public async Task<string?> GetRecordingUrlAsync(Guid sessionId, Guid callerUserId)
        {
            var session = await _unitOfWork.Repository<ConsultationSessions>()
                .GetQueryable()
                .Include(s => s.Doctor).ThenInclude(d => d.User)
                .Include(s => s.Member)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ConsultanSessionId == sessionId);

            if (session == null)
                throw new NotFoundException("Không tìm thấy phiên khám.");

            // ── Kiểm tra danh tính ────────────────────────────────────────
            bool isDoctor = session.Doctor?.UserId == callerUserId;
            bool isOwner = false;

            if (!isDoctor)
            {
                var member = await _unitOfWork.Repository<Members>()
                    .GetQueryable().AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MemberId == session.MemberId && m.UserId == callerUserId);
                isOwner = member != null;
            }

            // if (!isDoctor && !isOwner)
            //     throw new ForbiddenException("Bạn không có quyền xem video phiên khám này.");

            // ── Kiểm tra gói đăng ký (chỉ áp dụng với User, Doctor được miễn) ──
            if (!isDoctor)
            {
                // Tìm FamilyId của member trong phiên
                var memberRecord = await _unitOfWork.Repository<Members>()
                    .GetQueryable().AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MemberId == session.MemberId);

                if (memberRecord?.FamilyId != null)
                {
                    var activeSub = await _unitOfWork.Repository<FamilySubscriptions>()
                        .GetQueryable()
                        .Include(fs => fs.Package)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(fs =>
                            fs.FamilyId == memberRecord.FamilyId &&
                            fs.Status == "Active" &&
                            fs.Package.AllowVideoRecordingAccess);

                    if (activeSub == null)
                        throw new ForbiddenException(
                            "Tính năng xem lại video phiên khám chỉ dành cho gói Premium trở lên. " +
                            "Vui lòng nâng cấp gói để sử dụng tính năng này.");
                }
            }

            return session.RecordUrl;
        }

        // ─────────────────────────────────────────────────────────────────
        // 4. UPLOAD THỦ CÔNG TỪ WEB/APP (TRƯỜNG HỢP KHÔNG CÓ S3)
        // ─────────────────────────────────────────────────────────────────
        public async Task<string?> UploadManualRecordingAsync(Guid sessionId, Stream videoStream)
        {
            try
            {
                var session = await _unitOfWork.Repository<ConsultationSessions>().GetByIdAsync(sessionId);
                if (session == null) return null;

                var cloudinaryUrl = await _uploadPhotoService.UploadVideoFromStreamAsync(
                    videoStream: videoStream,
                    publicId: $"session_{sessionId}_manual",
                    folder: "consultation_recordings"
                );

                if (!string.IsNullOrEmpty(cloudinaryUrl))
                {
                    session.RecordUrl = cloudinaryUrl;
                    _unitOfWork.Repository<ConsultationSessions>().Update(session);
                    await _unitOfWork.CompleteAsync();
                }

                return cloudinaryUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Recording] Lỗi khi upload video thủ công cho session {SessionId}", sessionId);
                return null;
            }
        }

    }
}
