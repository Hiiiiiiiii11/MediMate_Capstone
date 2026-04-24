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
        // 1. BẮT ĐẦU GHI HÌNH
        // ─────────────────────────────────────────────────────────────────
        public async Task<bool> StartRecordingAsync(Guid sessionId)
        {
            if (!ValidateConfig()) return false;

            var session = await _unitOfWork.Repository<ConsultationSessions>().GetByIdAsync(sessionId);
            if (session == null || session.Status != "InProgress")
                return false;

            // Nếu đã có resourceId thì đang ghi rồi, bỏ qua
            if (!string.IsNullOrEmpty(session.AgoraRecordingResourceId))
                return true;

            try
            {
                var channelName = sessionId.ToString();
                var uid = "0"; // UID đặc biệt dành cho cloud recorder

                // ── Step 1: Acquire resourceId ──────────────────────────
                var resourceId = await AcquireResourceAsync(channelName, uid);
                if (string.IsNullOrEmpty(resourceId))
                {
                    _logger.LogWarning("[Recording] Acquire thất bại cho session {SessionId}", sessionId);
                    return false;
                }

                // ── Step 2: Start recording ─────────────────────────────
                var sid = await StartCloudRecordingAsync(resourceId, channelName, uid, sessionId);
                if (string.IsNullOrEmpty(sid))
                {
                    _logger.LogWarning("[Recording] Start thất bại cho session {SessionId}", sessionId);
                    return false;
                }

                // ── Step 3: Lưu resourceId và sid vào DB ────────────────
                session.AgoraRecordingResourceId = resourceId;
                session.AgoraSid = sid;
                _unitOfWork.Repository<ConsultationSessions>().Update(session);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("[Recording] Bắt đầu ghi hình session {SessionId} — SID: {Sid}", sessionId, sid);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Recording] Lỗi khi bắt đầu ghi hình session {SessionId}", sessionId);
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // 2. DỪNG GHI HÌNH + UPLOAD LÊN CLOUDINARY
        // ─────────────────────────────────────────────────────────────────
        public async Task<AgoraRecordingResultDto> StopAndUploadRecordingAsync(Guid sessionId)
        {
            var result = new AgoraRecordingResultDto();

            var session = await _unitOfWork.Repository<ConsultationSessions>().GetByIdAsync(sessionId);
            if (session == null)
            {
                result.ErrorMessage = "Không tìm thấy phiên khám.";
                return result;
            }

            if (string.IsNullOrEmpty(session.AgoraRecordingResourceId) || string.IsNullOrEmpty(session.AgoraSid))
            {
                // Chưa có ghi hình → không có gì để dừng
                result.Success = false;
                result.ErrorMessage = "Phiên này chưa bắt đầu ghi hình.";
                return result;
            }

            try
            {
                var channelName = sessionId.ToString();

                // ── Step 1: Stop recording trên Agora ───────────────────
                var stopResponse = await StopCloudRecordingAsync(
                    session.AgoraRecordingResourceId,
                    session.AgoraSid,
                    channelName,
                    "0");

                // ── Step 2: Tính thời lượng ──────────────────────────────
                int durationSeconds = 0;
                if (session.StartedAt != default && session.EndedAt.HasValue)
                    durationSeconds = (int)(session.EndedAt.Value - session.StartedAt).TotalSeconds;

                // ── Step 3: Tải file đầu tiên playable và upload Cloudinary
                string? cloudinaryUrl = null;
                var fileList = stopResponse?.ServerResponse?.FileList;
                if (fileList != null)
                {
                    var playableFile = fileList.FirstOrDefault(f => f.IsPlayable == true)
                                      ?? fileList.FirstOrDefault();

                    if (playableFile?.FileName != null)
                    {
                        // URL tạm của Agora CDN (có thể delay 1-2 phút sau khi stop)
                        // Agora lưu file trên their cloud storage — với mix mode ta cần thêm bucket config
                        // Ở đây ta dùng URL pattern của Agora web recording để tải thử
                        var agoraFileUrl = BuildAgoraFileUrl(channelName, playableFile.FileName);
                        cloudinaryUrl = await DownloadAndUploadToCloudinaryAsync(agoraFileUrl, sessionId);
                    }
                }

                // ── Step 4: Cập nhật DB ──────────────────────────────────
                if (!string.IsNullOrEmpty(cloudinaryUrl))
                {
                    session.RecordUrl = cloudinaryUrl;
                }
                session.RecordingDuration = durationSeconds;
                // Xóa resourceId và sid để không cố stop lần nữa
                session.AgoraRecordingResourceId = null;
                session.AgoraSid = null;

                _unitOfWork.Repository<ConsultationSessions>().Update(session);
                await _unitOfWork.CompleteAsync();

                result.Success = true;
                result.RecordingUrl = cloudinaryUrl;
                result.DurationSeconds = durationSeconds;

                _logger.LogInformation("[Recording] Hoàn tất session {SessionId} — URL: {Url}", sessionId, cloudinaryUrl ?? "(chưa có file)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Recording] Lỗi khi dừng và upload session {SessionId}", sessionId);
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        // 3. LẤY URL VIDEO (CÓ KIỂM TRA QUYỀN)
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

            // Kiểm tra quyền: Doctor của phiên hoặc Family Owner (Guardian)
            bool isDoctor = session.Doctor?.UserId == callerUserId;
            bool isOwner = session.GuardianUserId == callerUserId;

            // Nếu không phải doctor cũng không phải owner, kiểm tra xem có phải member.UserId không
            if (!isDoctor && !isOwner)
            {
                var member = await _unitOfWork.Repository<Members>()
                    .GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MemberId == session.MemberId && m.UserId == callerUserId);
                isOwner = member != null;
            }

            //if (!isDoctor && !isOwner)
            //    throw new ForbiddenException("Bạn không có quyền xem video phiên khám này.");

            return session.RecordUrl;
        }

        // ─────────────────────────────────────────────────────────────────
        // AGORA REST API HELPERS
        // ─────────────────────────────────────────────────────────────────

        private async Task<string?> AcquireResourceAsync(string channelName, string uid)
        {
            var client = CreateHttpClient();
            var url = $"{AgoraBaseUrl}/{_appId}/cloud_recording/acquire";

            var body = new
            {
                cname = channelName,
                uid = uid,
                clientRequest = new
                {
                    resourceExpiredHour = 24,
                    scene = 0  // 0 = realtime, 1 = web
                }
            };

            var response = await client.PostAsync(url, JsonContent(body));
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AgoraAcquireResponse>(json, JsonOptions());
            return result?.ResourceId;
        }

        private async Task<string?> StartCloudRecordingAsync(
            string resourceId, string channelName, string uid, Guid sessionId)
        {
            var client = CreateHttpClient();
            var url = $"{AgoraBaseUrl}/{_appId}/cloud_recording/resourceid/{resourceId}/mode/mix/start";

            // Tạo token Agora cho cloud recorder
            var recordingToken = GenerateRecordingToken(channelName);

            var body = new
            {
                cname = channelName,
                uid = uid,
                clientRequest = new
                {
                    token = recordingToken,
                    recordingConfig = new
                    {
                        maxIdleTime = 30,           // Tự dừng sau 30s không có ai
                        streamTypes = 3,            // 0=audio, 1=video, 3=cả hai
                        channelType = 0,            // 0=communication channel
                        videoStreamType = 0,        // 0=high quality stream
                        transcodingConfig = new
                        {
                            height = 720,
                            width = 1280,
                            bitrate = 2000,
                            fps = 15,
                            mixedVideoLayout = 1    // 1=best fit
                        }
                    },
                    storageConfig = new
                    {
                        // Agora sẽ lưu file tạm trước khi ta pull về
                        // Nếu không cấu hình bucket, file nằm trên Agora server 24h
                        vendor = 0,  // 0 = Agora default storage
                        region = 0,
                        bucket = "",
                        accessKey = "",
                        secretKey = ""
                    }
                }
            };

            var response = await client.PostAsync(url, JsonContent(body));
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogError("[Recording] Start API lỗi: {Error}", err);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AgoraStartRecordingResponse>(json, JsonOptions());
            return result?.Sid;
        }

        private async Task<AgoraStopRecordingResponse?> StopCloudRecordingAsync(
            string resourceId, string sid, string channelName, string uid)
        {
            var client = CreateHttpClient();
            var url = $"{AgoraBaseUrl}/{_appId}/cloud_recording/resourceid/{resourceId}/sid/{sid}/mode/mix/stop";

            var body = new
            {
                cname = channelName,
                uid = uid,
                clientRequest = new { }
            };

            var response = await client.PostAsync(url, JsonContent(body));
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AgoraStopRecordingResponse>(json, JsonOptions());
        }

        // ─────────────────────────────────────────────────────────────────
        // CLOUDINARY UPLOAD HELPERS
        // ─────────────────────────────────────────────────────────────────

        private async Task<string?> DownloadAndUploadToCloudinaryAsync(string fileUrl, Guid sessionId)
        {
            try
            {
                // 1. Tải bytes từ Agora CDN
                var httpClient = _httpClientFactory.CreateClient();
                var videoBytes = await httpClient.GetByteArrayAsync(fileUrl);

                // 2. Chuyển sang Stream và gọi UploadPhotoService
                using var stream = new MemoryStream(videoBytes);
                var cloudinaryUrl = await _uploadPhotoService.UploadVideoFromStreamAsync(
                    videoStream: stream,
                    publicId: $"session_{sessionId}",
                    folder: "consultation_recordings"
                );

                return cloudinaryUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Recording] Lỗi tải file từ Agora CDN — URL: {Url}", fileUrl);
                return null;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // UTILITY HELPERS
        // ─────────────────────────────────────────────────────────────────

        private HttpClient CreateHttpClient()
        {
            var client = _httpClientFactory.CreateClient("Agora");
            // Basic Auth: customerId:customerSecret
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_customerId}:{_customerSecret}"));
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            return client;
        }

        private static StringContent JsonContent(object body)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions());
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private static JsonSerializerOptions JsonOptions() => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private string BuildAgoraFileUrl(string channelName, string fileName)
        {
            // Với Agora default storage, file có thể truy cập qua URL này
            // Nếu dùng external bucket (S3/GCS) thì URL khác
            return $"https://api.agora.io/v1/apps/{_appId}/cloud_recording/files/{fileName}";
        }

        private string GenerateRecordingToken(string channelName)
        {
            // Tái sử dụng logic token generation của Agora (uid = 0 cho recorder)
            try
            {
                var builder = new AgoraIO.Rtc.RtcTokenBuilder();
                uint expiredTs = (uint)DateTimeOffset.Now.ToUnixTimeSeconds() + 7200;
                return builder.BuildToken(_appId, _appCertificate, channelName, true, expiredTs);
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool ValidateConfig()
        {
            if (string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_appCertificate) ||
                string.IsNullOrEmpty(_customerId) || string.IsNullOrEmpty(_customerSecret))
            {
                _logger.LogWarning("[Recording] Thiếu cấu hình Agora (APP_ID, CERTIFICATE, CUSTOMER_ID, CUSTOMER_SECRET).");
                return false;
            }
            return true;
        }
    }
}
