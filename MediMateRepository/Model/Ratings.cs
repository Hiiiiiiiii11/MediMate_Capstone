using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class Ratings
    {
        [Key]
        public Guid RatingId { get; set; } = Guid.NewGuid();
        public Guid ConsultanSessionId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public virtual ConsultationSessions ConsultationSession { get; set; }
        public virtual Doctors Doctor { get; set; }
        public virtual Members Member { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
