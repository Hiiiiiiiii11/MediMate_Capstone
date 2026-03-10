using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public  interface IChatBotService
    {
        // Lấy danh sách lịch sử các phiên chat của 1 thành viên
        Task<ApiResponse<IEnumerable<ChatSessionResponse>>> GetMemberSessionsAsync(Guid memberId, Guid currentUserId);

        // Lấy chi tiết lịch sử tin nhắn trong 1 phiên chat
        Task<ApiResponse<IEnumerable<ChatMessageResponse>>> GetSessionMessagesAsync(Guid sessionId, Guid currentUserId);

        // Gửi tin nhắn và nhận phản hồi từ AI
        Task<ApiResponse<ChatMessageResponse>> SendMessageAsync(Guid memberId, Guid currentUserId, SendMessageRequest request);

        // Xóa phiên chat
        Task<ApiResponse<bool>> DeleteSessionAsync(Guid sessionId, Guid currentUserId);
    }
}
