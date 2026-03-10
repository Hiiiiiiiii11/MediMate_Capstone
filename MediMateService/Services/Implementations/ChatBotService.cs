using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;

namespace MediMateService.Services.Implementations
{
    public class ChatbotService : IChatBotService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public ChatbotService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<IEnumerable<ChatSessionResponse>>> GetMemberSessionsAsync(Guid memberId, Guid currentUserId)
        {
            if (!await _currentUserService.CheckAccess(memberId, currentUserId))
            {
                return ApiResponse<IEnumerable<ChatSessionResponse>>.Fail("Không có quyền truy cập.", 403);
            }

            var sessions = await _unitOfWork.Repository<ChatbotSession>()
                .FindAsync(s => s.MemberId == memberId && s.IsActive);

            var response = sessions.OrderByDescending(s => s.LastMessageAt).Select(s => new ChatSessionResponse
            {
                SessionId = s.BotSessionId,
                Title = s.SessionTitle,
                StartAt = s.StartAt,
                LastMessageAt = s.LastMessageAt
            });

            return ApiResponse<IEnumerable<ChatSessionResponse>>.Ok(response);
        }

        public async Task<ApiResponse<IEnumerable<ChatMessageResponse>>> GetSessionMessagesAsync(Guid sessionId, Guid currentUserId)
        {
            var session = await _unitOfWork.Repository<ChatbotSession>().GetByIdAsync(sessionId);
            if (session == null || !session.IsActive)
                return ApiResponse<IEnumerable<ChatMessageResponse>>.Fail("Phiên chat không tồn tại.", 404);

            if (!await _currentUserService.CheckAccess(session.MemberId, currentUserId))
                return ApiResponse<IEnumerable<ChatMessageResponse>>.Fail("Không có quyền truy cập.", 403);

            var messages = await _unitOfWork.Repository<ChatbotMessages>()
                .FindAsync(m => m.BotSessionId == sessionId);

            var response = messages.OrderBy(m => m.CreateAt).Select(m => new ChatMessageResponse
            {
                MessageId = m.BotMessageId,
                Role = m.Role,
                Content = m.Content,
                CreateAt = m.CreateAt
            });

            return ApiResponse<IEnumerable<ChatMessageResponse>>.Ok(response);
        }

        public async Task<ApiResponse<bool>> DeleteSessionAsync(Guid sessionId, Guid currentUserId)
        {
            var session = await _unitOfWork.Repository<ChatbotSession>().GetByIdAsync(sessionId);
            if (session == null) return ApiResponse<bool>.Fail("Phiên chat không tồn tại.", 404);

            if (!await _currentUserService.CheckAccess(session.MemberId, currentUserId))
                return ApiResponse<bool>.Fail("Không có quyền truy cập.", 403);

            // Soft delete (Ẩn đi chứ không xóa hẳn để làm data train AI nếu cần)
            session.IsActive = false;
            _unitOfWork.Repository<ChatbotSession>().Update(session);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Đã xóa phiên chat.");
        }

        // [ĐÃ SỬA CHỮA: Thêm tham số memberId vào đây]
        public async Task<ApiResponse<ChatMessageResponse>> SendMessageAsync(Guid memberId, Guid currentUserId, SendMessageRequest request)
        {
            // 1. Kiểm tra quyền
            if (!await _currentUserService.CheckAccess(memberId, currentUserId))
                return ApiResponse<ChatMessageResponse>.Fail("Không có quyền sử dụng Bot cho thành viên này.", 403);

            ChatbotSession session = null;

            // 2. Xử lý Session (Lấy cũ hoặc tạo mới)
            if (request.SessionId.HasValue && request.SessionId.Value != Guid.Empty)
            {
                session = await _unitOfWork.Repository<ChatbotSession>().GetByIdAsync(request.SessionId.Value);
                if (session == null || !session.IsActive)
                    return ApiResponse<ChatMessageResponse>.Fail("Phiên chat không hợp lệ hoặc đã bị đóng.", 404);

                // Đảm bảo Session này thuộc về đúng MemberId đang gửi request
                if (session.MemberId != memberId)
                    return ApiResponse<ChatMessageResponse>.Fail("Phiên chat này không thuộc về thành viên hiện tại.", 403);
            }
            else
            {
                // Tạo session mới
                string title = request.Content.Length > 30 ? request.Content.Substring(0, 30) + "..." : request.Content;

                session = new ChatbotSession
                {
                    BotSessionId = Guid.NewGuid(),
                    MemberId = memberId,
                    SessionTitle = title,
                    StartAt = DateTime.Now,
                    LastMessageAt = DateTime.Now,
                    IsActive = true
                };
                await _unitOfWork.Repository<ChatbotSession>().AddAsync(session);
            }

            // 3. Lưu tin nhắn của USER
            var userMessage = new ChatbotMessages
            {
                BotMessageId = Guid.NewGuid(),
                BotSessionId = session.BotSessionId,
                Role = ChatRole.User,
                Content = request.Content,
                MetadataJson = null,
                CreateAt = DateTime.Now
            };
            await _unitOfWork.Repository<ChatbotMessages>().AddAsync(userMessage);

            // Cập nhật thời gian chat
            session.LastMessageAt = DateTime.Now;
            _unitOfWork.Repository<ChatbotSession>().Update(session);

            // Lưu xuống DB ngay để phòng trường hợp gọi AI bị lỗi
            await _unitOfWork.CompleteAsync();

            // ----------------------------------------------------
            // 4. GỌI API TRÍ TUỆ NHÂN TẠO (AI)
            var history = await _unitOfWork.Repository<ChatbotMessages>()
                .FindAsync(m => m.BotSessionId == session.BotSessionId);

            var aiResponseContent = await GenerateAIResponseAsync(history.OrderBy(h => h.CreateAt).ToList());
            // ----------------------------------------------------

            // 5. Lưu tin nhắn của BOT
            var botMessage = new ChatbotMessages
            {
                BotMessageId = Guid.NewGuid(),
                BotSessionId = session.BotSessionId,
                Role = ChatRole.Bot,
                Content = aiResponseContent,
                MetadataJson = null,
                CreateAt = DateTime.Now
            };
            await _unitOfWork.Repository<ChatbotMessages>().AddAsync(botMessage);

            session.LastMessageAt = DateTime.Now;
            _unitOfWork.Repository<ChatbotSession>().Update(session);

            await _unitOfWork.CompleteAsync();

            // 6. Trả về phản hồi cho Mobile App
            return ApiResponse<ChatMessageResponse>.Ok(new ChatMessageResponse
            {
                MessageId = botMessage.BotMessageId,
                Role = botMessage.Role, // Bot
                Content = botMessage.Content,
                CreateAt = botMessage.CreateAt
            });
        }

        // =========================================================
        // HÀM PRIVATE GỌI AI
        // =========================================================
        private async Task<string> GenerateAIResponseAsync(List<ChatbotMessages> chatHistory)
        {
            await Task.Delay(1500);

            var userLastMsg = chatHistory.LastOrDefault(m => m.Role == ChatRole.User)?.Content;
            return $"Tôi là MediMate AI. Bạn vừa hỏi: '{userLastMsg}'. Bạn cần tôi tư vấn chi tiết về loại thuốc hay triệu chứng nào không?";
        }
    }
}