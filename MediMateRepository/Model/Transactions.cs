using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediMateRepository.Model
{
    public class Transactions
    {
        [Key]
        public Guid TransactionId { get; set; } = Guid.NewGuid();

        [Required]
        public string TransactionCode { get; set; } = string.Empty;
        public Guid? PaymentId { get; set; }
        public Guid? PayoutId { get; set; }
        public string? GatewayName { get; set; }
        public string? GatewayTransactionId { get; set; }
        public string TransactionStatus { get; set; } = "Pending";
        public decimal AmountPaid { get; set; }
        public string TransactionType { get; set; } = "Tiền nhận vào";
        public string? GatewayResponse { get; set; }
        public DateTime? PaidAt { get; set; }
        public virtual Payments? Payment { get; set; }
        public virtual DoctorPayout? Payout { get; set; }
    }
}
