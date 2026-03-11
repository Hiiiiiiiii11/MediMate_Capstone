using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class ConsultantSession
    {
        public Guid ConsultanSessionId { get; set; }
        //public Guid AppointmentId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }
        public string RecordUrl { get; set; }
        public string Status { get; set; }
        public string DoctorNote { get; set; }
        public virtual Members Member { get; set; }
        public virtual Doctors Doctor { get; set; }
        public virtual ICollection<ChatDoctorMessage> Messages { get; set; } = new List<ChatDoctorMessage>();

    }
}
