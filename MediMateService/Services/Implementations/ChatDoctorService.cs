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
            var session = (await _unitOfWork.Repository<ConsultationSessions>()
                .FindAsync(s => s.ConsultanSessionId == sessionId, "Member,Doctor")).FirstOrDefault();

            if (session == null) return ApiResponse<IEnumerable<ChatDoctorMessageResponse>>.Fail("Phiên tư vấn không tồn tại.", 404);

            // Kiểm tra quyền
            if (!await ValidateAccessAsync(session, currentUserId, isDoctorRequest))
                return ApiResponse<IEnumerable<ChatDoctorMessageResponse>>.Fail("Bạn không có quyền xem phiên chat này.", 403);

            var messages = await _unitOfWork.Repository<ChatDoctorMessages>()
                .FindAsync(m => m.ConsultanSessionId == sessionId);

            // Dùng hàm MapToResponse để chuyển đổi List
            var response = messages.OrderBy(m => m.SendAt)
                                   .Select(m => MapToResponse(m, session));

            return ApiResponse<IEnumerable<ChatDoctorMessageResponse>>.Ok(response);
        }

        public async Task<ApiResponse<ChatDoctorMessageResponse>> SendMessageAsync(Guid sessionId, Guid currentUserId, SendChatDoctorRequest request, bool isDoctorRequest)
        {
            var session = (await _unitOfWork.Repository<ConsultationSessions>()
                .FindAsync(s => s.ConsultanSessionId == sessionId, "Member,Doctor")).FirstOrDefault();

            if (session == null) return ApiResponse<ChatDoctorMessageResponse>.Fail("Phiên tư vấn không tồn tại.", 404);

            //// Kiểm tra phiên chat có đang mở không
            if (session.Status == "Rejected")
                return ApiResponse<ChatDoctorMessageResponse>.Fail("Phiên tư vấn đã bị từ chối, không thể gửi tin nhắn.", 400);

            if (session.Status == ConsultationSessionConstants.ENDED && !isDoctorRequest)
            {
                // Sau khi phiên kết thúc: User không thể nhắn thêm, Doctor vẫn có thể nhắn
                return ApiResponse<ChatDoctorMessageResponse>.Fail(
                    "Phiên tư vấn đã kết thúc. Bạn không thể gửi thêm tin nhắn, nhưng bác sĩ vẫn có thể liên hệ với bạn.", 403);
            }

            if (!await ValidateAccessAsync(session, currentUserId, isDoctorRequest))
                return ApiResponse<ChatDoctorMessageResponse>.Fail("Bạn không có quyền chat trong phiên này.", 403);

            // Xử lý upload ảnh (nếu có)
            string? attachmentUrl = null;
            if (request.AttachmentFile != null)
            {
                var uploadResult = await _uploadPhotoService.UploadPhotoAsync(request.AttachmentFile);
                attachmentUrl = uploadResult.OriginalUrl;
            }

            // Xác định SenderId và Type
            Guid senderId = isDoctorRequest ? session.DoctorId : session.MemberId;
            SenderType senderType = isDoctorRequest ? SenderType.Doctor : SenderType.User;

            var message = new ChatDoctorMessages
            {
                ChatDoctorMessageId = Guid.NewGuid(),
                ConsultanSessionId = sessionId,
                SenderId = senderId,
                Type = senderType,
                Content = request.Content,
                AttachmentUrl = attachmentUrl,
                IsRead = false, // Tin nhắn mới gửi thì chưa đọc
                SendAt = DateTime.Now
            };

            await _unitOfWork.Repository<ChatDoctorMessages>().AddAsync(message);
            await _unitOfWork.CompleteAsync();

            Guid receiverUserId = isDoctorRequest ? session.Member.UserId ?? Guid.Empty : session.Doctor.UserId;

            // 2. Tên người vừa gửi để hiện lên thông báo
            string senderName = isDoctorRequest ? (session.Doctor?.FullName ?? "Bác sĩ") : (session.Member?.FullName ?? "Bệnh nhân");

            // 3. Nội dung thông báo (Nếu chỉ gửi ảnh thì hiện chữ "[Hình ảnh đính kèm]")
            string notifBody = string.IsNullOrWhiteSpace(request.Content) ? "[Hình ảnh đính kèm]" : request.Content;

            // Bắn thông báo! (Tự động lưu DB và gọi Firebase)
            await _notificationService.SendNotificationAsync(
                userId: receiverUserId,
                title: $"💬 Tin nhắn mới từ {senderName}",
                message: notifBody,
                type: ChatActionTypes.NEW_CHAT_MESSAGE,
                referenceId: sessionId // Gửi kèm SessionId để lúc bấm vào thông báo App sẽ mở thẳng phòng chat này ra
            );

            // Bắn SignalR
            var responseData = MapToResponse(message, session);
            await _hubContext.Clients.Group($"User_{receiverUserId}").SendAsync("ReceiveMessage", responseData);
            await _hubContext.Clients.Group($"User_{currentUserId}").SendAsync("ReceiveMessage", responseData);

            // Dùng hàm MapToResponse cho tin nhắn vừa tạo
            return ApiResponse<ChatDoctorMessageResponse>.Ok(responseData);
        }

        public async Task<ApiResponse<bool>> MarkMessagesAsReadAsync(Guid sessionId, Guid currentUserId, bool isDoctorRequest)
        {
            var session = (await _unitOfWork.Repository<ConsultationSessions>()
                .FindAsync(s => s.ConsultanSessionId == sessionId, "Doctor,Member")).FirstOrDefault();
                
            if (session == null) return ApiResponse<bool>.Fail("Phiên tư vấn không tồn tại.", 404);

            if (!await ValidateAccessAsync(session, currentUserId, isDoctorRequest))
                return ApiResponse<bool>.Fail("Access Denied", 403);

            var targetTypeToMark = isDoctorRequest ? SenderType.User : SenderType.Doctor;

            var unreadMessages = await _unitOfWork.Repository<ChatDoctorMessages>()
                .FindAsync(m => m.ConsultanSessionId == sessionId
                                && m.Type == targetTypeToMark
                                && !m.IsRead);

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                    _unitOfWork.Repository<ChatDoctorMessages>().Update(msg);
                }
                await _unitOfWork.CompleteAsync();

                // SignalR thông báo đã đọc
                var participant1 = session.Doctor?.UserId ?? Guid.Empty;
                var participant2 = session.Member?.UserId ?? Guid.Empty;

                if (participant1 != Guid.Empty) await _hubContext.Clients.Group($"User_{participant1}").SendAsync("ReceiveMessageUpdate");
                if (participant2 != Guid.Empty) await _hubContext.Clients.Group($"User_{participant2}").SendAsync("ReceiveMessageUpdate");
            }

            return ApiResponse<bool>.Ok(true);
        }

        private async Task<bool> ValidateAccessAsync(ConsultationSessions session, Guid currentUserId, bool isDoctorRequest)
        {
            if (isDoctorRequest)
            {
                // Tìm DoctorProfile của currentUserId đang đăng nhập
                var doctorProfile = (await _unitOfWork.Repository<Doctors>()
                    .FindAsync(d => d.UserId == currentUserId)).FirstOrDefault();

                // Bác sĩ này phải đúng là bác sĩ được phân công trong Session
                return doctorProfile != null && doctorProfile.DoctorId == session.DoctorId;
            }
            else
            {
                // Kiểm tra xem User có quyền với MemberId trong Session không
                return await _currentUserService.CheckAccess(session.MemberId, currentUserId);
            }
        }

        public async Task<ApiResponse<IEnumerable<ChatSessionSummaryResponse>>> GetSessionsByFamilyIdAsync(Guid familyId, Guid currentUserId)
        {
            // Kiểm tra user có thuộc family này không
                var isFamilyMember = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == familyId && (m.UserId == currentUserId || m.MemberId == currentUserId))).Any();

            if (!isFamilyMember)
                return ApiResponse<IEnumerable<ChatSessionSummaryResponse>>.Fail("Bạn không có quyền truy cập tin nhắn của gia đình này.", 403);

            // Lấy tất cả thành viên trong gia đình
            var familyMembers = await _unitOfWork.Repository<Members>().FindAsync(m => m.FamilyId == familyId);
            var memberIds = familyMembers.Select(m => m.MemberId).ToList();

            // Lấy các phiên chat có MemberId nằm trong danh sách trên
            // Include thêm Doctor và Messages để lấy thông tin hiển thị
            var sessions = await _unitOfWork.Repository<ConsultationSessions>()
                .FindAsync(s => memberIds.Contains(s.MemberId), includeProperties: "Doctor,Messages");

            var result = sessions.Select(s => new ChatSessionSummaryResponse
            {
                SessionId = s.ConsultanSessionId,
                PartnerName = s.Doctor?.FullName ?? "Bác sĩ ẩn danh",
                PartnerAvatar = s.Doctor?.LicenseImage, // Hoặc trường AvatarUrl nếu bảng Doctor của bạn có
                Status = s.Status,

                // Sắp xếp tin nhắn mới nhất lên đầu để lấy nội dung hiển thị
                // SỬA: Thay Message thành Content, CreatedAt thành SendAt
                LastMessage = s.Messages?.OrderByDescending(m => m.SendAt).FirstOrDefault()?.Content,
                LastMessageTime = s.Messages?.OrderByDescending(m => m.SendAt).FirstOrDefault()?.SendAt,

                // SỬA: Đếm tin nhắn dựa trên Enum SenderType
                UnreadCount = s.Messages?.Count(m => m.Type == SenderType.Doctor && !m.IsRead) ?? 0
            })
            .OrderByDescending(x => x.LastMessageTime ?? DateTime.MinValue) // Đẩy phòng chat có tin mới nhất lên đầu
            .ToList();

            return ApiResponse<IEnumerable<ChatSessionSummaryResponse>>.Ok(result);
        }


        public async Task<ApiResponse<IEnumerable<ChatSessionSummaryResponse>>> GetSessionsByDoctorIdAsync(Guid doctorId, Guid currentUserId)
        {
            var doctor = (await _unitOfWork.Repository<Doctors>().FindAsync(d => d.DoctorId == doctorId)).FirstOrDefault();
            if (doctor == null || doctor.UserId != currentUserId)
                return ApiResponse<IEnumerable<ChatSessionSummaryResponse>>.Fail("Bạn không có quyền xem tin nhắn của bác sĩ này.", 403);

            var sessions = await _unitOfWork.Repository<ConsultationSessions>()
                .FindAsync(s => s.DoctorId == doctorId, includeProperties: "Member,Messages");

            var result = sessions.Select(s => new ChatSessionSummaryResponse
            {
                SessionId = s.ConsultanSessionId,
                PartnerName = s.Member?.FullName ?? "Bệnh nhân ẩn danh",
                PartnerAvatar = s.Member?.AvatarUrl,
                Status = s.Status,

                // SỬA: Thay Message thành Content, CreatedAt thành SendAt
                LastMessage = s.Messages?.OrderByDescending(m => m.SendAt).FirstOrDefault()?.Content,
                LastMessageTime = s.Messages?.OrderByDescending(m => m.SendAt).FirstOrDefault()?.SendAt,

                // SỬA: Đếm tin nhắn dựa trên Enum SenderType
                UnreadCount = s.Messages?.Count(m => m.Type == SenderType.Doctor && !m.IsRead) ?? 0
            })
            .OrderByDescending(x => x.LastMessageTime ?? DateTime.MinValue)
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
