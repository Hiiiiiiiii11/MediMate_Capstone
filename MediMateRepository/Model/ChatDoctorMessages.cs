using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class ChatDoctorMessages
    {
        public Guid ChatDoctorMessageId { get; set; }
        public Guid ConsultanSessionId { get; set; }
        public Guid SenderId { get; set; }
        public SenderType Type { get; set; }
        public string Content { get; set; }
        public string? AttachmentUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime SendAt { get; set; }
        public virtual ConsultationSessions ConsultantSession { get; set; }
        public virtual Members Sender { get; set; }
        public virtual Doctors DoctorSender { get; set; }
    }
    public enum SenderType
    {
        User = 1,
        Doctor = 2
    }
}
