using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface INotificationSettingService
    {
        Task<ApiResponse<NotificationSettingResponse>> GetSettingByFamilyIdAsync(Guid familyId, Guid currentUserId);
        Task<ApiResponse<NotificationSettingResponse>> UpdateSettingAsync(Guid familyId, Guid currentUserId, UpdateNotificationSettingRequest request);
    }
}
