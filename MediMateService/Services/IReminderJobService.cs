using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IReminderJobService
    {
        Task CheckAndNotifyOverdueReminder(Guid reminderId);
        Task NotifyUpcomingAppointmentAsync(Guid appointmentId);
    }
}
