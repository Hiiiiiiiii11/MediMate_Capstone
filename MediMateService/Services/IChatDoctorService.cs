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

        Task<ApiResponse<IEnumerable<ChatSessionSummaryResponse>>> GetSessionsByFamilyIdAsync(Guid familyId, Guid currentUserId);

        // 2. Lấy tất cả các phiên chat của một Bác sĩ (Dành cho Doctor)
        // Mục đích: Hiển thị tab "Tin nhắn" trên app của bác sĩ
        Task<ApiResponse<IEnumerable<ChatSessionSummaryResponse>>> GetSessionsByDoctorIdAsync(Guid doctorId, Guid currentUserId);

        // 3. Lấy thông tin chi tiết của 1 phiên chat (Header Chat)
        // Mục đích: Khi ấn vào phòng chat, cần gọi API này để lấy Avatar, Tên người đối diện, Trạng thái (Active/Ended)
        Task<ApiResponse<ChatSessionSummaryResponse>> GetSessionDetailsAsync(Guid sessionId, Guid currentUserId, bool isDoctorRequest);
    }
}
