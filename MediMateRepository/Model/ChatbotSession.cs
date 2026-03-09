using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class ChatbotSession
    {
        public Guid BotSessionId { get; set; }
        public Guid MemberId { get; set; }
        public string SessionTitle { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime LastMessageAt { get; set; }
        public bool IsActive { get; set; }
        public virtual Members Member { get; set; }
        public virtual ICollection<ChatbotMessages> Messages { get; set; } = new List<ChatbotMessages>();
    }
}
