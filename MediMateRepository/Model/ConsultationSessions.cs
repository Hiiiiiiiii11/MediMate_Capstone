using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class ConsultationSessions
    {
        public Guid ConsultanSessionId { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string? RecordUrl { get; set; }
        public string Status { get; set; } = "Processing";

        // Tracking join status (event-driven → InProgress khi cả 2 join)
        public bool UserJoined { get; set; } = false;
        public bool DoctorJoined { get; set; } = false;

        // ─── Guardian (Người giám hộ) ─────────────────────────────────────
        // Được set khi member.UserId == null (dependent member)
        // GuardianUserId = Family.CreateBy của gia đình member đó
        public Guid? GuardianUserId { get; set; }
        public bool GuardianJoined { get; set; } = false;

        // Ghi chú hệ thống: trễ giờ, khách huỷ no-show, v.v.
        public string? Note { get; set; }

        // Ghi chú của bác sĩ (kê đơn, lời dặn dò)
        public string? DoctorNote { get; set; }

        public virtual Appointments Appointment { get; set; }
        public virtual ICollection<ChatDoctorMessages> Messages { get; set; } = new List<ChatDoctorMessages>();
        public virtual Members Member { get; set; } = null!;
        public virtual Doctors Doctor { get; set; } = null!;
        public virtual ICollection<Ratings> Ratings { get; set; } = new List<Ratings>();
        public virtual User? Guardian { get; set; }
    }
}
