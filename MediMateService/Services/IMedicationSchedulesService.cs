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
        Task<ApiResponse<IEnumerable<ReminderDailyResponse>>> GetFamilyDailyRemindersAsync(Guid familyId, Guid currentUserId, DateTime date);

        // 2. Cập nhật Schedule
        Task<ApiResponse<ScheduleResponse>> UpdateScheduleAsync(Guid scheduleId, Guid currentUserId, UpdateScheduleRequest request);

        // 3. Xóa Schedule
        Task<ApiResponse<bool>> DeleteScheduleAsync(Guid scheduleId, Guid currentUserId);

    }
}
