using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class MedicationReminders
    {
        public Guid ReminderId { get; set; }
        public Guid ScheduleId { get; set; }
        public DateTime ReminderDate { get; set; }
        public DateTime ReminderTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Taken, Skipped
        public DateTime ScheduledAt { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime AcknowledgedAt { get; set; }
        public virtual MedicationSchedules Schedule { get; set; }
    }
}
