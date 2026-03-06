using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IMedicationLogService
    {
        // Ghi log (Đồng thời cập nhật Reminder)
        Task<ApiResponse<MedicationLogResponse>> LogMedicationActionAsync(LogMedicationRequest request, Guid currentUserId);

        // Lấy lịch sử uống thuốc của 1 thành viên (có lọc theo ngày)
        Task<ApiResponse<IEnumerable<MedicationLogResponse>>> GetMemberLogsAsync(Guid memberId, Guid currentUserId, DateTime? startDate, DateTime? endDate);

        // Lấy thống kê tuân thủ (VD: Uống được bao nhiêu % liệu trình)
        Task<ApiResponse<object>> GetAdherenceStatsAsync(Guid scheduleId, Guid currentUserId);
        // Lấy lịch sử uống thuốc của cả gia đình (có lọc theo ngày)
        Task<ApiResponse<IEnumerable<MedicationLogResponse>>> GetFamilyLogsAsync(Guid familyId, Guid currentUserId, DateTime? startDate, DateTime? endDate);
    }
}
