using MediMateRepository.Model;
using MediMateRepository.Repositories;

namespace MediMateService.Services.Implementations
{
    public class ReminderJobService : IReminderJobService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFirebaseNotificationService _firebaseService;

        public ReminderJobService(IUnitOfWork unitOfWork, IFirebaseNotificationService firebaseService)
        {
            _unitOfWork = unitOfWork;
            _firebaseService = firebaseService;
        }

        public async Task CheckAndNotifyOverdueReminder(Guid reminderId)
        {
            // 1. Kéo thông tin Reminder kèm theo Member, Setting, và Family
            var reminder = (await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.ReminderId == reminderId,
                includeProperties: "Schedule,Schedule.Member,Schedule.Member.NotificationSetting")).FirstOrDefault();

            if (reminder == null) return;

            if (reminder.Status == "Pending" && reminder.Schedule.IsActive)
            {
                var targetMember = reminder.Schedule.Member;
                if (targetMember == null) return;

                var settings = targetMember.NotificationSetting;

                // Nếu chưa có cài đặt (chưa ai vào màn hình Setting bao giờ), mặc định coi như được phép gửi (bật hết)
                bool canSendToMember = settings == null || settings.EnablePushNotification;
                bool canSendToFamily = settings == null || settings.EnableFamilyAlert;

                string title = "⚠️ Nhắc nhở uống thuốc!";
                string bodyMember = $"Đã đến giờ uống {reminder.Schedule.Dosage} {reminder.Schedule.MedicineName}. Hãy uống thuốc và xác nhận trên app nhé!";
                string bodyFamily = $"{targetMember.FullName} có lịch uống {reminder.Schedule.MedicineName} bây giờ. Hãy nhắc nhở nhé!";

                var data = new Dictionary<string, string> { { "reminderId", reminderId.ToString() } };

                // ==========================================
                // 2. GỬI CHO BẢN THÂN MEMBER ĐÓ (Nếu họ có FcmToken riêng và cho phép Push)
                // ==========================================
                if (canSendToMember && !string.IsNullOrEmpty(targetMember.FcmToken))
                {
                    await _firebaseService.SendNotificationAsync(targetMember.FcmToken, title, bodyMember, data);
                }

                // ==========================================
                // 3. GỬI CHO CHỦ HỘ GIA ĐÌNH (Nếu bật cảnh báo gia đình)
                // ==========================================
                if (canSendToFamily && targetMember.FamilyId.HasValue)
                {
                    // Lấy gia đình để tìm người tạo (Creator)
                    var family = await _unitOfWork.Repository<Families>().GetByIdAsync(targetMember.FamilyId.Value);
                    if (family != null)
                    {
                        // Tìm FcmToken của User chủ hộ
                        var creatorUser = await _unitOfWork.Repository<User>().GetByIdAsync(family.CreateBy);

                        // Đảm bảo không gửi trùng lặp nếu Member cần uống thuốc CHÍNH LÀ Chủ hộ
                        if (creatorUser != null && !string.IsNullOrEmpty(creatorUser.FcmToken) && creatorUser.FcmToken != targetMember.FcmToken)
                        {
                            await _firebaseService.SendNotificationAsync(creatorUser.FcmToken, title, bodyFamily, data);
                        }
                    }
                }

                // 4. Cập nhật trạng thái
                reminder.SentdAt = DateTime.Now;
                _unitOfWork.Repository<MedicationReminders>().Update(reminder);
                await _unitOfWork.CompleteAsync();
            }
        }
    }
}