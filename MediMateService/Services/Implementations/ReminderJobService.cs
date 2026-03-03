using MediMateRepository.Model;
using MediMateRepository.Repositories;

namespace MediMateService.Services.Implementations
{
    public class ReminderJobService : IReminderJobService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFirebaseNotificationService _firebaseService; // 1. Inject Firebase Service

        public ReminderJobService(IUnitOfWork unitOfWork, IFirebaseNotificationService firebaseService)
        {
            _unitOfWork = unitOfWork;
            _firebaseService = firebaseService;
        }


        // HÀM NÀY CHẠY ĐÚNG VÀO LÚC "ENDTIME" CỦA TỪNG REMINDER
        public async Task CheckAndNotifyOverdueReminder(Guid reminderId)
        {
            var reminder = (await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.ReminderId == reminderId,
                includeProperties: "Schedule,Schedule.Member,Schedule.Member.User")).FirstOrDefault(); // Kéo luôn User lên để lấy Token

            if (reminder == null) return;

            if (reminder.Status == "Pending" && reminder.Schedule.IsActive)
            {
                // 2. Lấy FCM Token từ DB (Giả sử bạn đã lưu ở bảng User hoặc Member)
                // Ưu tiên gửi cho Dependent (nếu có máy riêng), nếu không thì gửi cho Parent (User)
                string fcmToken = reminder.Schedule.Member?.FcmToken;

                if (!string.IsNullOrEmpty(fcmToken))
                {
                    string title = "⚠️ Quá giờ uống thuốc!";
                    string body = $"{reminder.Schedule.Member.FullName} đã lỡ khung giờ uống thuốc {reminder.Schedule.MedicineName}. Hãy kiểm tra ngay!";

                    var data = new Dictionary<string, string> { { "reminderId", reminderId.ToString() } };

                    // 3. BẮN THÔNG BÁO
                    await _firebaseService.SendNotificationAsync(fcmToken, title, body, data);
                }

                reminder.SentdAt = DateTime.Now;
                _unitOfWork.Repository<MedicationReminders>().Update(reminder);
                await _unitOfWork.CompleteAsync();
            }
        }
    }
}