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

        public int MinimumHoursGap { get; set; } = 2; // Số giờ tối thiểu giữa 2 liều cùng thuốc
        public int MaxDosesPerDay { get; set; } = 6;  // Số liều tối đa mỗi ngày cho 1 loại thuốc
        public int MissedDosesThreshold { get; set; } = 3; // Số lần bỏ thuốc liên tiếp để cảnh báo khẩn

        // Navigation Property trỏ về Families
        public virtual Families Family { get; set; }
    }
}
