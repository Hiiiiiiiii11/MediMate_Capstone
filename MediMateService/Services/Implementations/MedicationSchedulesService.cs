using Hangfire;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Share.Common;
using Share.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services.Implementations
{
    public class MedicationSchedulesService : IMedicationSchedulesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IActivityLogService _activityLogService;

        public MedicationSchedulesService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IBackgroundJobClient backgroundJobClient, IActivityLogService activityLogService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _backgroundJobClient = backgroundJobClient;
            _activityLogService = activityLogService;
        }

        public async Task<ApiResponse<ScheduleResponse>> CreateScheduleAsync(Guid memberId, Guid currentUserId, CreateScheduleRequest request)
        {
            if (!await _currentUserService.CheckAccess(memberId, currentUserId))
                return ApiResponse<ScheduleResponse>.Fail("Không có quyền truy cập.", 403);

            var schedule = new MedicationSchedules
            {
                ScheduleId = Guid.NewGuid(),
                MemberId = memberId,
                ScheduleName = request.ScheduleName,
                TimeOfDay = request.TimeOfDay,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<MedicationSchedules>().AddAsync(schedule);
            await _unitOfWork.CompleteAsync();

            var targetMember = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            schedule.Member = targetMember;

            return ApiResponse<ScheduleResponse>.Ok(MapToResponse(schedule), "Đã tạo khung giờ thành công.");
        }

        // 2. LẤY NHẮC NHỞ TRONG NGÀY CHO GIAO DIỆN (UI)
        public async Task<ApiResponse<IEnumerable<ReminderDailyResponse>>> GetDailyRemindersAsync(Guid memberId, Guid currentUserId, DateTime date)
        {
            var startDate = date.Date;
            var endDate = date.Date.AddDays(1).AddTicks(-1);

            var reminders = await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.Schedule.MemberId == memberId
                                && r.ReminderDate >= startDate
                                && r.ReminderDate <= endDate
                                && r.Schedule.IsActive,
                        includeProperties: "Schedule,Schedule.Member,Schedule.ScheduleDetails,Schedule.ScheduleDetails.PrescriptionMedicine");

            // Preload dictionary TakenByUserId -> FullName
            var takenUserIds = reminders.Where(r => r.TakenByUserId.HasValue)
                                        .Select(r => r.TakenByUserId!.Value).Distinct().ToList();
            var takenByMembers = takenUserIds.Any()
                ? (await _unitOfWork.Repository<Members>().FindAsync(m => takenUserIds.Contains(m.UserId!.Value)))
                    .ToDictionary(m => m.UserId!.Value, m => m.FullName)
                : new Dictionary<Guid, string>();

            var response = reminders.OrderBy(r => r.ReminderTime)
                                    .Select(r => MapToReminderDailyResponse(r, takenByMembers));
            return ApiResponse<IEnumerable<ReminderDailyResponse>>.Ok(response);
        }

        // 3. XỬ LÝ NÚT BẤM "ĐÃ UỐNG" VÀ GHI LOG
        public async Task<ApiResponse<bool>> MarkReminderActionAsync(Guid reminderId, Guid currentUserId, MedicationActionRequest request)
        {
            // 1. Lấy Reminder - Quan trọng: không dùng Include(Schedule) nếu chỉ để update status reminder
            // trừ khi bạn thực sự cần dữ liệu của Schedule để ghi Log.
            var reminder = (await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.ReminderId == reminderId, "Schedule")).FirstOrDefault();

            if (reminder == null)
                return ApiResponse<bool>.Fail("Không tìm thấy nhắc nhở.", 404);

            // Chốt chặn quan trọng: Nếu đã xử lý rồi thì thoát ra ngay (tránh lỗi nhấn nhanh 2 lần)
            if (reminder.Status != "Pending" && reminder.Status != "Snoozed")
                return ApiResponse<bool>.Fail("Nhắc nhở này đã được xử lý trước đó.", 400);

            // 2. Cập nhật trạng thái Reminder
            reminder.Status = request.Status; // "Taken" hoặc "Skipped"
            reminder.TakenByUserId = currentUserId; // Lưu lại ai là người nhấn nút

            // ✅ LƯU Ý: Trong Entity Framework, khi bạn đã Find một đối tượng (không dùng AsNoTracking),
            // bạn chỉ cần thay đổi giá trị thuộc tính. 
            // KHÔNG cần gọi .Update(reminder) vì nó sẽ kéo theo lỗi trùng Key ở các bảng liên quan (Schedule).

            // 3. Sinh ra Log lưu lịch sử
            var localActualTime = request.ActualTime.Kind == DateTimeKind.Utc
                ? TimeZoneInfo.ConvertTimeFromUtc(request.ActualTime, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
                : request.ActualTime;

            var log = new MedicationLogs
            {
                LogId = Guid.NewGuid(),
                MemberId = reminder.Schedule.MemberId,
                ScheduleId = reminder.ScheduleId,
                ReminderId = reminder.ReminderId,
                LogDate = DateTime.Now.Date,
                ScheduledTime = reminder.ReminderTime,
                ActualTime = localActualTime,
                Status = request.Status,
                Notes = request.Notes,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<MedicationLogs>().AddAsync(log);

            // 4. Lưu thay đổi
            // Lúc này EF sẽ tự phát hiện reminder bị thay đổi Status và log là bản ghi mới.
            await _unitOfWork.CompleteAsync();


            return ApiResponse<bool>.Ok(true, "Đã lưu lịch sử uống thuốc thành công.");
        }

        public async Task<ApiResponse<bool>> SnoozeReminderAsync(Guid reminderId, Guid currentUserId, int delayMinutes)
        {
            var reminder = (await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.ReminderId == reminderId, "Schedule")).FirstOrDefault();

            if (reminder == null || reminder.Status != "Pending")
                return ApiResponse<bool>.Fail("Không tìm thấy nhắc nhở hoặc đã xử lý.", 404);

            if (!await _currentUserService.CheckAccess(reminder.Schedule.MemberId, currentUserId))
                return ApiResponse<bool>.Fail("Không có quyền truy cập.", 403);

            if (delayMinutes <= 0 || delayMinutes > 1440) return ApiResponse<bool>.Fail("Thời gian hoãn không hợp lệ.", 400);

            var nextPushTime = DateTime.Now.AddMinutes(delayMinutes);
            if (nextPushTime >= reminder.EndTime)
            {
                return ApiResponse<bool>.Fail("Không thể hoãn vì đã sắp hết thời gian được phép uống thuốc.", 400);
            }

            // Ghi Log Snoozed
            var log = new MedicationLogs
            {
                LogId = Guid.NewGuid(),
                MemberId = reminder.Schedule.MemberId,
                ScheduleId = reminder.ScheduleId,
                ReminderId = reminder.ReminderId,
                LogDate = DateTime.Now.Date,
                ScheduledTime = reminder.ReminderTime,
                ActualTime = DateTime.Now,
                Status = "Snoozed",
                Notes = $"Đã báo thức lại sau {delayMinutes} phút.",
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<MedicationLogs>().AddAsync(log);
            
            // Cập nhật trạng thái Reminder thành Snoozed (Tuỳ chọn để UI biết)
            reminder.Status = "Snoozed";
            _unitOfWork.Repository<MedicationReminders>().Update(reminder);
            
            await _unitOfWork.CompleteAsync();

            // Re-schedule Hangfire Job để đẩy thông báo tiếp theo
            _backgroundJobClient.Schedule<IReminderJobService>(
                job => job.NotifyReminderTimeAsync(reminder.ReminderId, 1),
                new DateTimeOffset(nextPushTime)
            );

            // KHÔNG đẩy EndTime đi chỗ khác, cũng KHÔNG tạo lại Job Missed check. 
            // Job Missed check ban đầu vẫn nằm ở EndTime đợi sẵn để chốt sổ!

            return ApiResponse<bool>.Ok(true, "Đã hoãn báo thức thành công.");
        }

        // =======================================================
        // LẤY NHẮC NHỞ THEO GIA ĐÌNH (CHO DASHBOARD CHUNG)
        // =======================================================
        public async Task<ApiResponse<IEnumerable<ReminderDailyResponse>>> GetFamilyDailyRemindersAsync(Guid familyId, Guid currentUserId, DateTime date)
        {
            var isFamilyMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId && (m.UserId == currentUserId || m.MemberId == currentUserId))).Any();

            if (!isFamilyMember) return ApiResponse<IEnumerable<ReminderDailyResponse>>.Fail("Bạn không có quyền xem thông tin gia đình này.", 403);

            var startDate = date.Date;
            var endDate = date.Date.AddDays(1).AddTicks(-1);

            var reminders = await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.Schedule.Member.FamilyId == familyId
                                && r.ReminderDate >= startDate
                                && r.ReminderDate <= endDate
                                && r.Schedule.IsActive,
                        includeProperties: "Schedule,Schedule.Member,Schedule.ScheduleDetails,Schedule.ScheduleDetails.PrescriptionMedicine");

            // Preload dictionary TakenByUserId -> FullName
            var takenUserIds = reminders.Where(r => r.TakenByUserId.HasValue)
                                        .Select(r => r.TakenByUserId!.Value).Distinct().ToList();
            var takenByMembers = takenUserIds.Any()
                ? (await _unitOfWork.Repository<Members>().FindAsync(m => takenUserIds.Contains(m.UserId!.Value)))
                    .ToDictionary(m => m.UserId!.Value, m => m.FullName)
                : new Dictionary<Guid, string>();

            var response = reminders.OrderBy(r => r.ReminderTime)
                                    .Select(r => MapToReminderDailyResponse(r, takenByMembers));
            return ApiResponse<IEnumerable<ReminderDailyResponse>>.Ok(response);
        }


        // =======================================================
        // CẬP NHẬT LỊCH (UPDATE)
        // =======================================================
        public async Task<ApiResponse<ScheduleResponse>> UpdateScheduleAsync(Guid scheduleId, Guid currentUserId, UpdateScheduleRequest request)
        {
            var schedule = (await _unitOfWork.Repository<MedicationSchedules>()
                .FindAsync(s => s.ScheduleId == scheduleId, "Member")).FirstOrDefault();

            if (schedule == null) return ApiResponse<ScheduleResponse>.Fail("Không tìm thấy khung giờ.", 404);

            if (!await _currentUserService.CheckAccess(schedule.MemberId, currentUserId))
                return ApiResponse<ScheduleResponse>.Fail("Access Denied", 403);

            // Cập nhật thông tin cơ bản
            schedule.ScheduleName = request.ScheduleName;
            
            bool timeChanged = schedule.TimeOfDay != request.TimeOfDay;
            schedule.TimeOfDay = request.TimeOfDay;

            _unitOfWork.Repository<MedicationSchedules>().Update(schedule);
            await _unitOfWork.CompleteAsync();
            
            // Nếu đổi giờ, cần update lại các reminders pending
            if (timeChanged)
            {
                var familySetting = (await _unitOfWork.Repository<NotificationSetting>()
                    .FindAsync(s => s.FamilyId == schedule.Member.FamilyId)).FirstOrDefault();
                int advanceMinutes = familySetting?.ReminderAdvanceMinutes ?? 15;

                var futurePendingReminders = await _unitOfWork.Repository<MedicationReminders>()
                    .FindAsync(r => r.ScheduleId == scheduleId && r.Status == "Pending" && r.ReminderDate >= DateTime.Now.Date);

                foreach (var r in futurePendingReminders)
                {
                    r.ReminderTime = r.ReminderDate.Add(schedule.TimeOfDay);
                    r.EndTime = r.ReminderTime.AddHours(2); // Đồng bộ window 2h như CreateBulk
                    _unitOfWork.Repository<MedicationReminders>().Update(r);
                    
                    var pushTime = r.ReminderTime.AddMinutes(-advanceMinutes);
                    if (pushTime < DateTime.Now) pushTime = DateTime.Now.AddMinutes(1);

                    // Re-schedule jobs mới. 
                    _backgroundJobClient.Schedule<IReminderJobService>(
                        job => job.NotifyReminderTimeAsync(r.ReminderId, 1),
                        new DateTimeOffset(pushTime)
                    );
                    _backgroundJobClient.Schedule<IReminderJobService>(
                        job => job.CheckMissedReminderAndAlertFamilyAsync(r.ReminderId),
                        new DateTimeOffset(r.EndTime)
                    );
                }
                await _unitOfWork.CompleteAsync();
            }

            return ApiResponse<ScheduleResponse>.Ok(MapToResponse(schedule), "Cập nhật thành công.");
        }

        public async Task<ApiResponse<ScheduleDetailItemResponse>> UpdateScheduleDetailAsync(Guid detailId, Guid currentUserId, UpdateScheduleDetailRequest request)
        {
            var detail = (await _unitOfWork.Repository<MedicationScheduleDetails>()
                .FindAsync(d => d.ScheduleDetailId == detailId, "Schedule,PrescriptionMedicine"))
                .FirstOrDefault();

            if (detail == null) return ApiResponse<ScheduleDetailItemResponse>.Fail("Không tìm thấy chi tiết lịch.", 404);

            if (!await _currentUserService.CheckAccess(detail.Schedule.MemberId, currentUserId))
                return ApiResponse<ScheduleDetailItemResponse>.Fail("Access Denied", 403);

            if (!string.IsNullOrEmpty(request.Dosage)) detail.Dosage = request.Dosage;
            if (request.StartDate.HasValue) detail.StartDate = request.StartDate.Value.Date;
            if (request.EndDate.HasValue) detail.EndDate = request.EndDate.Value.Date;

            _unitOfWork.Repository<MedicationScheduleDetails>().Update(detail);
            await _unitOfWork.CompleteAsync();

            if (request.EndDate.HasValue || request.StartDate.HasValue)
            {
                var schedule = detail.Schedule;
                var now = DateTime.Now.Date;
                var beginGenDate = request.StartDate.HasValue && request.StartDate.Value.Date >= now ? request.StartDate.Value.Date : now;
                var endGenDate = request.EndDate ?? detail.EndDate;

                if (endGenDate >= now)
                {
                    var existingReminders = await _unitOfWork.Repository<MedicationReminders>()
                        .FindAsync(r => r.ScheduleId == schedule.ScheduleId && r.ReminderDate >= beginGenDate && r.ReminderDate <= endGenDate);
                    
                    var newReminders = new List<MedicationReminders>();

                    for (var date = beginGenDate; date <= endGenDate; date = date.AddDays(1))
                    {
                        var reminderTime = date.Add(schedule.TimeOfDay);
                        var endTime = reminderTime.AddHours(2);
                        
                        if (endTime > DateTime.Now && !existingReminders.Any(r => r.ReminderDate == date))
                        {
                            var reminder = new MedicationReminders
                            {
                                ReminderId = Guid.NewGuid(),
                                ScheduleId = schedule.ScheduleId,
                                ReminderDate = date,
                                ReminderTime = reminderTime,
                                EndTime = endTime,
                                Status = "Pending"
                            };
                            await _unitOfWork.Repository<MedicationReminders>().AddAsync(reminder);
                            newReminders.Add(reminder);
                        }
                    }

                    if (newReminders.Any())
                    {
                        var familySetting = (await _unitOfWork.Repository<NotificationSetting>()
                            .FindAsync(s => s.FamilyId == detail.Schedule.Member.FamilyId)).FirstOrDefault();
                        int advanceMinutes = familySetting?.ReminderAdvanceMinutes ?? 15;

                        await _unitOfWork.CompleteAsync();
                        foreach (var reminder in newReminders)
                        {
                            var pushTime = reminder.ReminderTime.AddMinutes(-advanceMinutes);
                            if (pushTime < DateTime.Now) pushTime = DateTime.Now.AddMinutes(1);

                            _backgroundJobClient.Schedule<IReminderJobService>(
                                job => job.NotifyReminderTimeAsync(reminder.ReminderId, 1),
                                new DateTimeOffset(pushTime) // Báo trước AdvanceMinutes
                            );
                            _backgroundJobClient.Schedule<IReminderJobService>(
                                job => job.CheckMissedReminderAndAlertFamilyAsync(reminder.ReminderId),
                                new DateTimeOffset(reminder.EndTime) // Đánh dấu Missed tại EndTime
                            );
                        }
                    }
                }
            }

            var response = new ScheduleDetailItemResponse
            {
                DetailId = detail.ScheduleDetailId,
                PrescriptionMedicineId = detail.PrescriptionMedicineId,
                MedicineName = detail.PrescriptionMedicine?.MedicineName ?? "Thuốc",
                Dosage = detail.Dosage,
                Instructions = detail.PrescriptionMedicine?.Instructions ?? string.Empty,
                StartDate = detail.StartDate,
                EndDate = detail.EndDate
            };

            return ApiResponse<ScheduleDetailItemResponse>.Ok(response, "Cập nhật chi tiết lịch thành công.");
        }

        public async Task<ApiResponse<ScheduleDetailResponse>> GetScheduleByIdAsync(Guid scheduleId, Guid currentUserId)
        {
            var schedule = (await _unitOfWork.Repository<MedicationSchedules>()
                .FindAsync(
                    s => s.ScheduleId == scheduleId,
                    includeProperties: "Member,ScheduleDetails,ScheduleDetails.PrescriptionMedicine"
                )).FirstOrDefault();

            if (schedule == null)
                return ApiResponse<ScheduleDetailResponse>.Fail("Không tìm thấy lịch uống thuốc.", 404);

            if (!await _currentUserService.CheckAccess(schedule.MemberId, currentUserId))
                return ApiResponse<ScheduleDetailResponse>.Fail("Bạn không có quyền xem thông tin này.", 403);

            return ApiResponse<ScheduleDetailResponse>.Ok(MapToDetailResponse(schedule));
        }

        // =======================================================
        // XÓA LỊCH (DELETE)
        // =======================================================
        //public async Task<ApiResponse<bool>> DeleteScheduleAsync(Guid scheduleId, Guid currentUserId)
        //{
        //    var schedule = await _unitOfWork.Repository<MedicationSchedules>().GetByIdAsync(scheduleId);
        //    if (schedule == null) return ApiResponse<bool>.Fail("Không tìm thấy khung giờ.", 404);

        //    if (!await _currentUserService.CheckAccess(schedule.MemberId, currentUserId))
        //        return ApiResponse<bool>.Fail("Access Denied", 403);

        //    schedule.IsActive = false;

        //    var pendingReminders = await _unitOfWork.Repository<MedicationReminders>()
        //        .FindAsync(r => r.ScheduleId == scheduleId && r.Status == "Pending");

        //    _unitOfWork.Repository<MedicationReminders>().RemoveRange(pendingReminders);
        //    _unitOfWork.Repository<MedicationSchedules>().Update(schedule);

        //    await _unitOfWork.CompleteAsync();

        //    return ApiResponse<bool>.Ok(true, "Đã xóa khung giờ.");
        //}

        public async Task<ApiResponse<bool>> DeleteScheduleAsync(Guid scheduleId, Guid currentUserId)
        {
            var schedule = await _unitOfWork.Repository<MedicationSchedules>().GetByIdAsync(scheduleId);
            if (schedule == null) return ApiResponse<bool>.Fail("Không tìm thấy khung giờ.", 404);

            if (!await _currentUserService.CheckAccess(schedule.MemberId, currentUserId))
                return ApiResponse<bool>.Fail("Access Denied", 403);

            // ==========================================
            // BƯỚC 1: XÓA SẠCH DỮ LIỆU CON (Đề phòng DB không bật Cascade Delete)
            // ==========================================

            // 1.1 Xóa TẤT CẢ Reminders của Schedule này (Thay vì chỉ xóa Pending như cũ)
            var allReminders = await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.ScheduleId == scheduleId);
            if (allReminders.Any())
            {
                _unitOfWork.Repository<MedicationReminders>().RemoveRange(allReminders);
            }

            // 1.2 Xóa TẤT CẢ các Chi tiết thuốc (ScheduleDetails) nằm trong khung giờ này
            var scheduleDetails = await _unitOfWork.Repository<MedicationScheduleDetails>()
                .FindAsync(d => d.ScheduleId == scheduleId);
            if (scheduleDetails.Any())
            {
                _unitOfWork.Repository<MedicationScheduleDetails>().RemoveRange(scheduleDetails);
            }

            // ==========================================
            // BƯỚC 2: XÓA CỨNG BẢNG CHA (HARD DELETE)
            // ==========================================

            _unitOfWork.Repository<MedicationSchedules>().Remove(schedule); // Dùng Remove thay vì Update

            // Lưu toàn bộ thay đổi xuống DB
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Đã xóa vĩnh viễn khung giờ và các dữ liệu liên quan.");
        }

        public async Task<ApiResponse<IEnumerable<ScheduleResponse>>> GetMemberSchedulesAsync(Guid memberId, Guid currentUserId)
        {
            if (!await _currentUserService.CheckAccess(memberId, currentUserId))
                return ApiResponse<IEnumerable<ScheduleResponse>>.Fail("Không có quyền truy cập.", 403);

            var schedules = await _unitOfWork.Repository<MedicationSchedules>()
                .FindAsync(s => s.MemberId == memberId && s.IsActive == true, "Member,ScheduleDetails,ScheduleDetails.PrescriptionMedicine");

            var response = schedules
                .OrderByDescending(s => s.IsActive)
                .ThenBy(s => s.TimeOfDay)
                .Select(s => MapToResponse(s));

            return ApiResponse<IEnumerable<ScheduleResponse>>.Ok(response);
        }

        public async Task<ApiResponse<IEnumerable<ScheduleResponse>>> GetFamilySchedulesAsync(Guid familyId, Guid currentUserId)
        {
            var isFamilyMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId && (m.UserId == currentUserId || m.MemberId == currentUserId))).Any();

            if (!isFamilyMember)
                return ApiResponse<IEnumerable<ScheduleResponse>>.Fail("Bạn không có quyền xem thông tin gia đình này.", 403);

            var schedules = await _unitOfWork.Repository<MedicationSchedules>()
                .FindAsync(s => s.Member.FamilyId == familyId, "Member,ScheduleDetails,ScheduleDetails.PrescriptionMedicine");

            var response = schedules
                .OrderByDescending(s => s.IsActive)
                .ThenBy(s => s.TimeOfDay)
                .Select(s => MapToResponse(s));

            return ApiResponse<IEnumerable<ScheduleResponse>>.Ok(response);
        }

        // =======================================================
        // TẠO NHIỀU THUỐC VÀO CÁC KHUNG GIỜ CÙNG LÚC
        // =======================================================
        public async Task<ApiResponse<List<ScheduleResponse>>> CreateBulkSchedulesAsync(Guid memberId, Guid currentUserId, CreateBulkScheduleRequest request)
        {
            if (!await _currentUserService.CheckAccess(memberId, currentUserId))
                return ApiResponse<List<ScheduleResponse>>.Fail("Không có quyền truy cập.", 403);

            var targetMember = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (targetMember == null) return ApiResponse<List<ScheduleResponse>>.Fail("Không tìm thấy bệnh nhân.", 404);

            int minHoursGap = 2; // Default
            int maxDosesPerDay = 6; // Default
            int advanceMinutes = 15; // Default

            if (targetMember.FamilyId.HasValue)
            {
                var familySetting = (await _unitOfWork.Repository<NotificationSetting>()
                    .FindAsync(s => s.FamilyId == targetMember.FamilyId.Value)).FirstOrDefault();
                if (familySetting != null)
                {
                    minHoursGap = familySetting.MinimumHoursGap > 0 ? familySetting.MinimumHoursGap : 2;
                    maxDosesPerDay = familySetting.MaxDosesPerDay > 0 ? familySetting.MaxDosesPerDay : 6;
                    advanceMinutes = familySetting.ReminderAdvanceMinutes;
                }
            }

            // --- KIỂM TRA RÀNG BUỘC (CONFLICT VALIDATION) ---
            var proposedDosesCount = new Dictionary<string, int>(); // Số liều tạo ra mỗi ngày cho mỗi thuốc
            var allProposedTimes = new Dictionary<string, List<TimeSpan>>();

            foreach (var item in request.Schedules)
            {
                var timeRanges = ParseTimeRanges(item.SpecificTimes);
                if (!proposedDosesCount.ContainsKey(item.MedicineName))
                {
                    proposedDosesCount[item.MedicineName] = 0;
                    allProposedTimes[item.MedicineName] = new List<TimeSpan>();
                }

                proposedDosesCount[item.MedicineName] += timeRanges.Count;

                if (proposedDosesCount[item.MedicineName] > maxDosesPerDay)
                {
                    return ApiResponse<List<ScheduleResponse>>.Fail($"Vượt quá số liều tối đa mỗi ngày ({maxDosesPerDay} liều) cho thuốc {item.MedicineName}.", 400);
                }

                foreach (var range in timeRanges)
                {
                    // Check overlapped blocks
                    foreach (var existingTime in allProposedTimes[item.MedicineName])
                    {
                        var diff = Math.Abs((existingTime - range.Start).TotalHours);
                        if (diff < minHoursGap)
                        {
                            return ApiResponse<List<ScheduleResponse>>.Fail($"Khoảng cách giữa 2 liều của {item.MedicineName} quá sát nhau. Cần cách nhau tối thiểu {minHoursGap} giờ.", 400);
                        }
                    }
                    allProposedTimes[item.MedicineName].Add(range.Start);
                }
            }
            // --- KẾT THÚC KIỂM TRA RÀNG BUỘC ---

            // Fetch existing schedules for the member to reuse time blocks
            var existingSchedules = (await _unitOfWork.Repository<MedicationSchedules>()
                .FindAsync(s => s.MemberId == memberId)).ToList();

            var allReminders = new List<MedicationReminders>();
            var modifiedSchedules = new HashSet<MedicationSchedules>();

            foreach (var item in request.Schedules)
            {
                // In Pillbox, we convert "SpecificTimes" to time blocks
                var timeRanges = ParseTimeRanges(item.SpecificTimes);

                foreach (var range in timeRanges)
                {
                    TimeSpan timeBlock = range.Start;
                    string blockName = GetBlockNameConvention(timeBlock);

                    // Find or create TimeBlock
                    var schedule = existingSchedules.FirstOrDefault(s => s.TimeOfDay == timeBlock);
                    if (schedule == null)
                    {
                        schedule = new MedicationSchedules
                        {
                            ScheduleId = Guid.NewGuid(),
                            MemberId = memberId,
                            ScheduleName = blockName,
                            TimeOfDay = timeBlock,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };
                        await _unitOfWork.Repository<MedicationSchedules>().AddAsync(schedule);
                        existingSchedules.Add(schedule);
                    }
                    modifiedSchedules.Add(schedule);

                    // Find Prescription Medicine
                    Guid presMedId = Guid.Empty;
                    if (request.PrescriptionId.HasValue)
                    {
                        var presMed = (await _unitOfWork.Repository<PrescriptionMedicines>()
                            .FindAsync(pm => pm.PrescriptionId == request.PrescriptionId.Value && pm.MedicineName == item.MedicineName)).FirstOrDefault();
                        if (presMed != null) presMedId = presMed.PrescriptionMedicineId;
                    }

                    // Add detail
                    var detail = new MedicationScheduleDetails
                    {
                        ScheduleDetailId = Guid.NewGuid(),
                        ScheduleId = schedule.ScheduleId,
                        PrescriptionMedicineId = presMedId,
                        Dosage = item.Dosage,
                        StartDate = item.StartDate.Date,
                        EndDate = item.EndDate?.Date ?? item.StartDate.Date.AddDays(30)
                    };
                    await _unitOfWork.Repository<MedicationScheduleDetails>().AddAsync(detail);

                    // Generate Reminder if needed
                    var endGenDate = item.EndDate ?? DateTime.Now.AddDays(30).Date;
                    for (var date = item.StartDate.Date; date <= endGenDate; date = date.AddDays(1))
                    {
                        var startTime = date.Add(timeBlock);
                        var endTime = date.Add(range.End);
                        if (endTime <= startTime) endTime = endTime.AddDays(1);

                        if (endTime > DateTime.Now)
                        {
                            // Avoid duplicate reminders for the same timeblock on the same day
                            var existingReminder = allReminders.FirstOrDefault(r => r.ScheduleId == schedule.ScheduleId && r.ReminderDate == date);
                            if (existingReminder == null)
                            {
                                var reminder = new MedicationReminders
                                {
                                    ReminderId = Guid.NewGuid(),
                                    ScheduleId = schedule.ScheduleId,
                                    ReminderDate = date,
                                    ReminderTime = startTime,
                                    EndTime = endTime,
                                    Status = "Pending"
                                };
                                await _unitOfWork.Repository<MedicationReminders>().AddAsync(reminder);
                                allReminders.Add(reminder);
                            }
                        }
                    }
                }
            }

            await _unitOfWork.CompleteAsync();

            // Lên lịch Hangfire cho TẤT CẢ nhắc nhở mới
            foreach (var reminder in allReminders)
            {
                var pushTime = reminder.ReminderTime.AddMinutes(-advanceMinutes);
                if (pushTime < DateTime.Now) pushTime = DateTime.Now.AddMinutes(1);

                _backgroundJobClient.Schedule<IReminderJobService>(
                    job => job.NotifyReminderTimeAsync(reminder.ReminderId, 1),
                    new DateTimeOffset(pushTime) // Báo trước Advance Minutes
                );

                _backgroundJobClient.Schedule<IReminderJobService>(
                    job => job.CheckMissedReminderAndAlertFamilyAsync(reminder.ReminderId),
                    new DateTimeOffset(reminder.EndTime) // Lúc chốt sổ Missed
                );
            }

            var schedulesToReturn = await _unitOfWork.Repository<MedicationSchedules>()
                .FindAsync(s => s.MemberId == memberId, "Member,ScheduleDetails,ScheduleDetails.PrescriptionMedicine");
            var responseData = schedulesToReturn.Where(s => modifiedSchedules.Select(ms => ms.ScheduleId).Contains(s.ScheduleId)).Select(MapToResponse).ToList();

            return ApiResponse<List<ScheduleResponse>>.Ok(responseData, "Đã lưu lịch uống thuốc thành công.");
        }

        public async Task<ApiResponse<bool>> UpdateMemberPreferredTimesAsync(Guid memberId, Guid currentUserId, UpdateMemberPreferredTimesRequest request)
        {
            // 1. Kiểm tra quyền truy cập
            if (!await _currentUserService.CheckAccess(memberId, currentUserId))
                return ApiResponse<bool>.Fail("Không có quyền truy cập.", 403);

            // 2. Lấy tất cả các lịch hiện có của Member này
            var existingSchedules = await _unitOfWork.Repository<MedicationSchedules>()
                .FindAsync(s => s.MemberId == memberId, "Member");

            if (!existingSchedules.Any())
                return ApiResponse<bool>.Ok(true, "Thành viên chưa có lịch uống thuốc nào để cập nhật.");

            // Lấy setting để biết cần báo trước bao nhiêu phút
            var targetMember = existingSchedules.First().Member;
            int advanceMinutes = 15;
            if (targetMember.FamilyId.HasValue)
            {
                var familySetting = (await _unitOfWork.Repository<NotificationSetting>()
                    .FindAsync(s => s.FamilyId == targetMember.FamilyId.Value)).FirstOrDefault();
                if (familySetting != null) advanceMinutes = familySetting.ReminderAdvanceMinutes;
            }

            bool hasAnyChange = false;

            // 3. Hàm xử lý cập nhật giờ và dời Reminders
            async Task ApplyNewTimeIfValid(MedicationSchedules schedule, TimeSpan? newTime, int minH, int maxH)
            {
                if (!newTime.HasValue || schedule.TimeOfDay == newTime.Value) return;

                // Validate xem giờ người dùng chọn có hợp lý với buổi đó không
                if (newTime.Value.Hours < minH || newTime.Value.Hours >= maxH)
                    throw new BadRequestException($"Giờ được chọn không phù hợp với quy chuẩn khung giờ ({minH}h-{maxH}h).");

                // Cập nhật giờ mới
                schedule.TimeOfDay = newTime.Value;
                _unitOfWork.Repository<MedicationSchedules>().Update(schedule);
                hasAnyChange = true;

                // DỜI LẠI TOÀN BỘ REMINDERS TƯƠNG LAI CỦA LỊCH NÀY
                var futurePendingReminders = await _unitOfWork.Repository<MedicationReminders>()
                    .FindAsync(r => r.ScheduleId == schedule.ScheduleId
                                    && r.Status == "Pending"
                                    && r.ReminderDate >= DateTime.Now.Date);

                foreach (var r in futurePendingReminders)
                {
                    r.ReminderTime = r.ReminderDate.Add(schedule.TimeOfDay);
                    r.EndTime = r.ReminderTime.AddHours(2);
                    _unitOfWork.Repository<MedicationReminders>().Update(r);

                    var pushTime = r.ReminderTime.AddMinutes(-advanceMinutes);
                    if (pushTime < DateTime.Now) pushTime = DateTime.Now.AddMinutes(1);

                    // Re-schedule Hangfire
                    _backgroundJobClient.Schedule<IReminderJobService>(
                        job => job.NotifyReminderTimeAsync(r.ReminderId, 1),
                        new DateTimeOffset(pushTime)
                    );
                    _backgroundJobClient.Schedule<IReminderJobService>(
                        job => job.CheckMissedReminderAndAlertFamilyAsync(r.ReminderId),
                        new DateTimeOffset(r.EndTime)
                    );
                }
            }

            try
            {
                // 4. Quét từng lịch và áp dụng giờ mới nếu tên lịch có chứa từ khóa
                foreach (var schedule in existingSchedules)
                {
                    var name = schedule.ScheduleName?.ToLower() ?? "";

                    if (name.Contains("sáng"))
                        await ApplyNewTimeIfValid(schedule, request.MorningTime, 6, 11);
                    else if (name.Contains("trưa"))
                        await ApplyNewTimeIfValid(schedule, request.NoonTime, 11, 15);
                    else if (name.Contains("chiều"))
                        await ApplyNewTimeIfValid(schedule, request.AfternoonTime, 15, 18);
                    else if (name.Contains("tối"))
                        await ApplyNewTimeIfValid(schedule, request.EveningTime, 18, 24);
                }

                if (hasAnyChange)
                {
                    await _unitOfWork.CompleteAsync();
                    return ApiResponse<bool>.Ok(true, "Đã đồng bộ giờ uống thuốc mới cho toàn bộ lịch.");
                }

                return ApiResponse<bool>.Ok(true, "Không có sự thay đổi nào được thực hiện.");
            }
            catch (BadRequestException ex)
            {
                return ApiResponse<bool>.Fail(ex.Message, 400);
            }
        }


        private ReminderDailyResponse MapToReminderDailyResponse(
            MedicationReminders r,
            Dictionary<Guid, string> takenByLookup = null)
        {
            string takenByName = null;
            if (r.TakenByUserId.HasValue && takenByLookup != null)
                takenByLookup.TryGetValue(r.TakenByUserId.Value, out takenByName);

            return new ReminderDailyResponse
            {
                ReminderId = r.ReminderId,
                ScheduleId = r.ScheduleId,
                MemberId = r.Schedule.MemberId,
                MemberName = r.Schedule.Member?.FullName ?? "Unknown",
                ScheduleName = r.Schedule.ScheduleName,
                ReminderTime = r.ReminderTime,
                EndTime = r.EndTime,
                Status = r.Status,
                TakenByUserId = r.TakenByUserId,
                TakenByName = takenByName,
                Medicines = r.Schedule.ScheduleDetails?
                    .Where(d => d.StartDate.Date <= r.ReminderDate.Date && d.EndDate.Date >= r.ReminderDate.Date)
                    .Select(d => new ScheduleDetailItemResponse
                {
                    DetailId = d.ScheduleDetailId,
                    PrescriptionMedicineId = d.PrescriptionMedicineId,
                    MedicineName = d.PrescriptionMedicine?.MedicineName ?? "Thuốc",
                    Dosage = d.Dosage,
                    Instructions = d.PrescriptionMedicine?.Instructions ?? string.Empty,
                    StartDate = d.StartDate,
                    EndDate = d.EndDate
                }).ToList() ?? new List<ScheduleDetailItemResponse>()
            };
        }
        
        private ScheduleResponse MapToResponse(MedicationSchedules schedule)
        {
            return new ScheduleResponse
            {
                ScheduleId = schedule.ScheduleId,
                MemberId = schedule.MemberId,
                MemberName = schedule.Member?.FullName ?? "Unknown",
                ScheduleName = schedule.ScheduleName,
                TimeOfDay = schedule.TimeOfDay,
                IsActive = schedule.IsActive,
                CreatedAt = schedule.CreatedAt,
                ScheduleDetails = schedule.ScheduleDetails?.Select(d => new ScheduleDetailItemResponse
                {
                    DetailId = d.ScheduleDetailId,
                    PrescriptionMedicineId = d.PrescriptionMedicineId,
                    MedicineName = d.PrescriptionMedicine?.MedicineName ?? "Thuốc",
                    Dosage = d.Dosage,
                    Instructions = d.PrescriptionMedicine?.Instructions ?? string.Empty,
                    StartDate = d.StartDate,
                    EndDate = d.EndDate
                }).ToList() ?? new List<ScheduleDetailItemResponse>()
            };
        }

        private ScheduleDetailResponse MapToDetailResponse(MedicationSchedules schedule)
        {
            var response = new ScheduleDetailResponse
            {
                ScheduleId = schedule.ScheduleId,
                MemberId = schedule.MemberId,
                MemberName = schedule.Member?.FullName ?? "Unknown",
                ScheduleName = schedule.ScheduleName,
                TimeOfDay = schedule.TimeOfDay,
                IsActive = schedule.IsActive,
                CreatedAt = schedule.CreatedAt,
                ScheduleDetails = schedule.ScheduleDetails?.Select(d => new ScheduleDetailItemResponse
                {
                    DetailId = d.ScheduleDetailId,
                    PrescriptionMedicineId = d.PrescriptionMedicineId,
                    MedicineName = d.PrescriptionMedicine?.MedicineName ?? "Thuốc",
                    Dosage = d.Dosage,
                    Instructions = d.PrescriptionMedicine?.Instructions ?? string.Empty,
                    StartDate = d.StartDate,
                    EndDate = d.EndDate
                }).ToList() ?? new List<ScheduleDetailItemResponse>()
            };
            return response;
        }

        private List<(TimeSpan Start, TimeSpan End)> ParseTimeRanges(string timesStr)
        {
            var result = new List<(TimeSpan, TimeSpan)>();
            if (string.IsNullOrEmpty(timesStr)) return result;

            foreach (var t in timesStr.Split(','))
            {
                var parts = t.Split('-');
                if (parts.Length == 2 && TimeSpan.TryParse(parts[0].Trim(), out var start) && TimeSpan.TryParse(parts[1].Trim(), out var end))
                    result.Add((start, end));
                else if (parts.Length == 1 && TimeSpan.TryParse(parts[0].Trim(), out var s))
                    result.Add((s, s.Add(TimeSpan.FromHours(1)))); // Mặc định khung 1h
            }
            return result;
        }

        private string GetBlockNameConvention(TimeSpan time)
        {
            if (time.Hours < 11) return "Sáng";
            if (time.Hours < 14) return "Trưa";
            if (time.Hours < 18) return "Chiều";
            return "Tối";
        }
    }
}
