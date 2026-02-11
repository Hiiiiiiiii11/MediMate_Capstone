using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class MedicationLogs
    {
        public Guid LogId { get; set; }
        public Guid MemberId { get; set; }
        public Guid ScheduleId { get; set; }
        public Guid ReminderId { get; set; }
        public DateTime LogDate { get; set; }
        public DateTime ScheduledTime { get; set; }
        public DateTime ActualTime { get; set; }
        public string Status { get; set; } = string.Empty; // Taken, Missed, Skipped
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public virtual Members Member { get; set; }
        public virtual MedicationSchedules Schedule { get; set; }
        public virtual MedicationReminders Reminder { get; set; }

    }
}
