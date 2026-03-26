using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class NotificationSettingResponse
    {
        public Guid SettingId { get; set; }
        public Guid FamilyId { get; set; }
        public bool EnablePushNotification { get; set; }
        public bool EnableEmailNotification { get; set; }
        public bool EnableSmsNotification { get; set; }
        public int ReminderAdvanceMinutes { get; set; }
        public bool EnableFamilyAlert { get; set; }
        public string CustomSetting { get; set; } = string.Empty;
        public DateTime UpdateAt { get; set; }
    }

    public class UpdateNotificationSettingRequest
    {
        // Dùng nullable để Client chỉ gửi những field cần update
        public bool? EnablePushNotification { get; set; }
        public bool? EnableEmailNotification { get; set; }
        public bool? EnableSmsNotification { get; set; }

        [Range(0, 1440, ErrorMessage = "Thời gian nhắc trước phải từ 0 đến 1440 phút.")]
        public int? ReminderAdvanceMinutes { get; set; }

        public bool? EnableFamilyAlert { get; set; }
        public string? CustomSetting { get; set; }
    }
}
