using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    /// <summary>
    /// Thông tin ngân hàng của User — dùng để hoàn tiền (refund) khi hủy lịch hẹn.
    /// Một User chỉ có tối đa 1 tài khoản ngân hàng chính.
    /// </summary>
    public class UserBankAccount
    {
        [Key]
        public Guid BankAccountId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string BankName { get; set; } = string.Empty;

        [Required]
        public string AccountNumber { get; set; } = string.Empty;

        [Required]
        public string AccountHolder { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual User User { get; set; } = null!;
    }
}
