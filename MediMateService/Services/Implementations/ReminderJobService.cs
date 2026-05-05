using Hangfire;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Share.Constants;

namespace MediMateService.Services.Implementations
{
    public class ReminderJobService : IReminderJobService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFirebaseNotificationService _firebaseService;
        private readonly INotificationService _notificationService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IHubContext<MediMateHub> _hubContext;

        public ReminderJobService(
            IUnitOfWork unitOfWork,
            IFirebaseNotificationService firebaseService,
            INotificationService notificationService,
            IBackgroundJobClient backgroundJobClient,
            IHubContext<MediMateHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _firebaseService = firebaseService;
            _notificationService = notificationService;
            _backgroundJobClient = backgroundJobClient;
            _hubContext = hubContext;
        }

        // ─────────────────────────────────────────────────────────────────
        // SWEEP: COMPENSATE MISSED REMINDERS & AUTO-SNOOZE
        // ─────────────────────────────────────────────────────────────────
        public async Task CompensateMissedRemindersAsync()
        {
            var now = DateTime.Now;

            // Tìm tất cả các Lời nhắc đang chờ, thuộc về Lịch đang Active
            var pendingReminders = await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.Status == "Pending" && r.Schedule.IsActive, 
                includeProperties: "Schedule,Schedule.Member");

            foreach (var reminder in pendingReminders)
            {
                var targetMember = reminder.Schedule.Member;
                if (targetMember == null || !targetMember.FamilyId.HasValue) continue;

                var familySetting = (await _unitOfWork.Repository<NotificationSetting>()
                    .FindAsync(s => s.FamilyId == targetMember.FamilyId.Value)).FirstOrDefault();

                int advanceMinutes = familySetting?.ReminderAdvanceMinutes ?? 15;
                var pushTime = reminder.ReminderTime.AddMinutes(-advanceMinutes);

                // Chưa đến giờ gửi thông báo
                if (now < pushTime) continue;

                // Nếu đã tới/vượt quá EndTime -> Nhường chỗ cho CheckMissedReminderAndAlertFamilyAsync xử lý trễ hạn
                if (now >= reminder.EndTime) continue;

                bool isSent = reminder.SentAt != default(DateTime);

                // 1. Trường hợp: Chưa từng gửi gì cả (Bị rớt do server restart ngay lúc giờ vàng)
                if (!isSent)
                {
                    await NotifyReminderTimeAsync(reminder.ReminderId, 5);
                    continue;
                }

                // 2. Trường hợp: Đã gửi rồi, kiểm tra xem Auto-snooze có đang bật không
                bool isAutoSnooze = true;
                try {
                    if (!string.IsNullOrEmpty(familySetting?.CustomSetting)) {
                        var customObj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonObject>(familySetting.CustomSetting);
                        if (customObj != null && customObj.ContainsKey("autoSnooze")) {
                            isAutoSnooze = customObj["autoSnooze"]?.GetValue<bool>() ?? true;
                        }
                    }
                } catch (Exception) { }

                if (!isAutoSnooze) continue;

                // Kiểm tra xem đã trôi qua 15 phút kể từ lần nhắc gần nhất chưa (Chu kỳ Snooze)
                if ((now - reminder.SentAt).TotalMinutes >= 15)
                {
                    await NotifyReminderTimeAsync(reminder.ReminderId, 2); // attempt 2: báo lặp lại
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // CHECK & NOTIFY: TỚI GIỜ UỐNG THUỐC
        // ─────────────────────────────────────────────────────────────────
        public async Task NotifyReminderTimeAsync(Guid reminderId, int attempt = 1)
        {
            var reminder = (await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.ReminderId == reminderId,
                includeProperties: "Schedule,Schedule.Member")).FirstOrDefault();

            if (reminder == null || reminder.Status != "Pending" || !reminder.Schedule.IsActive) return;

            var targetMember = reminder.Schedule.Member;
            if (targetMember == null || !targetMember.FamilyId.HasValue) return;

            var familySetting = (await _unitOfWork.Repository<NotificationSetting>()
                .FindAsync(s => s.FamilyId == targetMember.FamilyId.Value)).FirstOrDefault();

            int advanceMinutes = familySetting?.ReminderAdvanceMinutes ?? 15;
            var pushTime = reminder.ReminderTime.AddMinutes(-advanceMinutes);

            // BẢO VỆ: Nếu người dùng vừa ĐỔI GIỜ uống thuốc trên App (UpdateSchedule), Job cũ vẫn tồn tại và sẽ nổ sai giờ.
            // Nếu thời điểm hiện tại đang SỚM HƠN pushTime thực tế quá 5 phút -> Đây là Job rác (mồ côi) -> Tự hủy!
            if (DateTime.Now < pushTime.AddMinutes(-5)) return;

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

            // LOGIC TEXT DỰA TRÊN ATTEMPT VÀ THỜI GIAN
            string title;
            string bodyMember;
            string bodyFamily;

            var nextPushTime = DateTime.Now.AddMinutes(15);
            // Chỉ cảnh báo đỏ khi hiện tại cách EndTime <= 15 phút (không giới hạn số lần nhắc attempt)
            bool isLastWarning = DateTime.Now >= reminder.EndTime.AddMinutes(-15);

            if (attempt == 1 && DateTime.Now < reminder.ReminderTime.AddMinutes(-2))
            {
                title = "⏰ Sắp đến giờ uống thuốc!";
                bodyMember = $"Sắp tới bạn có lịch uống {medicineNames} lúc {reminder.ReminderTime:HH\\:mm}. Hãy chuẩn bị nhé!";
                bodyFamily = $"{targetMember.FullName} sắp có lịch uống {medicineNames} lúc {reminder.ReminderTime:HH\\:mm}.";
            }
            else if (isLastWarning)
            {
                title = "🚨 CẢNH BÁO TRỄ GIỜ UỐNG THUỐC!";
                bodyMember = $"Bạn sắp trễ giờ uống {medicineNames} (lịch {reminder.ReminderTime:HH\\:mm}). Nhắc nhở lần cuối, hãy uống uống thuốc để đảm bảo sức khỏe!";
                bodyFamily = $"CẢNH BÁO: {targetMember.FullName} chưa uống {medicineNames} lúc {reminder.ReminderTime:HH\\:mm}! Nhắc nhở lần cuối!";
            }
            else
            {
                title = "⚠️ Bạn có thuốc cần uống!";
                bodyMember = $"Đã đến giờ uống {medicineNames}. Hãy uống thuốc và xác nhận trên app nhé!";
                bodyFamily = $"{targetMember.FullName} có lịch uống {medicineNames} bây giờ. Hãy nhắc nhở nhé!";
            }
            
            // Lấy AutoSnooze từ cấu hình (MẶC ĐỊNH TRUE THEO YÊU CẦU)
            bool isAutoSnooze = true;
            try {
                if (!string.IsNullOrEmpty(familySetting?.CustomSetting)) {
                    var customObj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonObject>(familySetting.CustomSetting);
                    if (customObj != null && customObj.ContainsKey("autoSnooze")) {
                        isAutoSnooze = customObj["autoSnooze"]?.GetValue<bool>() ?? true;
                    }
                }
            } catch (Exception) { }

            var medicinesList = scheduleDetails.Select(d => new
            {
                medicineName = d.PrescriptionMedicine?.MedicineName ?? "Thuốc",
                dosage = d.Dosage,
                instructions = d.PrescriptionMedicine?.Instructions
            }).ToList();
            string medicinesJson = System.Text.Json.JsonSerializer.Serialize(medicinesList);

            var data = new Dictionary<string, string> 
            { 
                { "type", "MEDICATION_REMINDER" },
                { "reminderId", reminderId.ToString() },
                { "scheduleName", reminder.Schedule.ScheduleName },
                { "memberName", targetMember.FullName },
                { "reminderTime", reminder.ReminderTime.ToString("yyyy-MM-ddTHH:mm:ss") },
                { "endTime", reminder.EndTime.ToString("yyyy-MM-ddTHH:mm:ss") },
                { "autoSnooze", isAutoSnooze.ToString().ToLower() },
                { "medicines", medicinesJson }
            };

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

            // Lặp lại chu kỳ báo thức (Auto-Snooze) 15 phút một lần cho đến khi đạt EndTime (Hoặc quá số lần)
            if (isAutoSnooze && !isLastWarning)
            {
                if (nextPushTime < reminder.EndTime)
                {
                    _backgroundJobClient.Schedule<IReminderJobService>(
                        job => job.NotifyReminderTimeAsync(reminder.ReminderId, attempt + 1),
                        new DateTimeOffset(nextPushTime)
                    );
                }
            }
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

            // PHÒNG BỆNH: Nếu user Snooze, EndTime trong DB đã bị dài ra. 
            // Job cũ (theo EndTime cũ) có thể chạy sớm hơn EndTime mới. Nếu còn sớm, bỏ qua Job này!
            if (DateTime.Now < reminder.EndTime) return;

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
                        string urgentBody = targetMember.UserId == family.CreateBy
                            ? $"Bạn đã bỏ thuốc {threshold} lần liên tiếp! Việc dùng thuốc không đều đặn sẽ ảnh hưởng xấu đến kết quả điều trị. Hãy chú ý nhé!"
                            : $"Bệnh nhân {targetMember.FullName} đã bỏ thuốc {threshold} lần liên tiếp! Hãy liên lạc và kiểm tra tình hình lập tức.";
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

            // [QUAN TRỌNG]: Tìm chủ hộ (Family Head) để gửi Notification
            Guid? headUserId = member.UserId;
            if (!headUserId.HasValue && member.FamilyId != null)
            {
                var familyManager = await _unitOfWork.Repository<Members>().GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.FamilyId == member.FamilyId && m.UserId != null);
                headUserId = familyManager?.UserId;
            }

            if (headUserId.HasValue)
            {
                await _notificationService.SendNotificationAsync(
                    userId: headUserId.Value,
                    title: "⏰ Sắp đến giờ khám!",
                    message: $"Bạn có lịch khám online với Bác sĩ {doctor.FullName} vào lúc {timeString} (15 phút nữa). Vui lòng chuẩn bị!",
                    type: "UPCOMING_APPOINTMENT",
                    referenceId: appointment.AppointmentId
                );
            }

            // Bắn trực tiếp cho thiết bị của chính bệnh nhân
            await _notificationService.SendNotificationAsync(
                userId: null,
                title: "⏰ Sắp đến giờ khám!",
                message: $"Bạn có lịch khám online với Bác sĩ {doctor.FullName} vào lúc {timeString} (15 phút nữa). Vui lòng chuẩn bị!",
                type: "UPCOMING_APPOINTMENT",
                referenceId: appointment.AppointmentId,
                memberId: member.MemberId
            );

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

            string timeString = appointment.AppointmentTime.ToString(@"hh\:mm");

            // Thông báo cho Member (nếu có userId) hoặc Guardian
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

            // Gửi trực tiếp cho thiết bị của Bệnh nhân (đề phòng app bệnh nhân ở máy khác)
            await _notificationService.SendNotificationAsync(
                userId: null,
                title: "🔔 Phòng khám đã mở!",
                message: $"Phiên tư vấn với Bác sĩ {doctor.FullName} lúc {timeString} đã sẵn sàng. Tham gia ngay!",
                type: ConsultationSessionActionTypes.SESSION_STARTED,
                referenceId: session.ConsultanSessionId,
                memberId: member.MemberId
            );

            // Member bình thường → thông báo bác sĩ như cũ
            await _notificationService.SendNotificationAsync(
                userId: doctor.UserId,
                title: "🔔 Phòng khám đã mở!",
                message: $"Phiên tư vấn với bệnh nhân {member.FullName} lúc {timeString} đã sẵn sàng. Tham gia ngay!",
                type: ConsultationSessionActionTypes.SESSION_STARTED,
                referenceId: session.ConsultanSessionId
            );

            _backgroundJobClient.Schedule<IReminderJobService>(
                job => job.AutoEndExpiredSessionAsync(session.ConsultanSessionId),
                TimeSpan.FromMinutes(65)
            );
        }

        // ─────────────────────────────────────────────────────────────────
        // JOB: TỰ ĐỘNG KẾT THÚC SESSION SAU 65 PHÚT (T+60 PHÚT SO VỚI GIỜ HẸN)
        // ─────────────────────────────────────────────────────────────────
        public async Task AutoEndExpiredSessionAsync(Guid sessionId)
        {
            var session = await _unitOfWork.Repository<ConsultationSessions>().GetByIdAsync(sessionId);

            // Nếu đã kết thúc thủ công hoặc không tìm thấy → bỏ qua
            if (session == null || session.Status == ConsultationSessionConstants.ENDED) return;
            // 1. Cập nhật trạng thái Session
            session.Status = ConsultationSessionConstants.ENDED;
            session.EndedAt = DateTime.Now;
            var autoNote = "Phiên tư vấn tự động kết thúc do hết thời gian";
            session.Note = string.IsNullOrWhiteSpace(session.Note)
                ? autoNote
                : $"{session.Note}; {autoNote}";

            _unitOfWork.Repository<ConsultationSessions>().Update(session);

            // 2. Cập nhật trạng thái Appointment sang Completed (nếu chưa bị hủy)
            var appointment = await _unitOfWork.Repository<Appointments>().GetByIdAsync(session.AppointmentId);
            if (appointment != null && appointment.Status != AppointmentConstants.CANCELLED)
            {
                appointment.Status = AppointmentConstants.COMPLETED;
                _unitOfWork.Repository<Appointments>().Update(appointment);
                if (session.DoctorJoined)
                {
                    var payout = await _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                        .FirstOrDefaultAsync(p => p.AppointmentId == appointment.AppointmentId);

                    if (payout != null)
                    {
                        payout.ConsultationId = session.ConsultanSessionId;
                        payout.Status = "ReadyToPay";
                        _unitOfWork.Repository<DoctorPayout>().Update(payout);
                    }
                }
            }

            // Lưu tất cả thay đổi vào DB
            await _unitOfWork.CompleteAsync();

            // 3. Gửi thông báo kết thúc cho cả 2 bên
            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(session.MemberId);
            var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(session.DoctorId);

            // [QUAN TRỌNG]: Tìm chủ hộ (Family Head) để gửi Notification
            Guid? headUserId = member?.UserId;
            if (!headUserId.HasValue && member?.FamilyId != null)
            {
                var familyManager = await _unitOfWork.Repository<Members>().GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.FamilyId == member.FamilyId && m.UserId != null);
                headUserId = familyManager?.UserId;
            }

            if (headUserId.HasValue)
            {
                await _notificationService.SendNotificationAsync(
                    userId: headUserId.Value,
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
                    message: $"Phiên tư vấn với bệnh nhân {member?.FullName ?? "Unknown"} đã hết thời gian. Bạn vẫn có thể gửi tin nhắn bổ sung.",
                    type: ConsultationSessionActionTypes.SESSION_TIMEOUT,
                    referenceId: session.ConsultanSessionId
                );
            }
        }

        public async Task AutoCancelUnapprovedAppointmentAsync(Guid appointmentId)
        {
            // 1. Lấy thông tin chi tiết lịch hẹn kèm thông tin thành viên và gia đình
            var appointment = await _unitOfWork.Repository<Appointments>().GetQueryable()
                .Include(a => a.Member)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            // Chỉ xử lý nếu lịch hẹn tồn tại và vẫn đang ở trạng thái Chờ duyệt (Pending)
            if (appointment == null || appointment.Status != AppointmentConstants.PENDING)
                return;

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 2. Cập nhật trạng thái lịch hẹn
                appointment.Status = AppointmentConstants.CANCELLED;
                appointment.CancelReason = "Hệ thống tự động hủy do bác sĩ không xác nhận kịp thời (quá hạn duyệt).";
                _unitOfWork.Repository<Appointments>().Update(appointment);

                var member = appointment.Member;

                // 3. Nếu đã thanh toán bằng tiền -> Cập nhật trạng thái chờ hoàn tiền và hủy Payout
                if (appointment.PaymentStatus == "Paid")
                {
                    appointment.PaymentStatus = "Refunded";
                    
                    var payout = await _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                        .FirstOrDefaultAsync(p => p.AppointmentId == appointment.AppointmentId && p.Status == "Hold" && p.Amount > 0);
                    if (payout != null)
                    {
                        payout.Status = "Cancelled";
                        _unitOfWork.Repository<DoctorPayout>().Update(payout);
                    }

                    await _notificationService.SendNotificationToRoleAsync(
                        Roles.Admin,
                        "Yêu cầu hoàn tiền mới",
                        $"Lịch hẹn {appointment.AppointmentId.ToString()[..8].ToUpper()} vừa bị hủy tự động do hết hạn duyệt và cần được hoàn tiền.",
                        "Warning"
                    );
                }

                // 4. Chuẩn bị thông tin thông báo
                string timeStr = appointment.AppointmentTime.ToString(@"hh\:mm");
                string dateStr = appointment.AppointmentDate.ToString("dd/MM/yyyy");
                string patientName = member?.FullName ?? "Thành viên";

                // [QUAN TRỌNG]: Tìm chủ hộ (Family Head) để gửi Notification và SignalR
                Guid? headUserId = member?.UserId;
                if (!headUserId.HasValue && member?.FamilyId != null)
                {
                    var familyManager = await _unitOfWork.Repository<Members>().GetQueryable()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.FamilyId == member.FamilyId && m.UserId != null);
                    headUserId = familyManager?.UserId;
                }

                // 5. GỬI THÔNG BÁO CHO USER (Người đặt lịch/Chủ hộ)
                if (headUserId.HasValue)
                {
                    await _notificationService.SendNotificationAsync(
                        userId: headUserId.Value,
                        title: "❌ Lịch hẹn đã bị hủy tự động",
                        message: $"Lịch hẹn cho {patientName} vào {timeStr} ngày {dateStr} đã bị hủy do bác sĩ không xác nhận kịp thời. Số tiền đặt lịch sẽ được hoàn lại trong 1-2 ngày làm việc. Vui lòng liên hệ bộ phận hỗ trợ nếu có thắc mắc.",
                        type: AppointmentActionTypes.APPOINTMENT_CANCELLED,
                        referenceId: appointment.AppointmentId
                    );

                    if (appointment.PaymentStatus == "Refunded")
                    {
                        bool userHasBankAccount = await _unitOfWork.Repository<UserBankAccount>().GetQueryable()
                            .AnyAsync(b => b.UserId == headUserId.Value);

                        if (!userHasBankAccount)
                        {
                            await _notificationService.SendNotificationAsync(
                                userId: headUserId.Value,
                                title: "⚠️ Bạn chưa có thông tin ngân hàng để nhận hoàn tiền!",
                                message: "Lịch hẹn bị hủy tự động và hệ thống sẽ hoàn tiền cho bạn. " +
                                         "Tuy nhiên, bạn chưa cập nhật thông tin ngân hàng. " +
                                         "Vui lòng vào Cài đặt → Tài khoản ngân hàng để thêm thông tin nhận hoàn tiền.",
                                type: "BANKING_INFO_MISSING",
                                referenceId: appointment.AppointmentId
                            );
                        }
                    }

                    // SignalR: Cập nhật UI ngay lập tức cho Manager
                    await _hubContext.Clients.Group($"User_{headUserId.Value}").SendAsync("AppointmentStatusUpdated", new
                    {
                        appointmentId = appointment.AppointmentId,
                        status = appointment.Status
                    });
                }
                
                // SignalR: Cập nhật UI cho máy phụ nếu Member có account riêng
                if (member?.UserId != null && member.UserId.Value != headUserId)
                {
                    await _hubContext.Clients.Group($"User_{member.UserId.Value}").SendAsync("AppointmentStatusUpdated", new
                    {
                        appointmentId = appointment.AppointmentId,
                        status = appointment.Status
                    });
                }

                // 6. GỬI THÔNG BÁO CHO MEMBER (Nếu là hồ sơ bệnh nhân riêng lẻ)
                await _notificationService.SendNotificationAsync(
                    userId: null,
                    title: "❌ Lịch hẹn của bạn đã bị hủy",
                    message: $"Lịch hẹn khám vào lúc {timeStr} ngày {dateStr} đã bị hủy tự động. Vui lòng chọn khung giờ hoặc bác sĩ khác.",
                    type: AppointmentActionTypes.APPOINTMENT_CANCELLED,
                    referenceId: appointment.AppointmentId,
                    memberId: appointment.MemberId
                );
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

                // Lưu toàn bộ thay đổi
                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}