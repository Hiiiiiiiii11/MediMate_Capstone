using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class CreateScheduleRequest
    {
        public Guid? PrescriptionId { get; set; }
        public string Dosage { get; set; } = string.Empty;
        public string MedicineName { get; set; } = string.Empty;
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
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public Guid? PrescriptionId { get; set; } // Liên kết với đơn thuốc
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty; // Bổ sung
        public string SpecificTimes { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty; // Bổ sung để hiển thị cách dùng
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateAt { get; set; } // Bổ sung
    }

    public class ReminderDailyResponse
    {
        public Guid ReminderId { get; set; }
        public Guid ScheduleId { get; set; }
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty; // Bổ sung để người dùng biết cách uống ngay tại màn nhắc nhở
        public DateTime ReminderTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ScheduleDetailResponse : ScheduleResponse
    {
        public PrescriptionInfoResponse? Prescription { get; set; }
    }

    public class PrescriptionInfoResponse
    {
        public Guid PrescriptionId { get; set; }
        public string? PrescriptionCode { get; set; }
        public string? HospitalName { get; set; }
        public string? DoctorName { get; set; }
        public DateTime? PrescriptionDate { get; set; }

        // ĐƯA DANH SÁCH THUỐC VÀO ĐÂY: 
        // Vì danh sách thuốc là CỦA ĐƠN THUỐC, nên để trong object này sẽ hợp lý về mặt dữ liệu hơn.
        public List<PrescriptionMedicineDto> Medicines { get; set; } = new List<PrescriptionMedicineDto>();
    }

    public class CreateBulkScheduleRequest
    {
        public Guid? PrescriptionId { get; set; } // ID Đơn thuốc (Nếu có)
        public List<ScheduleItemRequest> Schedules { get; set; } = new List<ScheduleItemRequest>();
    }

    public class ScheduleItemRequest
    {
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string SpecificTimes { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Instructions { get; set; } = string.Empty;
    }

}
