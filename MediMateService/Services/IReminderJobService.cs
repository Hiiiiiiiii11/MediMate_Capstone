namespace MediMateService.Services
{
    public interface IReminderJobService
    {
        Task NotifyReminderTimeAsync(Guid reminderId);
        Task CheckAndNotifyOverdueReminder(Guid reminderId);
        Task CheckMissedReminderAndAlertFamilyAsync(Guid reminderId);
        Task NotifyUpcomingAppointmentAsync(Guid appointmentId);

        /// <summary>
        /// Tạo ConsultationSession cho lịch hẹn (chạy T-5 phút trước giờ hẹn).
        /// Session sẽ tự động tạo với Status = "Processing".
        /// </summary>
        Task CreateConsultationSessionAsync(Guid appointmentId);

        /// <summary>
        /// Force-end session nếu vẫn còn Processing/InProgress sau khi hết giờ (T+60 phút).
        /// Session tạo lúc T-5 → job này chạy sau 65 phút từ lúc tạo.
        /// </summary>
        Task AutoEndExpiredSessionAsync(Guid sessionId);
        Task AutoCancelUnapprovedAppointmentAsync(Guid appointmentId);
    }
}
