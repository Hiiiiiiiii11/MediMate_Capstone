using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class NotificationSetting
    {
        public Guid SettingId { get; set; }

        // CHUYỂN TỪ MemberId SANG FamilyId
        public Guid FamilyId { get; set; }

        public bool EnablePushNotification { get; set; }
        public bool EnableEmailNotification { get; set; }
        public bool EnableSmsNotification { get; set; }
        public int ReminderAdvanceMinutes { get; set; }
        public bool EnableFamilyAlert { get; set; }
        public string? CustomSetting { get; set; }
        public DateTime UpdateAt { get; set; }

        // Navigation Property trỏ về Families
        public virtual Families Family { get; set; }
    }
}
