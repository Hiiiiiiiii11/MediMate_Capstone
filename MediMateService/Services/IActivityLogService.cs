using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IActivityLogService
    {
        // Hàm này dùng nội bộ ở Backend để ghi log (không trả ra API)
        // Dùng object cho oldData và newData để tự động ép kiểu sang JSON
        Task LogActivityAsync(Guid familyId, Guid memberId, string actionType, string entityName, Guid entityId, string description, object? oldData = null, object? newData = null);

        // Hàm này dùng cho Controller gọi ra API để hiển thị Lịch sử hoạt động của Gia đình
        Task<ApiResponse<IEnumerable<ActivityLogResponse>>> GetFamilyActivitiesAsync(Guid familyId, Guid currentUserId, int page = 1, int pageSize = 20);
      
    }
}

