using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class LogMedicationRequest
    {
        [Required]
        public Guid ReminderId { get; set; }

        [Required]
        [RegularExpression("^(Taken|Skipped|Missed)$", ErrorMessage = "Trạng thái chỉ nhận: Taken, Skipped, Missed")]
        public string Status { get; set; } = string.Empty;

        public DateTime? ActualTime { get; set; } // Nếu không gửi, mặc định lấy giờ Server

        public string? Notes { get; set; } // VD: "Uống thuốc hơi buồn nôn", "Quên mang thuốc đi làm"
    }

    public class MedicationLogResponse
    {
        public Guid LogId { get; set; }
        public Guid MemberId { get; set; }
        public Guid ScheduleId { get; set; }
        public Guid ReminderId { get; set; }
        public string MedicineName { get; set; } = string.Empty;

        // MỚI: Thêm trường này để hiển thị tên người uống trên UI
        public string MemberName { get; set; } = string.Empty;

        public DateTime LogDate { get; set; }
        public DateTime ScheduledTime { get; set; }
        public DateTime ActualTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
