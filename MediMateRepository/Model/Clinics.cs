using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class Clinics
    {
        [Key]
        public Guid ClinicId { get; set; } = Guid.NewGuid();
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Address { get; set; } = string.Empty;
        
        // Giấy phép hoạt động của Phòng khám
        public string LicenseUrl { get; set; } = string.Empty;
        
        public string LogoUrl { get; set; } = string.Empty;
        
        // Trạng thái hoạt động của phòng khám (Tạm ngưng, Đang mở...)
        public bool IsActive { get; set; } = true;
        
        // Admin quản lý phòng khám này (Có thể null nếu do Admin hệ thống tạo)
        public Guid? AdminId { get; set; }
        public virtual User? Admin { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        [Required]
        public string Email { get; set; }

        // ── Thông tin ngân hàng (bắt buộc khi tạo — dùng để nhận payout) ──────
        [Required]
        public string BankName { get; set; } = string.Empty;

        [Required]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required]
        public string BankAccountHolder { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<ClinicDoctors> ClinicDoctors { get; set; } = new List<ClinicDoctors>();
        public virtual ICollection<ClinicContract> ClinicContracts { get; set; } = new List<ClinicContract>();
    }
}
