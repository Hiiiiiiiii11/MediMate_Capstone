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
}
