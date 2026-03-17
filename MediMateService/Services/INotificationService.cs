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
    }
}
