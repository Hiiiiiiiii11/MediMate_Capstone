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
        Task<ApiResponse<bool>> SendNotificationAsync(Guid userId, string title, string message, string type, Guid? referenceId = null);

        Task<ApiResponse<IEnumerable<NotificationDto>>> GetUserNotificationsAsync(Guid userId);

        // 2. Đánh dấu 1 thông báo cụ thể là "Đã đọc"
        Task<ApiResponse<bool>> MarkAsReadAsync(Guid notificationId, Guid userId);

        // 3. Đánh dấu TẤT CẢ thông báo của User là "Đã đọc"
        Task<ApiResponse<bool>> MarkAllAsReadAsync(Guid userId);
    }
}
