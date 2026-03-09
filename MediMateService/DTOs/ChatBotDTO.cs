using MediMateRepository.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class SendMessageRequest
    {
        // Nếu truyền lên null hoặc rỗng -> Tạo phiên chat mới
        public Guid? SessionId { get; set; }

        [Required(ErrorMessage = "Nội dung tin nhắn không được để trống")]
        public string Content { get; set; } = string.Empty;
    }

    // Trả về danh sách phiên chat
    public class ChatSessionResponse
    {
        public Guid SessionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartAt { get; set; }
        public DateTime LastMessageAt { get; set; }
    }

    // Trả về chi tiết tin nhắn
    public class ChatMessageResponse
    {
        public Guid MessageId { get; set; }
        public ChatRole Role { get; set; } // 0: User, 1: Bot
        public string RoleName => Role.ToString();
        public string Content { get; set; } = string.Empty;
        public DateTime CreateAt { get; set; }
    }
}
