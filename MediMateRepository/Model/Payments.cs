using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class Payments
    {
        [Key]
        public Guid PaymentId { get; set; } = Guid.NewGuid();
        public Guid? SubscriptionId { get; set; }
        public Guid? AppointmentId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentContent { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public virtual FamilySubscriptions? Subscription { get; set; }
        public virtual Appointments? Appointment { get; set; }
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Transactions> Transactions { get; set; } = new List<Transactions>();
    }
}
