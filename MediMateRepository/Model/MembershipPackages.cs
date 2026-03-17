using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class MembershipPackages
    {
        [Key]
        public Guid PackageId { get; set; } = Guid.NewGuid();
        public string PackageName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "VND";
        public int DurationDays { get; set; }
        public int MemberLimit { get; set; }
        public int OcrLimit { get; set; }
        public int ConsultantLimit { get; set; }
        public string? Description { get; set; }
    }
}
