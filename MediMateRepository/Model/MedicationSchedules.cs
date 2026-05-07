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
        
        public string ScheduleName { get; set; } = string.Empty; // VD: "Buổi sáng", "Buổi trưa"
        public TimeSpan TimeOfDay { get; set; } // VD: 08:00:00

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Members Member { get; set; }

        // 1 Lịch cố định chứa nhiều loại thuốc
        public virtual ICollection<MedicationScheduleDetails> ScheduleDetails { get; set; } = new List<MedicationScheduleDetails>();
        public virtual ICollection<MedicationReminders> MedicationReminders { get; set; } = new List<MedicationReminders>();
    }
}
