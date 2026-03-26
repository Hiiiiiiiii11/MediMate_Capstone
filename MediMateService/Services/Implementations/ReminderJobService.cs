using MediMateRepository.Model;
using MediMateRepository.Repositories;

namespace MediMateService.Services.Implementations
{
    public class ReminderJobService : IReminderJobService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFirebaseNotificationService _firebaseService;
        private readonly INotificationService _notificationService;

        public ReminderJobService(IUnitOfWork unitOfWork, IFirebaseNotificationService firebaseService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _firebaseService = firebaseService;
            _notificationService = notificationService;
        }

        public async Task CheckAndNotifyOverdueReminder(Guid reminderId)
        {
            // 1. Kéo thông tin Reminder kèm theo Member (Bỏ NotificationSetting cũ)
            var reminder = (await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.ReminderId == reminderId,
                includeProperties: "Schedule,Schedule.Member")).FirstOrDefault();

            // Nếu lịch bị xóa, bị vô hiệu hóa hoặc không phải Pending thì bỏ qua
            if (reminder == null || reminder.Status != "Pending" || !reminder.Schedule.IsActive) return;

            var targetMember = reminder.Schedule.Member;
            if (targetMember == null || !targetMember.FamilyId.HasValue) return;

            // =========================================================
            // 2. KÉO CÀI ĐẶT THÔNG BÁO TỪ GIA ĐÌNH (Dùng FamilyId)
            // =========================================================
            var familySetting = (await _unitOfWork.Repository<NotificationSetting>()
                .FindAsync(s => s.FamilyId == targetMember.FamilyId.Value)).FirstOrDefault();

            // Nếu chưa có cài đặt, mặc định coi như được phép gửi (bật hết)
            bool canSendToMember = familySetting == null || familySetting.EnablePushNotification;
            bool canSendToFamily = familySetting == null || familySetting.EnableFamilyAlert;

            // Nếu gia đình tắt sạch thông báo thì hủy Job luôn
            if (!canSendToMember && !canSendToFamily) return;

            // =========================================================
            // 3. LẤY CHI TIẾT THUỐC CẦN UỐNG HÔM NAY
            // =========================================================
            var scheduleDetails = await _unitOfWork.Repository<MedicationScheduleDetails>()
                .FindAsync(d => d.ScheduleId == reminder.ScheduleId
                                 && d.StartDate.Date <= reminder.ReminderDate.Date
                                 && d.EndDate.Date >= reminder.ReminderDate.Date,
                       includeProperties: "PrescriptionMedicine");

            string medicineNames = scheduleDetails.Any()
                ? string.Join(", ", scheduleDetails.Select(d => d.PrescriptionMedicine?.MedicineName ?? "Thuốc"))
                : "Thuốc theo lịch";

            string title = "⚠️ Nhắc nhở uống thuốc!";
            string bodyMember = $"Đã đến giờ uống {medicineNames} (Lịch: {reminder.Schedule.ScheduleName}). Hãy uống thuốc và xác nhận trên app nhé!";
            string bodyFamily = $"{targetMember.FullName} có lịch uống {medicineNames} bây giờ. Hãy nhắc nhở nhé!";

            var data = new Dictionary<string, string> { { "reminderId", reminderId.ToString() } };

            // ==========================================
            // 4. GỬI CHO BẢN THÂN MEMBER ĐÓ (Nếu họ có FcmToken riêng và bật thông báo)
            // ==========================================
            if (canSendToMember && !string.IsNullOrEmpty(targetMember.FcmToken))
            {
                await _firebaseService.SendNotificationAsync(targetMember.FcmToken, title, bodyMember, data);
            }

            // ==========================================
            // 5. GỬI CHO CHỦ HỘ GIA ĐÌNH (Nếu bật cảnh báo gia đình)
            // ==========================================
            if (canSendToFamily)
            {
                var family = await _unitOfWork.Repository<Families>().GetByIdAsync(targetMember.FamilyId.Value);
                if (family != null)
                {
                    // Tìm FcmToken của User chủ hộ
                    var creatorUser = await _unitOfWork.Repository<User>().GetByIdAsync(family.CreateBy);

                    // Gửi nếu chủ hộ tồn tại VÀ Chủ hộ khác với người vừa nhận thông báo (Tránh gửi 2 lần cho 1 máy)
                    if (creatorUser != null && !string.IsNullOrEmpty(creatorUser.FcmToken) && creatorUser.FcmToken != targetMember.FcmToken)
                    {
                        await _firebaseService.SendNotificationAsync(creatorUser.FcmToken, title, bodyFamily, data);
                    }
                }
            }

            // 6. Cập nhật trạng thái
            // (Sửa lại thành SentAt nếu Database của bạn đã fix lỗi chính tả, hoặc giữ SentdAt nếu DB vẫn đang là SentdAt)
            reminder.SentAt = DateTime.Now;
            _unitOfWork.Repository<MedicationReminders>().Update(reminder);
            await _unitOfWork.CompleteAsync();
        }

        public async Task NotifyUpcomingAppointmentAsync(Guid appointmentId)
        {
            // 1. Lấy thông tin lịch khám từ DB
            var appointment = await _unitOfWork.Repository<Appointments>().GetByIdAsync(appointmentId);

            // NẾU LỊCH BỊ HỦY HOẶC CHƯA DUYỆT -> KHÔNG LÀM GÌ CẢ (Hủy ngầm Job)
            if (appointment == null || appointment.Status != "Approved") return;

            // 2. Kiểm tra lại giờ giấc (Phòng trường hợp lịch bị dời ngày, cái Job cũ vẫn chạy)
            var appointmentFullDateTime = appointment.AppointmentDate.Date.Add(appointment.AppointmentTime);
            var timeDifference = appointmentFullDateTime - DateTime.Now;

            // Nếu còn đúng khoảng 14 - 16 phút nữa là tới giờ khám thì mới gửi (tránh gửi sai do dời lịch)
            if (timeDifference.TotalMinutes < 0 || timeDifference.TotalMinutes > 20) return;

            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(appointment.MemberId);
            var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(appointment.DoctorId);

            if (member == null || doctor == null) return;

            string timeString = appointment.AppointmentTime.ToString(@"hh\:mm");

            // 3. Bắn thông báo
            if (member.UserId.HasValue)
            {
                await _notificationService.SendNotificationAsync(
                    userId: member.UserId.Value,
                    title: "⏰ Sắp đến giờ khám!",
                    message: $"Bạn có lịch khám online với Bác sĩ {doctor.FullName} vào lúc {timeString} (15 phút nữa). Vui lòng chuẩn bị!",
                    type: "UPCOMING_APPOINTMENT",
                    referenceId: appointment.AppointmentId
                );
            }

            await _notificationService.SendNotificationAsync(
                userId: doctor.UserId,
                title: "⏰ Sắp đến giờ làm việc!",
                message: $"Bạn có lịch khám online với bệnh nhân {member.FullName} vào lúc {timeString} (15 phút nữa).",
                type: "UPCOMING_APPOINTMENT",
                referenceId: appointment.AppointmentId
            );
        }
    }
}