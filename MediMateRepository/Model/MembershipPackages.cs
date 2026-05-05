using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class MembershipPackages
    {
        [Key]
        public Guid PackageId { get; set; } = Guid.NewGuid();
        public string PackageName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "VND";
        public int DurationDays { get; set; }
        public int MemberLimit { get; set; }
        public int OcrLimit { get; set; }
        // Đã chuyển sang mô hình Pay-per-booking, gói không còn chứa số lượt khám
        // public int ConsultantLimit { get; set; }
        public string? Description { get; set; }

        // ── Feature Flags ────────────────────────────────────────────────
        /// <summary>
        /// Cho phép xem lại video phiên khám sau khi kết thúc.
        /// Freemium = false, các gói trả phí = true.
        /// </summary>
        public bool AllowVideoRecordingAccess { get; set; } = false;

        /// <summary>
        /// Bật tính năng cảnh báo tương tác thuốc bằng AI (Groq).
        /// Freemium = false, các gói trả phí = true.
        /// </summary>
        public bool HealthAlertEnabled { get; set; } = false;
    }
}
