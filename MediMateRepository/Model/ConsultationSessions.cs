using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class ConsultationSessions
    {
        [Key]
        public Guid SessionId { get; set; } = Guid.NewGuid();
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsCompleted { get; set; }
    }
}
