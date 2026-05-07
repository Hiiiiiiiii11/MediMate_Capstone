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

        // Người thực hiện xác nhận (có thể là chủ hộ xác nhận cho thành viên)
        public Guid? TakenByUserId { get; set; }
    }

    public class LogMedicineDetail
    {
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
    }

    public class MedicationLogResponse
    {
        public Guid LogId { get; set; }
        public Guid MemberId { get; set; }
        public Guid ScheduleId { get; set; }
        public Guid ReminderId { get; set; }
        public string MedicineName { get; set; } = string.Empty;

        public List<LogMedicineDetail> Medicines { get; set; } = new();

        // Tên người uống thuốc (thành viên)
        public string MemberName { get; set; } = string.Empty;

        // Thông tin người đã xác nhận uống (có thể là người khác trong gia đình)
        public Guid? TakenByUserId { get; set; }
        public string? TakenByName { get; set; }

        public DateTime LogDate { get; set; }
        public DateTime ScheduledTime { get; set; }
        public DateTime ActualTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class FamilyAdherenceDashboard
    {
        public Guid FamilyId { get; set; }
        public int TotalScheduled { get; set; } // Tổng số lần cần uống
        public int TotalTaken { get; set; }      // Tổng số lần đã uống
        public int TotalSkipped { get; set; }    // Tổng số lần bỏ qua
        public int TotalMissed { get; set; }     // Tổng số lần quên (hệ thống tự đánh dấu)
        public double OverallAdherenceRate { get; set; } // Tỷ lệ % chung của cả nhà
        public List<MemberAdherenceStats> MemberStats { get; set; } = new();
    }

    public class MemberAdherenceStats
    {
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public int Taken { get; set; }
        public int Missed { get; set; }
        public int Skipped { get; set; }
        public double AdherenceRate { get; set; } // Tỷ lệ % của riêng người này
    }
}
