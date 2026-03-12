using Hangfire;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
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

            // 1.1 Tạo Schedule gốc
            var schedule = new MedicationSchedules
            {
                ScheduleId = Guid.NewGuid(),
                MemberId = memberId,
                PrescriptionMedicineId = request.PrescriptionMedicineId,
                MedicineName = request.MedicineName,
                Dosage = request.Dosage,
                SpecificTimes = request.SpecificTimes,
                StartDate = request.StartDate.Date,
                EndDate = request.EndDate?.Date,
                IsActive = true
            };
            await _unitOfWork.Repository<MedicationSchedules>().AddAsync(schedule);

            // 1.2 Phân tích khung giờ (VD: 08:00-09:00)
            var timeRanges = ParseTimeRanges(request.SpecificTimes);
            var endGenDate = request.EndDate ?? DateTime.Now.AddDays(30).Date; // Giới hạn sinh 30 ngày
            var reminders = new List<MedicationReminders>();

            for (var date = schedule.StartDate; date <= endGenDate; date = date.AddDays(1))
            {
                foreach (var range in timeRanges)
                {
                    var startTime = date.Add(range.Start);
                    var endTime = date.Add(range.End);
                    if (endTime <= startTime) endTime = endTime.AddDays(1); // Qua ngày hôm sau

                    if (endTime > DateTime.Now)
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
                        reminders.Add(reminder);
                    }
                }
            }
            await _unitOfWork.CompleteAsync(); // Lưu DB để lấy ra được ID

            // 1.3 Lên lịch Hangfire kiểm tra lúc QUÁ HẠN KHUNG GIỜ (EndTime)
            foreach (var reminder in reminders)
            {
                _backgroundJobClient.Schedule<IReminderJobService>(
                    job => job.CheckAndNotifyOverdueReminder(reminder.ReminderId),
                    new DateTimeOffset(reminder.EndTime) // Chạy đúng lúc EndTime
                );
            }
            var targetMember = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (targetMember != null && targetMember.FamilyId.HasValue)
            {
                var doer = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == targetMember.FamilyId && m.UserId == currentUserId)).FirstOrDefault();

                if (doer != null)
                {
                    await _activityLogService.LogActivityAsync(
                        familyId: targetMember.FamilyId.Value,
                        memberId: doer.MemberId,
                        actionType: ActivityActionTypes.CREATE,
                        entityName: ActivityEntityNames.MEDICATION_SCHEDULE,
                        entityId: schedule.ScheduleId,
                        description: $"Đã thiết lập lịch uống thuốc '{schedule.MedicineName}' cho '{targetMember.FullName}'."
                    );
                }
            }


            schedule.Member = targetMember;
            return ApiResponse<ScheduleResponse>.Ok(MapToResponse(schedule), "Đã tạo lịch thành công.");
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
                "Schedule,Schedule.Member");

            // Thay vì viết new ReminderDailyResponse ở đây, ta gọi thẳng hàm Helper cho gọn
            var response = reminders.OrderBy(r => r.ReminderTime).Select(MapToReminderDailyResponse);
            return ApiResponse<IEnumerable<ReminderDailyResponse>>.Ok(response);

        }

        // 3. XỬ LÝ NÚT BẤM "ĐÃ UỐNG" VÀ GHI LOG
        public async Task<ApiResponse<bool>> MarkReminderActionAsync(Guid reminderId, Guid currentUserId, MedicationActionRequest request)
        {
            var reminder = (await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.ReminderId == reminderId, "Schedule")).FirstOrDefault();

            if (reminder == null || reminder.Status != "Pending")
                return ApiResponse<bool>.Fail("Hành động không hợp lệ.", 400);

            // 3.1 Cập nhật trạng thái Reminder
            reminder.Status = request.Status; // Taken hoặc Skipped
            _unitOfWork.Repository<MedicationReminders>().Update(reminder);

            // 3.2 Sinh ra Log lưu lịch sử
            var log = new MedicationLogs
            {
                LogId = Guid.NewGuid(),
                MemberId = reminder.Schedule.MemberId,
                ScheduleId = reminder.ScheduleId,
                ReminderId = reminder.ReminderId,
                LogDate = DateTime.Now.Date,
                ScheduledTime = reminder.ReminderTime,
                ActualTime = request.ActualTime,
                Status = request.Status,
                Notes = request.Notes,
                CreatedAt = DateTime.Now
            };
            await _unitOfWork.Repository<MedicationLogs>().AddAsync(log);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Đã lưu lịch sử uống thuốc.");
        }

        // Helper tách giờ
        private List<(TimeSpan Start, TimeSpan End)> ParseTimeRanges(string timesStr)
        {
            var result = new List<(TimeSpan, TimeSpan)>();
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
        // =======================================================
        // 1. LẤY NHẮC NHỞ THEO GIA ĐÌNH (CHO DASHBOARD CHUNG)
        // =======================================================
        public async Task<ApiResponse<IEnumerable<ReminderDailyResponse>>> GetFamilyDailyRemindersAsync(Guid familyId, Guid currentUserId, DateTime date)
        {
            // Kiểm tra xem User hiện tại có thuộc gia đình này không
            var isFamilyMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId && m.UserId == currentUserId)).Any();

            if (!isFamilyMember) return ApiResponse<IEnumerable<ReminderDailyResponse>>.Fail("Bạn không có quyền xem thông tin gia đình này.", 403);

            var startDate = date.Date;
            var endDate = date.Date.AddDays(1).AddTicks(-1);

            // Lấy tất cả Reminder của các thành viên trong Family
            var reminders = await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.Schedule.Member.FamilyId == familyId
                                && r.ReminderDate >= startDate
                                && r.ReminderDate <= endDate
                                && r.Schedule.IsActive,
                        includeProperties: "Schedule,Schedule.Member"); // Join tới bảng Member để lấy Tên

            var response = reminders.OrderBy(r => r.ReminderTime).Select(r => new ReminderDailyResponse
            {
                ReminderId = r.ReminderId,
                ScheduleId = r.ScheduleId,
                MemberId = r.Schedule.MemberId,
                MemberName = r.Schedule.Member.FullName, // Tên người cần uống thuốc
                MedicineName = r.Schedule.MedicineName,
                Dosage = r.Schedule.Dosage,
                ReminderTime = r.ReminderTime,
                EndTime = r.EndTime,
                Status = r.Status
            });

            return ApiResponse<IEnumerable<ReminderDailyResponse>>.Ok(response);
        }

        // =======================================================
        // 2. CẬP NHẬT LỊCH (UPDATE)
        // =======================================================
        public async Task<ApiResponse<ScheduleResponse>> UpdateScheduleAsync(Guid scheduleId, Guid currentUserId, UpdateScheduleRequest request)
        {
            var schedule = (await _unitOfWork.Repository<MedicationSchedules>()
                .FindAsync(s => s.ScheduleId == scheduleId)).FirstOrDefault();

            if (schedule == null) return ApiResponse<ScheduleResponse>.Fail("Không tìm thấy lịch.", 404);

            if (!await _currentUserService.CheckAccess(schedule.MemberId, currentUserId))
                return ApiResponse<ScheduleResponse>.Fail("Access Denied", 403);
            var oldData = new { schedule.MedicineName, schedule.Dosage, schedule.SpecificTimes, schedule.EndDate };
            bool hasChanges = false;
            // 2.1 Cập nhật thông tin cơ bản
            if (schedule.MedicineName != request.MedicineName) { schedule.MedicineName = request.MedicineName; hasChanges = true; }
            if (schedule.Dosage != request.Dosage) { schedule.Dosage = request.Dosage; hasChanges = true; }
            if (schedule.SpecificTimes != request.SpecificTimes) { schedule.SpecificTimes = request.SpecificTimes; hasChanges = true; }
            if (schedule.EndDate != request.EndDate?.Date) { schedule.EndDate = request.EndDate?.Date; hasChanges = true; }

            schedule.Instructions = request.Instructions;
            schedule.UpdatedAt = DateTime.Now;

            // 2.2 Xóa toàn bộ các Reminder "Pending" TRONG TƯƠNG LAI của lịch này
            // Mục đích: Dọn đường để tạo lại nhắc nhở với khung giờ mới
            var futurePendingReminders = await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.ScheduleId == scheduleId && r.Status == "Pending" && r.EndTime > DateTime.Now);

            _unitOfWork.Repository<MedicationReminders>().RemoveRange(futurePendingReminders);

            // 2.3 Sinh lại các Reminder mới từ thời điểm hiện tại trở đi
            var timeRanges = ParseTimeRanges(request.SpecificTimes);
            DateTime limitDate = DateTime.Now.AddDays(30).Date;
            DateTime endGenerationDate = request.EndDate.HasValue && request.EndDate.Value < limitDate
                                         ? request.EndDate.Value : limitDate;

            var newReminders = new List<MedicationReminders>();
            // Bắt đầu quét từ ngày hôm nay (Now) thay vì StartDate cũ
            for (var date = DateTime.Now.Date; date <= endGenerationDate; date = date.AddDays(1))
            {
                foreach (var range in timeRanges)
                {
                    var startTime = date.Add(range.Start);
                    var endTime = date.Add(range.End);
                    if (endTime <= startTime) endTime = endTime.AddDays(1);

                    if (endTime > DateTime.Now)
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
                        newReminders.Add(reminder);
                    }
                }
            }

            _unitOfWork.Repository<MedicationSchedules>().Update(schedule);
            await _unitOfWork.CompleteAsync();

            // 2.4 Đặt lịch Hangfire cho các Reminder mới
            foreach (var reminder in newReminders)
            {
                _backgroundJobClient.Schedule<IReminderJobService>(
                    job => job.CheckAndNotifyOverdueReminder(reminder.ReminderId),
                    new DateTimeOffset(reminder.EndTime)
                );
            }

            if (hasChanges)
            {
                var newData = new { schedule.MedicineName, schedule.Dosage, schedule.SpecificTimes, schedule.EndDate };
                var targetMember = await _unitOfWork.Repository<Members>().GetByIdAsync(schedule.MemberId);

                if (targetMember != null && targetMember.FamilyId.HasValue)
                {
                    var doer = (await _unitOfWork.Repository<Members>()
                        .FindAsync(m => m.FamilyId == targetMember.FamilyId && m.UserId == currentUserId)).FirstOrDefault();

                    if (doer != null)
                    {
                        await _activityLogService.LogActivityAsync(
                            familyId: targetMember.FamilyId.Value,
                            memberId: doer.MemberId,
                            actionType: ActivityActionTypes.UPDATE,
                            entityName: ActivityEntityNames.MEDICATION_SCHEDULE,
                            entityId: schedule.ScheduleId,
                            description: $"Đã điều chỉnh lịch uống thuốc '{schedule.MedicineName}' của '{targetMember.FullName}'.",
                            oldData: oldData,
                            newData: newData
                        );
                    }
                }
            }

            return ApiResponse<ScheduleResponse>.Ok(new ScheduleResponse
            {
                ScheduleId = schedule.ScheduleId,
                MedicineName = schedule.MedicineName,
                SpecificTimes = schedule.SpecificTimes,
                IsActive = schedule.IsActive
            }, "Cập nhật lịch thành công.");
        }

        // =======================================================
        // 3. XÓA LỊCH (DELETE)
        // =======================================================
        public async Task<ApiResponse<bool>> DeleteScheduleAsync(Guid scheduleId, Guid currentUserId)
        {
            var schedule = await _unitOfWork.Repository<MedicationSchedules>().GetByIdAsync(scheduleId);
            if (schedule == null) return ApiResponse<bool>.Fail("Không tìm thấy lịch.", 404);

            if (!await _currentUserService.CheckAccess(schedule.MemberId, currentUserId))
                return ApiResponse<bool>.Fail("Access Denied", 403);

            // KHÔNG XÓA CỨNG LỊCH (Soft Delete) để giữ lại Lịch sử (Logs) đã uống trong quá khứ
            schedule.IsActive = false;
            schedule.UpdatedAt = DateTime.Now;

            // Xóa cứng toàn bộ Reminder "Pending" để hệ thống UI không hiển thị nữa
            // Khi Hangfire chạy tới những cái đã hẹn giờ, nó tìm không thấy DB -> Tự hủy êm đẹp
            var pendingReminders = await _unitOfWork.Repository<MedicationReminders>()
                .FindAsync(r => r.ScheduleId == scheduleId && r.Status == "Pending");

            _unitOfWork.Repository<MedicationReminders>().RemoveRange(pendingReminders);
            _unitOfWork.Repository<MedicationSchedules>().Update(schedule);

            await _unitOfWork.CompleteAsync();

            var targetMember = await _unitOfWork.Repository<Members>().GetByIdAsync(schedule.MemberId);
            if (targetMember != null && targetMember.FamilyId.HasValue)
            {
                var doer = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == targetMember.FamilyId && m.UserId == currentUserId)).FirstOrDefault();

                if (doer != null)
                {
                    await _activityLogService.LogActivityAsync(
                        familyId: targetMember.FamilyId.Value,
                        memberId: doer.MemberId,
                        actionType: ActivityActionTypes.DELETE, // Có thể cân nhắc thêm một hằng số STOP/DEACTIVATE
                        entityName: ActivityEntityNames.MEDICATION_SCHEDULE,
                        entityId: schedule.ScheduleId,
                        description: $"Đã dừng lịch nhắc uống thuốc '{schedule.MedicineName}' của '{targetMember.FullName}'."
                    );
                }
            }

            return ApiResponse<bool>.Ok(true, "Đã xóa lịch uống thuốc.");
        }
        public async Task<ApiResponse<IEnumerable<ScheduleResponse>>> GetMemberSchedulesAsync(Guid memberId, Guid currentUserId)
        {
            if (!await _currentUserService.CheckAccess(memberId, currentUserId))
                return ApiResponse<IEnumerable<ScheduleResponse>>.Fail("Không có quyền truy cập.", 403);

            // Include "Member" để lấy được tên người dùng nếu cần
            var schedules = await _unitOfWork.Repository<MedicationSchedules>()
                .FindAsync(s => s.MemberId == memberId, "Member");

            // Sắp xếp: Lịch đang hoạt động lên đầu, sau đó xếp theo ngày tạo mới nhất
            var response = schedules
                .OrderByDescending(s => s.IsActive)
                .ThenByDescending(s => s.StartDate)
                .Select(s => MapToResponse(s));

            return ApiResponse<IEnumerable<ScheduleResponse>>.Ok(response);
        }

        // =======================================================
        // 5. LẤY DANH SÁCH LỊCH CỦA TOÀN BỘ GIA ĐÌNH (FAMILY)
        // =======================================================
        public async Task<ApiResponse<IEnumerable<ScheduleResponse>>> GetFamilySchedulesAsync(Guid familyId, Guid currentUserId)
        {
            // Kiểm tra xem User hiện tại có thuộc gia đình này không
            var isFamilyMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId && m.UserId == currentUserId)).Any();

            if (!isFamilyMember)
                return ApiResponse<IEnumerable<ScheduleResponse>>.Fail("Bạn không có quyền xem thông tin gia đình này.", 403);

            // Lấy toàn bộ lịch của các thành viên trong gia đình
            var schedules = await _unitOfWork.Repository<MedicationSchedules>()
                .FindAsync(s => s.Member.FamilyId == familyId, "Member");

            var response = schedules
                .OrderByDescending(s => s.IsActive)
                .ThenByDescending(s => s.StartDate)
                .Select(s => MapToResponse(s));

            return ApiResponse<IEnumerable<ScheduleResponse>>.Ok(response);
        }
        private ScheduleResponse MapToResponse(MedicationSchedules schedule)
        {
            return new ScheduleResponse
            {
                ScheduleId = schedule.ScheduleId,
                MemberId = schedule.MemberId,
                // Check null an toàn trong trường hợp không include bảng Member
                MemberName = schedule.Member?.FullName ?? "Unknown",
                MedicineName = schedule.MedicineName,
                Dosage = schedule.Dosage,
                SpecificTimes = schedule.SpecificTimes,
                StartDate = schedule.StartDate,
                EndDate = schedule.EndDate,
                IsActive = schedule.IsActive
            };
        }

        private ReminderDailyResponse MapToReminderDailyResponse(MedicationReminders r)
        {
            return new ReminderDailyResponse
            {
                ReminderId = r.ReminderId,
                ScheduleId = r.ScheduleId,                  // Giờ sẽ không bị 0000000 nữa
                MemberId = r.Schedule.MemberId,             // Giờ sẽ không bị 0000000 nữa
                MemberName = r.Schedule.Member?.FullName,   // Lấy tên bệnh nhân
                MedicineName = r.Schedule.MedicineName,
                Dosage = r.Schedule.Dosage,                 // Lấy liều lượng (viên, ml...)
                ReminderTime = r.ReminderTime,
                EndTime = r.EndTime,
                Status = r.Status
            };
        }
    }
}
