using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class SendChatDoctorRequest
    {
        public string Content { get; set; } = string.Empty;

        // Cho phép gửi kèm file ảnh khám bệnh, kết quả xét nghiệm...
        public IFormFile? AttachmentFile { get; set; }
    }

    public class ChatDoctorMessageResponse
    {
        public Guid MessageId { get; set; }
        public Guid SessionId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderAvatar { get; set; } = string.Empty;
        public int SenderType { get; set; } // 1: User, 2: Doctor
        public string Content { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime SendAt { get; set; }
    }

    public class ChatSessionSummaryResponse
    {
        public Guid SessionId { get; set; }

        // Thông tin người đối diện (Nếu là User đăng nhập thì hiện tên Bác sĩ, và ngược lại)
        public string PartnerName { get; set; } = string.Empty;
        public string? PartnerAvatar { get; set; }

        // Trạng thái phiên khám (Active, Ended, Cancelled...)
        public string Status { get; set; } = string.Empty;

        // Nội dung tin nhắn cuối cùng (để hiển thị rút gọn ở danh sách)
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }

        // Số tin nhắn chưa đọc trong cái phòng chat này
        public int UnreadCount { get; set; }
    }
}
