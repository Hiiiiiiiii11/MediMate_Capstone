using System;
using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class SubscriptionUsageLogs
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid SubscriptionId { get; set; }
        public virtual FamilySubscriptions Subscription { get; set; } = null!;
        
        // Loại dịch vụ sử dụng: hiện tại là "OCR"
        public string UsageType { get; set; } = "OCR"; 
        
        // Số lượng bị trừ (ví dụ: 1 lượt quét)
        public int Amount { get; set; } = 1;
        
        // ReferenceId: Lưu PrescriptionId (nếu quét đơn thuốc)
        public Guid? ReferenceId { get; set; }
        
        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
