using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class DoctorPayoutRate
    {
        [Key]
        public Guid RateId { get; set; } = Guid.NewGuid();
        public string ConsultationType { get; set; } = string.Empty;
        public decimal AmountPerSession { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
