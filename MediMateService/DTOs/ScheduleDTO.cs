using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class CreateScheduleRequest
    {
        public string ScheduleName { get; set; } = string.Empty; // Sáng, Trưa, Tối
        public TimeSpan TimeOfDay { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
    public class UpdateScheduleRequest
    {
        public string ScheduleName { get; set; } = string.Empty;
        public TimeSpan TimeOfDay { get; set; }
        public DateTime? EndDate { get; set; }
    }
    public class UpdateScheduleDetailRequest
    {
        public string? Dosage { get; set; } // Null if user doesn't want to update
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
    public class MedicationActionRequest
    {
        public string Status { get; set; } = "Taken"; // "Taken" hoặc "Skipped"
        public string Notes { get; set; } = string.Empty;
        public DateTime ActualTime { get; set; } // Giờ bấm nút
    }

    public class ScheduleDetailItemResponse 
    {
        public Guid DetailId { get; set; }
        public Guid PrescriptionMedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ScheduleResponse
    {
        public Guid ScheduleId { get; set; }
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string ScheduleName { get; set; } = string.Empty;
        public TimeSpan TimeOfDay { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<ScheduleDetailItemResponse> ScheduleDetails { get; set; } = new List<ScheduleDetailItemResponse>();
    }

    public class ReminderDailyResponse
    {
        public Guid ReminderId { get; set; }
        public Guid ScheduleId { get; set; }
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string ScheduleName { get; set; } = string.Empty; 
        
        public List<ScheduleDetailItemResponse> Medicines { get; set; } = new List<ScheduleDetailItemResponse>();

        public DateTime ReminderTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ScheduleDetailResponse : ScheduleResponse
    {
        // Removed Prescription since a schedule may span multiple prescriptions
        // We only return the list of Medicines directly below
    }

    public class PrescriptionInfoResponse
    {
        public Guid PrescriptionId { get; set; }
        public string? PrescriptionCode { get; set; }
        public string? HospitalName { get; set; }
        public string? DoctorName { get; set; }
        public DateTime? PrescriptionDate { get; set; }

        public List<PrescriptionMedicineResponse> Medicines { get; set; } = new List<PrescriptionMedicineResponse>();
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
