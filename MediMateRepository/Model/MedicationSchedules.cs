using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class MedicationSchedules
    {
        public Guid ScheduleId { get; set; }
        public Guid MemberId { get; set; }
        public Guid? PrescriptionId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty; // Ví dụ: "Sáng 1, Chiều 1"
        public string SpecificTimes { get; set; } = string.Empty; // Ví dụ: "08:00, 20:00"

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; } // null nếu không có ngày kết thúc
        public string Instructions { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsAiGenerated { get; set; } = false;
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;

        public virtual Prescriptions? Prescription { get; set; }
        public virtual Members Member { get; set; }
        public virtual ICollection<MedicationReminders> MedicationReminders { get; set; } = new List<MedicationReminders>();

    }
}
