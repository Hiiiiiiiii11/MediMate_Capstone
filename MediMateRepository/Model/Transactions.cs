using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class Transactions
    {
        [Key]
        public Guid TransactionId { get; set; } = Guid.NewGuid();
        public Guid PaymentId { get; set; }
        public string? GatewayName { get; set; }
        public string? GatewayTransactionId { get; set; }
        public string TransactionStatus { get; set; } = "Pending";
        public decimal AmountPaid { get; set; }

        public virtual Payments Payment { get; set; } = null!;
    }
}
