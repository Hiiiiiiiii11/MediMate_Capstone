using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IMedicationSchedulesService
    {

        Task<ApiResponse<ScheduleResponse>> CreateScheduleAsync(Guid memberId, Guid currentUserId, CreateScheduleRequest request);
        Task<ApiResponse<IEnumerable<ReminderDailyResponse>>> GetDailyRemindersAsync(Guid memberId, Guid currentUserId, DateTime date);
        Task<ApiResponse<bool>> MarkReminderActionAsync(Guid reminderId, Guid currentUserId, MedicationActionRequest request);
        Task<ApiResponse<bool>> SnoozeReminderAsync(Guid reminderId, Guid currentUserId, int delayMinutes);
        Task<ApiResponse<IEnumerable<ReminderDailyResponse>>> GetFamilyDailyRemindersAsync(Guid familyId, Guid currentUserId, DateTime date);
        Task<ApiResponse<ScheduleDetailResponse>> GetScheduleByIdAsync(Guid scheduleId, Guid currentUserId);

        // 2. Cập nhật Schedule
        Task<ApiResponse<ScheduleResponse>> UpdateScheduleAsync(Guid scheduleId, Guid currentUserId, UpdateScheduleRequest request);
        Task<ApiResponse<ScheduleDetailItemResponse>> UpdateScheduleDetailAsync(Guid detailId, Guid currentUserId, UpdateScheduleDetailRequest request);

        // 3. Xóa Schedule
        Task<ApiResponse<bool>> DeleteScheduleAsync(Guid scheduleId, Guid currentUserId);
        Task<ApiResponse<IEnumerable<ScheduleResponse>>> GetMemberSchedulesAsync(Guid memberId, Guid currentUserId);
        Task<ApiResponse<IEnumerable<ScheduleResponse>>> GetFamilySchedulesAsync(Guid familyId, Guid currentUserId);
        Task<ApiResponse<List<ScheduleResponse>>> CreateBulkSchedulesAsync(Guid memberId, Guid currentUserId, CreateBulkScheduleRequest request);

    }
}
