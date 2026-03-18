using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class Notifications
    {
        [Key]
        public Guid NotificationId { get; set; } = Guid.NewGuid();

        // ID của người nhận thông báo (Bệnh nhân hoặc Bác sĩ)
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        // Phân loại thông báo để Frontend biết đường xử lý. 
        // VD: "NEW_APPOINTMENT", "APPOINTMENT_APPROVED", "APPOINTMENT_REJECTED"
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        // Rất quan trọng: Lưu ID của cái lịch hẹn/phòng chat vào đây. 
        // Để khi User bấm vào thông báo, App biết mở cái lịch/phòng chat nào lên.
        public Guid? ReferenceId { get; set; }

        public bool IsRead { get; set; } = false; // Trạng thái đã đọc chưa (để tắt chấm đỏ)

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual User User { get; set; } = null!;
    }
}
