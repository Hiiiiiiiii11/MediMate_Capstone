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
        public enum Role { User, Bot }
        public string Content { get; set; }
        public JsonArray Metadata { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
