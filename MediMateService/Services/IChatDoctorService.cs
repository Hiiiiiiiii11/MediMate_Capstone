using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IChatDoctorService
    {
        // Lấy lịch sử chat của 1 phiên tư vấn
        Task<ApiResponse<IEnumerable<ChatDoctorMessageResponse>>> GetSessionMessagesAsync(Guid sessionId, Guid currentUserId, bool isDoctorRequest);

        // Gửi tin nhắn
        Task<ApiResponse<ChatDoctorMessageResponse>> SendMessageAsync(Guid sessionId, Guid currentUserId, SendChatDoctorRequest request, bool isDoctorRequest);

        // Đánh dấu toàn bộ tin nhắn của đối phương là "Đã đọc"
        Task<ApiResponse<bool>> MarkMessagesAsReadAsync(Guid sessionId, Guid currentUserId, bool isDoctorRequest);
    }
}
