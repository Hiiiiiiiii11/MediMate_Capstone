using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IFirebaseNotificationService
    {
        Task<bool> SendNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string> data = null);
    }
}
