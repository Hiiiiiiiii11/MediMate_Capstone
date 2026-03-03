using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class CreateScheduleRequest
    {
        public Guid? PrescriptionMedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string SpecificTimes { get; set; } = string.Empty; // VD: "08:00-09:00, 19:00-20:00"
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Instructions { get; set; } = string.Empty;
    }
    public class UpdateScheduleRequest
    {
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string SpecificTimes { get; set; } = string.Empty;
        public DateTime? EndDate { get; set; }
        public string Instructions { get; set; } = string.Empty;
    }
    public class MedicationActionRequest
    {
        public string Status { get; set; } = "Taken"; // "Taken" hoặc "Skipped"
        public string Notes { get; set; } = string.Empty;
        public DateTime ActualTime { get; set; } // Giờ bấm nút
    }

    public class ScheduleResponse
    {
        public Guid ScheduleId { get; set; }
        public string MedicineName { get; set; }
        public string SpecificTimes { get; set; }
        public bool IsActive { get; set; }
    }

    public class ReminderDailyResponse
    {
        public Guid ReminderId { get; set; }
        public Guid ScheduleId { get; set; }
        // Thêm 2 trường này để hiển thị trên màn hình Gia đình
        public Guid MemberId { get; set; }
        public string MemberName { get; set; }

        public string MedicineName { get; set; }
        public string Dosage { get; set; }
        public DateTime ReminderTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
    }
}
