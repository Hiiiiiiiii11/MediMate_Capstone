using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services.Implementations
{
    public class ChatDoctorService : IChatDoctorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUploadPhotoService _uploadPhotoService;

        public ChatDoctorService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IUploadPhotoService uploadPhotoService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _uploadPhotoService = uploadPhotoService;
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

            //// Kiểm tra phiên chat có đang mở không (VD: Status = "In-Progress")
            //if (session.Status == "Completed" || session.Status == "Cancelled")
            //    return ApiResponse<ChatDoctorMessageResponse>.Fail("Phiên tư vấn đã kết thúc, không thể gửi thêm tin nhắn.", 400);

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

            // Dùng hàm MapToResponse cho tin nhắn vừa tạo
            return ApiResponse<ChatDoctorMessageResponse>.Ok(MapToResponse(message, session));
        }

        public async Task<ApiResponse<bool>> MarkMessagesAsReadAsync(Guid sessionId, Guid currentUserId, bool isDoctorRequest)
        {
            var session = await _unitOfWork.Repository<ConsultationSessions>().GetByIdAsync(sessionId);
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
