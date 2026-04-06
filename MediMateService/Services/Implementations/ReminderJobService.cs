using Hangfire;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using Share.Constants;

namespace MediMateService.Services.Implementations
{
    public class ReminderJobService : IReminderJobService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFirebaseNotificationService _firebaseService;
        private readonly INotificationService _notificationService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public ReminderJobService(
            IUnitOfWork unitOfWork,
            IFirebaseNotificationService firebaseService,
            INotificationService notificationService,
            IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork;
            _firebaseService = firebaseService;
            _notificationService = notificationService;
            _backgroundJobClient = backgroundJobClient;
        }

        // ─────────────────────────────────────────────────────────────────
        // CHECK & NOTIFY: TỚI GIỜ UỐNG THUỐC
        // ─────────────────────────────────────────────────────────────────
        public async Task NotifyReminderTimeAsync(Guid reminderId)
        {
            var reminder = (await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.ReminderId == reminderId,
                includeProperties: "Schedule,Schedule.Member")).FirstOrDefault();

            if (reminder == null || reminder.Status != "Pending" || !reminder.Schedule.IsActive) return;

            var targetMember = reminder.Schedule.Member;
            if (targetMember == null || !targetMember.FamilyId.HasValue) return;

            var familySetting = (await _unitOfWork.Repository<NotificationSetting>()
                .FindAsync(s => s.FamilyId == targetMember.FamilyId.Value)).FirstOrDefault();

            bool canSendToMember = familySetting == null || familySetting.EnablePushNotification;
            bool canSendToFamily = familySetting == null || familySetting.EnableFamilyAlert;

            if (!canSendToMember && !canSendToFamily) return;

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

            if (canSendToMember && !string.IsNullOrEmpty(targetMember.FcmToken))
            {
                await _firebaseService.SendNotificationAsync(targetMember.FcmToken, title, bodyMember, data);
            }

            if (canSendToFamily)
            {
                var family = await _unitOfWork.Repository<Families>().GetByIdAsync(targetMember.FamilyId.Value);
                if (family != null)
                {
                    var creatorUser = await _unitOfWork.Repository<User>().GetByIdAsync(family.CreateBy);
                    if (creatorUser != null && !string.IsNullOrEmpty(creatorUser.FcmToken) && creatorUser.FcmToken != targetMember.FcmToken)
                    {
                        await _firebaseService.SendNotificationAsync(creatorUser.FcmToken, title, bodyFamily, data);
                    }
                }
            }

            reminder.SentAt = DateTime.Now;
            _unitOfWork.Repository<MedicationReminders>().Update(reminder);
            await _unitOfWork.CompleteAsync();
        }

        public async Task CheckAndNotifyOverdueReminder(Guid reminderId)
        {
            // Backward compatibility
            await NotifyReminderTimeAsync(reminderId);
        }

        // ─────────────────────────────────────────────────────────────────
        // CHECK QUÁ HÀN VÀ CẢNH BÁO LIÊN TỤC BỎ THUỐC ĐẾN NGƯỜI NHÀ
        // ─────────────────────────────────────────────────────────────────
        public async Task CheckMissedReminderAndAlertFamilyAsync(Guid reminderId)
        {
            var reminder = (await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.ReminderId == reminderId,
                includeProperties: "Schedule,Schedule.Member")).FirstOrDefault();

            if (reminder == null || reminder.Status != "Pending" || !reminder.Schedule.IsActive) return;

            var targetMember = reminder.Schedule.Member;
            if (targetMember == null || !targetMember.FamilyId.HasValue) return;

            // Đổi trạng thái sang Missed do hết giờ (đã tới EndTime) mà vẫn Pending
            reminder.Status = "Missed";
            _unitOfWork.Repository<MedicationReminders>().Update(reminder);

            var log = new MedicationLogs
            {
                LogId = Guid.NewGuid(),
                MemberId = reminder.Schedule.MemberId,
                ScheduleId = reminder.ScheduleId,
                ReminderId = reminder.ReminderId,
                LogDate = DateTime.Now.Date,
                ScheduledTime = reminder.ReminderTime,
                ActualTime = DateTime.Now,
                Status = "Missed",
                Notes = "Tự động đánh dấu Missed do hết thời gian uống.",
                CreatedAt = DateTime.Now
            };
            await _unitOfWork.Repository<MedicationLogs>().AddAsync(log);
            await _unitOfWork.CompleteAsync();

            // Kiểm tra số lần bỏ thuốc liên tiếp
            var familySetting = (await _unitOfWork.Repository<NotificationSetting>()
                .FindAsync(s => s.FamilyId == targetMember.FamilyId.Value)).FirstOrDefault();

            int threshold = familySetting?.MissedDosesThreshold ?? 3;

            var recentLogs = await _unitOfWork.Repository<MedicationLogs>()
                .FindAsync(l => l.MemberId == targetMember.MemberId);

            var recentSortedLogs = recentLogs.OrderByDescending(l => l.LogDate).ThenByDescending(l => l.ScheduledTime).Take(threshold).ToList();

            if (recentSortedLogs.Count == threshold && recentSortedLogs.All(l => l.Status == "Missed" || l.Status == "Skipped"))
            {
                // Cảnh báo khẩn cấp vì đã bỏ liên tiếp
                var family = await _unitOfWork.Repository<Families>().GetByIdAsync(targetMember.FamilyId.Value);
                if (family != null)
                {
                    var creatorUser = await _unitOfWork.Repository<User>().GetByIdAsync(family.CreateBy);
                    if (creatorUser != null && !string.IsNullOrEmpty(creatorUser.FcmToken))
                    {
                        string urgentTitle = "🚨 CẢNH BÁO KHẨN CẤP: BỎ THUỐC";
                        string urgentBody = $"Bệnh nhân {targetMember.FullName} đã bỏ thuốc {threshold} lần liên tiếp! Hãy liên lạc và kiểm tra tình hình lập tức.";
                        var data = new Dictionary<string, string> { { "alertType", "Urgent" } };

                        await _firebaseService.SendNotificationAsync(creatorUser.FcmToken, urgentTitle, urgentBody, data);
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // NOTIFY: SẮP ĐẾN GIỜ KHÁM (T-15 PHÚT)
        // ─────────────────────────────────────────────────────────────────
        public async Task NotifyUpcomingAppointmentAsync(Guid appointmentId)
        {
            var appointment = await _unitOfWork.Repository<Appointments>().GetByIdAsync(appointmentId);

            if (appointment == null || appointment.Status != AppointmentConstants.APPROVED) return;

            var appointmentFullDateTime = appointment.AppointmentDate.Date.Add(appointment.AppointmentTime);
            var timeDifference = appointmentFullDateTime - DateTime.Now;

            if (timeDifference.TotalMinutes < 0 || timeDifference.TotalMinutes > 20) return;

            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(appointment.MemberId);
            var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(appointment.DoctorId);
            if (member == null || doctor == null) return;

            string timeString = appointment.AppointmentTime.ToString(@"hh\:mm");

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

        // ─────────────────────────────────────────────────────────────────
        // JOB: TẠO SESSION T-5 PHÚT TRƯỚC GIỜ HẸN
        //
        // Timeline:
        //   T-15 phút → NotifyUpcomingAppointmentAsync (thông báo sắp đến giờ)
        //   T-5  phút → CreateConsultationSessionAsync  ← JOB NÀY
        //   T+0 phút  → Giờ hẹn chính thức (StartedAt)
        //   T+60 phút → AutoEndExpiredSessionAsync (session timeout)
        //
        // Session tồn tại tổng 65 phút (từ T-5 đến T+60).
        // ─────────────────────────────────────────────────────────────────
        public async Task CreateConsultationSessionAsync(Guid appointmentId)
        {
            var appointment = await _unitOfWork.Repository<Appointments>().GetByIdAsync(appointmentId);

            // Chỉ tạo nếu appointment đang Approved
            if (appointment == null || appointment.Status != AppointmentConstants.APPROVED) return;

            // Tránh tạo trùng session
            var existing = (await _unitOfWork.Repository<ConsultationSessions>()
                .FindAsync(s => s.AppointmentId == appointmentId)).FirstOrDefault();
            if (existing != null) return;

            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(appointment.MemberId);
            var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(appointment.DoctorId);
            if (member == null || doctor == null) return;

            var session = new ConsultationSessions
            {
                ConsultanSessionId = Guid.NewGuid(),
                AppointmentId = appointment.AppointmentId,
                DoctorId = appointment.DoctorId,
                MemberId = appointment.MemberId,
                // StartedAt = đúng giờ hẹn (T+0), không phải lúc job chạy (T-5)
                StartedAt = appointment.AppointmentDate.Date.Add(appointment.AppointmentTime),
                EndedAt = null,
                Status = ConsultationSessionConstants.PROCESSING,
                UserJoined = false,
                DoctorJoined = false,
                Note = null,
                DoctorNote = null
            };

            await _unitOfWork.Repository<ConsultationSessions>().AddAsync(session);
            await _unitOfWork.CompleteAsync();

            // Thông báo: "Phòng khám đã mở, tham gia ngay"
            string timeString = appointment.AppointmentTime.ToString(@"hh\:mm");

            if (member.UserId.HasValue)
            {
                await _notificationService.SendNotificationAsync(
                    userId: member.UserId.Value,
                    title: "🔔 Phòng khám đã mở!",
                    message: $"Phiên tư vấn với Bác sĩ {doctor.FullName} lúc {timeString} đã sẵn sàng. Tham gia ngay!",
                    type: ConsultationSessionActionTypes.SESSION_STARTED,
                    referenceId: session.ConsultanSessionId
                );
            }

            await _notificationService.SendNotificationAsync(
                userId: doctor.UserId,
                title: "🔔 Phòng khám đã mở!",
                message: $"Phiên tư vấn với bệnh nhân {member.FullName} lúc {timeString} đã sẵn sàng. Tham gia ngay!",
                type: ConsultationSessionActionTypes.SESSION_STARTED,
                referenceId: session.ConsultanSessionId
            );

            // Schedule AutoEnd sau 65 phút kể từ bây giờ (= T+60 so với giờ hẹn)
            _backgroundJobClient.Schedule<IReminderJobService>(
                job => job.AutoEndExpiredSessionAsync(session.ConsultanSessionId),
                TimeSpan.FromMinutes(65)
            );
        }

        // ─────────────────────────────────────────────────────────────────
        // JOB: TỰ ĐỘNG KẾT THÚC SESSION SAU 65 PHÚT (T+60 PHÚT SO VỚI GIỜ HẸN)
        //
        // Sau khi ENDED:
        //   - User KHÔNG thể gửi tin nhắn mới.
        //   - Doctor VẪN có thể gửi tin nhắn (kiểm tra bên ChatDoctorService).
        // ─────────────────────────────────────────────────────────────────
        public async Task AutoEndExpiredSessionAsync(Guid sessionId)
        {
            var session = await _unitOfWork.Repository<ConsultationSessions>().GetByIdAsync(sessionId);

            // Nếu đã kết thúc thủ công → bỏ qua
            if (session == null || session.Status == ConsultationSessionConstants.ENDED) return;

            session.Status = ConsultationSessionConstants.ENDED;
            session.EndedAt = DateTime.Now;
            session.Note = string.IsNullOrWhiteSpace(session.Note)
                ? "Phiên tư vấn tự động kết thúc do hết thời gian"
                : $"{session.Note}; Phiên tự động kết thúc do hết thời gian";

            _unitOfWork.Repository<ConsultationSessions>().Update(session);

            // Appointment → Completed (nếu chưa bị hủy)
            var appointment = await _unitOfWork.Repository<Appointments>().GetByIdAsync(session.AppointmentId);
            if (appointment != null && appointment.Status != AppointmentConstants.CANCELLED)
            {
                appointment.Status = AppointmentConstants.COMPLETED;
                _unitOfWork.Repository<Appointments>().Update(appointment);
            }

            await _unitOfWork.CompleteAsync();

            // Thông báo kết thúc
            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(session.MemberId);
            var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(session.DoctorId);

            if (member?.UserId.HasValue == true)
            {
                await _notificationService.SendNotificationAsync(
                    userId: member.UserId.Value,
                    title: "⏱️ Phiên tư vấn đã kết thúc",
                    message: "Phiên tư vấn đã hết thời gian cho phép. Bác sĩ vẫn có thể nhắn tin cho bạn nếu cần.",
                    type: ConsultationSessionActionTypes.SESSION_TIMEOUT,
                    referenceId: session.ConsultanSessionId
                );
            }

            if (doctor != null)
            {
                await _notificationService.SendNotificationAsync(
                    userId: doctor.UserId,
                    title: "⏱️ Phiên tư vấn đã kết thúc",
                    message: $"Phiên tư vấn với bệnh nhân {member?.FullName} đã hết thời gian. Bạn vẫn có thể gửi tin nhắn cho bệnh nhân.",
                    type: ConsultationSessionActionTypes.SESSION_TIMEOUT,
                    referenceId: session.ConsultanSessionId
                );
            }
        }

        public async Task AutoCancelUnapprovedAppointmentAsync(Guid appointmentId)
        {
            // 1. Tìm lịch hẹn (Sử dụng UnitOfWork hoặc Repository bạn đang inject trong class này)
            var appointment = await _unitOfWork.Repository<Appointments>().GetByIdAsync(appointmentId);

            if (appointment == null) return;

            // 2. Nếu lịch hẹn KHÔNG còn ở trạng thái PENDING nữa thì bỏ qua (Bác sĩ đã duyệt hoặc tự hủy rồi)
            if (!appointment.Status.Equals(AppointmentConstants.PENDING, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // 3. Tiến hành tự động hủy
            appointment.Status = AppointmentConstants.CANCELLED;
            appointment.CancelReason = "Hệ thống tự động hủy do bác sĩ không phản hồi kịp thời.";
            _unitOfWork.Repository<Appointments>().Update(appointment);

            // 4. Hoàn trả lại lượt khám cho gia đình
            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(appointment.MemberId);
            if (member?.FamilyId != null)
            {
                var currentDate = DateOnly.FromDateTime(DateTime.Now);
                var activeSubscription = (await _unitOfWork.Repository<FamilySubscriptions>()
                    .FindAsync(s => s.FamilyId == member.FamilyId
                                     && s.Status == "Active"
                                     && s.EndDate >= currentDate)).FirstOrDefault();

                if (activeSubscription != null)
                {
                    activeSubscription.RemainingConsultantCount += 1;
                    _unitOfWork.Repository<FamilySubscriptions>().Update(activeSubscription);
                }
            }

            await _unitOfWork.CompleteAsync();

            // 5. Gửi thông báo cho Bệnh nhân
            if (member != null)
            {
                await _notificationService.SendNotificationAsync(
                    userId: member.UserId ?? Guid.Empty,
                    title: "⏳ Lịch khám đã bị hủy tự động",
                    message: $"Bác sĩ hiện không có mặt để phản hồi lịch khám lúc {appointment.AppointmentTime:hh\\:mm}. Lượt khám đã được hoàn trả, bạn vui lòng đặt bác sĩ khác nhé.",
                    type: AppointmentActionTypes.APPOINTMENT_CANCELLED,
                    referenceId: appointment.AppointmentId
                );
            }

            // 6. Gửi thông báo cho Bác sĩ (Để bác sĩ biết là đã mất khách)
            var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(appointment.DoctorId);
            if (doctor != null)
            {
                await _notificationService.SendNotificationAsync(
                    userId: doctor.UserId,
                    title: "⚠️ Lịch khám đã bị hủy tự động",
                    message: $"Bạn đã bỏ lỡ lịch đặt khám của bệnh nhân {member?.FullName ?? "Unknown"} vì không duyệt kịp thời gian.",
                    type: AppointmentActionTypes.APPOINTMENT_CANCELLED,
                    referenceId: appointment.AppointmentId
                );
            }
        }
    }
}