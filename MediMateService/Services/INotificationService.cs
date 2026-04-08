using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface INotificationService
    {
        Task<ApiResponse<bool>> SendNotificationAsync(Guid? userId, string title, string message, string type, Guid? referenceId = null, Guid? memberId = null);

        Task<ApiResponse<IEnumerable<NotificationDto>>> GetUserNotificationsAsync(Guid? userId = null, Guid? memberId = null);

        // 2. Đánh dấu 1 thông báo cụ thể là "Đã đọc"
        Task<ApiResponse<bool>> MarkAsReadAsync(Guid notificationId, Guid? userId = null, Guid? memberId = null);

        // 3. Đánh dấu TẤT CẢ thông báo của User/Member là "Đã đọc"
        Task<ApiResponse<bool>> MarkAllAsReadAsync(Guid? userId = null, Guid? memberId = null);
    }
}
