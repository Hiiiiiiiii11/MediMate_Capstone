using System;
using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class ClinicContract
    {
        [Key]
        public Guid ContractId { get; set; } = Guid.NewGuid();
        
        // Liên kết với Phòng khám
        public Guid ClinicId { get; set; }
        public virtual Clinics Clinic { get; set; } = null!;
        
        // File hợp đồng bản cứng/mềm giữa Admin và Phòng khám
        public string FileUrl { get; set; } = string.Empty;
        
        // Ngày tháng hiệu lực
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        // Trạng thái hợp đồng của phòng khám với hệ thống
        public string Status { get; set; } = "Active"; // Active, Expired, Terminated
        
        public string? Note { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
