using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using Share.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using MediMateService.Hubs;
using Microsoft.EntityFrameworkCore;

namespace MediMateService.Services.Implementations
{
    public class ChatDoctorService : IChatDoctorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUploadPhotoService _uploadPhotoService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<MediMateHub> _hubContext;

        public ChatDoctorService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IUploadPhotoService uploadPhotoService, INotificationService notificationService, IHubContext<MediMateHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _uploadPhotoService = uploadPhotoService;
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        public async Task<ApiResponse<IEnumerable<ChatDoctorMessageResponse>>> GetSessionMessagesAsync(Guid sessionId, Guid currentUserId, bool isDoctorRequest)
        {
            var session = await _unitOfWork.Repository<ConsultationSessions>()
                .GetQueryable()
                .AsNoTracking() // <--- THÊM Ở ĐÂY
                .Include(s => s.Member)
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.ConsultanSessionId == sessionId);

            if (session == null) return ApiResponse<IEnumerable<ChatDoctorMessageResponse>>.Fail("Phiên tư vấn không tồn tại.", 404);

            if (!await ValidateAccessAsync(session, currentUserId, isDoctorRequest))
                return ApiResponse<IEnumerable<ChatDoctorMessageResponse>>.Fail("Access Denied", 403);

            var messages = await _unitOfWork.Repository<ChatDoctorMessages>()
                .GetQueryable()
                .AsNoTracking() // <--- THÊM Ở ĐÂY
                .Where(m => m.ConsultanSessionId == sessionId)
                .OrderBy(m => m.SendAt)
                .ToListAsync();

            var response = messages.Select(m => MapToResponse(m, session));
            return ApiResponse<IEnumerable<ChatDoctorMessageResponse>>.Ok(response);
        }

        public async Task<ApiResponse<ChatDoctorMessageResponse>> SendMessageAsync(Guid sessionId, Guid currentUserId, SendChatDoctorRequest request, bool isDoctorRequest)
        {
            var session = await _unitOfWork.Repository<ConsultationSessions>()
                 .GetQueryable()
                 .Include(s => s.Doctor)
                 .Include(s => s.Member)
                 .FirstOrDefaultAsync(s => s.ConsultanSessionId == sessionId);

            if (session == null) return ApiResponse<ChatDoctorMessageResponse>.Fail("Phiên tư vấn không tồn tại.", 404);


            var chatEndTime = session.StartedAt.AddMinutes(125);

            // Tùy chọn: Nếu bạn muốn chặn không cho chat TRƯỚC giờ bắt đầu, có thể mở comment dòng dưới:
            // if (DateTime.Now < session.StartedAt) 
            //     return ApiResponse<ChatDoctorMessageResponse>.Fail($"Phòng chat chưa mở. Bạn có thể nhắn tin từ {session.StartedAt:HH:mm}.", 403);

            if (DateTime.Now > chatEndTime)
            {
                return ApiResponse<ChatDoctorMessageResponse>.Fail($"Phòng chat đã đóng. Thời gian cho phép nhắn tin đã kết thúc vào lúc {chatEndTime:HH:mm}.", 403);
            }

            // Kiểm tra quyền 
            if (!await ValidateAccessAsync(session, currentUserId, isDoctorRequest))
                return ApiResponse<ChatDoctorMessageResponse>.Fail("Access Denied", 403);

            string? attachmentUrl = request.AttachmentFile != null
                ? (await _uploadPhotoService.UploadPhotoAsync(request.AttachmentFile)).OriginalUrl
                : null;

            var message = new ChatDoctorMessages
            {
                ChatDoctorMessageId = Guid.NewGuid(),
                ConsultanSessionId = sessionId,
                SenderId = isDoctorRequest ? session.DoctorId : session.MemberId,
                Type = isDoctorRequest ? SenderType.Doctor : SenderType.User,
                Content = request.Content,
                AttachmentUrl = attachmentUrl,
                IsRead = false,
                SendAt = DateTime.Now
            };

            // Cập nhật bộ đếm
            if (isDoctorRequest) session.UnreadCountMember += 1;
            else session.UnreadCountDoctor += 1;

            await _unitOfWork.Repository<ChatDoctorMessages>().AddAsync(message);
            await _unitOfWork.CompleteAsync();

            // Thông báo và SignalR
            Guid receiverUserId = isDoctorRequest ? (session.Member?.UserId ?? Guid.Empty) : session.Doctor.UserId;
            string senderName = isDoctorRequest ? (session.Doctor?.FullName ?? "Bác sĩ") : (session.Member?.FullName ?? "Bệnh nhân");

            await _notificationService.SendNotificationAsync(receiverUserId, $"💬 {senderName}", request.Content ?? "[Hình ảnh]", ChatActionTypes.NEW_CHAT_MESSAGE, sessionId);

            var responseData = MapToResponse(message, session);
            await _hubContext.Clients.Group($"User_{receiverUserId}").SendAsync("ReceiveMessage", responseData);
            await _hubContext.Clients.Group($"User_{currentUserId}").SendAsync("ReceiveMessage", responseData);

            return ApiResponse<ChatDoctorMessageResponse>.Ok(responseData);
        }

        public async Task<ApiResponse<bool>> MarkMessagesAsReadAsync(Guid sessionId, Guid currentUserId, bool isDoctorRequest)
        {
            var session = await _unitOfWork.Repository<ConsultationSessions>()
                .GetQueryable()
                .Include(s => s.Doctor)
                .Include(s => s.Member)
                .FirstOrDefaultAsync(s => s.ConsultanSessionId == sessionId);

            if (session == null) return ApiResponse<bool>.Fail("Phiên tư vấn không tồn tại.", 404);

            // Kiểm tra quyền: Hỗ trợ cả UserId và MemberId (Logic bạn đã viết rất tốt)
            bool hasAccess = isDoctorRequest
                ? (session.Doctor != null && session.Doctor.UserId == currentUserId)
                : (session.MemberId == currentUserId || session.Member?.UserId == currentUserId);

            if (!hasAccess) return ApiResponse<bool>.Fail("Access Denied", 403);

            // Xác định đối tượng gửi tin nhắn để đánh dấu đã đọc
            var targetSenderType = isDoctorRequest ? SenderType.User : SenderType.Doctor;

            // 1. Cập nhật trạng thái từng tin nhắn trong DB
            var unreadMessages = await _unitOfWork.Repository<ChatDoctorMessages>()
                .FindAsync(m => m.ConsultanSessionId == sessionId
                                && m.Type == targetSenderType
                                && !m.IsRead);

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                    _unitOfWork.Repository<ChatDoctorMessages>().Update(msg);
                }

                // 2. [QUAN TRỌNG]: RESET BỘ ĐẾM TRONG SESSION
                if (isDoctorRequest) session.UnreadCountDoctor = 0;
                else session.UnreadCountMember = 0;

                await _unitOfWork.CompleteAsync();

                // 3. SignalR thông báo cập nhật UI (badge, danh sách tin nhắn)
                var doctorUserId = session.Doctor?.UserId ?? Guid.Empty;
                var memberUserId = session.Member?.UserId ?? Guid.Empty;

                if (doctorUserId != Guid.Empty)
                    await _hubContext.Clients.Group($"User_{doctorUserId}").SendAsync("ReceiveMessageUpdate", new { sessionId });

                if (memberUserId != Guid.Empty)
                    await _hubContext.Clients.Group($"User_{memberUserId}").SendAsync("ReceiveMessageUpdate", new { sessionId });
            }

            return ApiResponse<bool>.Ok(true);
        }

        private async Task<bool> ValidateAccessAsync(ConsultationSessions session, Guid currentUserId, bool isDoctorRequest)
        {
            // 1. Nếu là Người giám hộ
            if (session.GuardianUserId.HasValue && session.GuardianUserId.Value == currentUserId)
                return true;

            if (isDoctorRequest)
            {
                // GIẢI PHÁP: Sử dụng session.Doctor đã được Include từ hàm gọi
                // KHÔNG dùng repository.FindAsync ở đây nữa để tránh lỗi Tracking
                if (session.Doctor == null)
                {
                    // Trường hợp hy hữu nếu quên Include, ta mới dùng AsNoTracking để tìm
                    return false;
                }

                return session.Doctor.UserId == currentUserId;
            }
            else
            {
                // Kiểm tra quyền bệnh nhân/gia đình (Hàm này nên dùng AsNoTracking bên trong)
                return await _currentUserService.CheckAccess(session.MemberId, currentUserId);
            }
        }
        public async Task<ApiResponse<IEnumerable<ChatSessionSummaryResponse>>> GetSessionsByFamilyIdAsync(Guid familyId, Guid currentUserId)
        {
            // Check access... (AsNoTracking)
            var familyMembers = await _unitOfWork.Repository<Members>()
                .GetQueryable()
                .AsNoTracking()
                .Where(m => m.FamilyId == familyId)
                .ToListAsync();

            if (!familyMembers.Any(m => m.UserId == currentUserId || m.MemberId == currentUserId))
                return ApiResponse<IEnumerable<ChatSessionSummaryResponse>>.Fail("Access Denied", 403);

            var memberIds = familyMembers.Select(m => m.MemberId).ToList();

            var sessions = await _unitOfWork.Repository<ConsultationSessions>()
                .GetQueryable()
                .AsNoTracking()
                .Include(s => s.Doctor)
                .Include(s => s.Messages.OrderByDescending(m => m.SendAt).Take(1)) // Chỉ lấy tin cuối
                .Where(s => memberIds.Contains(s.MemberId))
                .ToListAsync();

            var result = sessions.Select(s => new ChatSessionSummaryResponse
            {
                SessionId = s.ConsultanSessionId,
                PartnerName = s.Doctor?.FullName ?? "Bác sĩ",
                PartnerAvatar = s.Doctor?.LicenseImage,
                Status = s.Status,
                LastMessage = s.Messages.FirstOrDefault()?.Content,
                LastMessageTime = s.Messages.FirstOrDefault()?.SendAt,
                UnreadCount = s.UnreadCountMember
            }).OrderByDescending(x => x.LastMessageTime).ToList();

            return ApiResponse<IEnumerable<ChatSessionSummaryResponse>>.Ok(result);
        }

        // 4. LẤY TIN NHẮN CHO BÁC SĨ (Dùng trực tiếp UnreadCountDoctor)
        public async Task<ApiResponse<IEnumerable<ChatSessionSummaryResponse>>> GetSessionsByDoctorIdAsync(Guid doctorId, Guid currentUserId)
        {
            var sessions = await _unitOfWork.Repository<ConsultationSessions>()
                .FindAsync(s => s.DoctorId == doctorId, includeProperties: "Member,Messages");

            var result = sessions.Select(s => new ChatSessionSummaryResponse
            {
                SessionId = s.ConsultanSessionId,
                PartnerName = s.Member?.FullName ?? "Bệnh nhân",
                PartnerAvatar = s.Member?.AvatarUrl,
                Status = s.Status,
                LastMessage = s.Messages?.OrderByDescending(m => m.SendAt).FirstOrDefault()?.Content,
                LastMessageTime = s.Messages?.OrderByDescending(m => m.SendAt).FirstOrDefault()?.SendAt,
                // LẤY TRỰC TIẾP TỪ CỘT UNREAD CỦA DOCTOR
                UnreadCount = s.UnreadCountDoctor,
                ExpiredAt = s.EndedAt.HasValue ? s.EndedAt.Value.AddHours(1) : null              
            })
            .OrderByDescending(x => x.LastMessageTime)
            .ToList();

            return ApiResponse<IEnumerable<ChatSessionSummaryResponse>>.Ok(result);
        }

        // ==========================================
        // 3. LẤY CHI TIẾT 1 PHÒNG CHAT (DÙNG CHO HEADER MÀN HÌNH CHAT)
        // ==========================================
        public async Task<ApiResponse<ChatSessionSummaryResponse>> GetSessionDetailsAsync(Guid sessionId, Guid currentUserId, bool isDoctorRequest)
        {
            var session = (await _unitOfWork.Repository<ConsultationSessions>()
                .FindAsync(s => s.ConsultanSessionId == sessionId, includeProperties: "Doctor,Member")).FirstOrDefault();

            if (session == null)
                return ApiResponse<ChatSessionSummaryResponse>.Fail("Không tìm thấy phiên tư vấn.", 404);

            // Tùy theo góc nhìn (Bác sĩ hay Bệnh nhân) để trả về thông tin Partner cho đúng
            var response = new ChatSessionSummaryResponse
            {
                SessionId = session.ConsultanSessionId,
                Status = session.Status,
                PartnerName = isDoctorRequest ? (session.Member?.FullName ?? "Unknown") : (session.Doctor?.FullName ?? "Unknown"),
                PartnerAvatar = isDoctorRequest ? session.Member?.AvatarUrl : session.Doctor?.LicenseImage
            };

            return ApiResponse<ChatSessionSummaryResponse>.Ok(response);
        }

        private ChatDoctorMessageResponse MapToResponse(ChatDoctorMessages message, ConsultationSessions session)
        {
            // Xác định tên và avatar dựa trên Type
            bool isDoctor = message.Type == SenderType.Doctor;
            string senderName = isDoctor ? session.Doctor?.FullName : session.Member?.FullName;
            string senderAvatar = isDoctor ? session.Doctor?.LicenseImage : session.Member?.AvatarUrl;

            return new ChatDoctorMessageResponse
            {
                MessageId = message.ChatDoctorMessageId,
                SessionId = message.ConsultanSessionId,
                SenderId = message.SenderId,
                SenderType = (int)message.Type,
                SenderName = senderName ?? "Unknown",
                SenderAvatar = senderAvatar ?? "",
                Content = message.Content,
                AttachmentUrl = message.AttachmentUrl,
                IsRead = message.IsRead,
                SendAt = message.SendAt
            };
        }
    }
}
