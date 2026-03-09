using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{

    public class ChatbotMessages
    {
        public Guid BotMessageId { get; set; }
        public Guid BotSessionId { get; set; }
        public ChatRole Role { get; set; }
        public string Content { get; set; }
        public string? MetadataJson { get; set; }
        public DateTime CreateAt { get; set; }

        public virtual ChatbotSession? Session { get; set; }
    }
    public enum ChatRole
    {
        User = 0,
        Bot = 1
    }
}
