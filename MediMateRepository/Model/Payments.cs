using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class Payments
    {
        [Key]
        public Guid PaymentId { get; set; } = Guid.NewGuid();
        public Guid SubscriptionId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentContent { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual FamilySubscriptions Subscription { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
