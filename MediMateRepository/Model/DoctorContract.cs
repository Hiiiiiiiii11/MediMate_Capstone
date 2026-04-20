using System;

namespace MediMateRepository.Model
{
    public class DoctorContract
    {
        public Guid ContractId { get; set; }
        
        // Hợp đồng làm việc lưu dưới dạng ảnh hoặc file
        public string FileUrl { get; set; } = string.Empty;
        
        // Ngày tháng hiệu lực
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        public string Status { get; set; } = "Active"; // Active, Expired, Terminated
        public string? Note { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}
