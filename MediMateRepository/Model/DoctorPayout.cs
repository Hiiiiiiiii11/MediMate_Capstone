using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class DoctorPayout
    {
        [Key]
        public Guid PayoutId { get; set; } = Guid.NewGuid();
        public Guid ConsultationId { get; set; }
        public Guid RateId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CalculatedAt { get; set; } = DateTime.Now;
        public DateTime? PaidAt { get; set; }
        public virtual ConsultationSessions ConsultationSession { get; set; } = null!;
        public virtual DoctorPayoutRate Rate { get; set; } = null!;
    }
}
