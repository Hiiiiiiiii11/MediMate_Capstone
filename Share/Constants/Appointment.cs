using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Constants
{
    public static class AppointmentConstants
    {
        public const string PENDING = "Pending";
        public const string APPROVED = "Approved";
        public const string IN_PROGRESS = "InProgress";
        public const string REJECTED = "Rejected";
        public const string CANCELLED = "Cancelled";
        public const string COMPLETED = "Completed";
    }
    public static class AppointmentActionTypes
    {
        public const string NEW_APPOINTMENT = "NEW_APPOINTMENT";
        public const string APPOINTMENT_UPDATED = "APPOINTMENT_UPDATE";
        public const string APPOINTMENT_CANCELLED = "APPOINTMENT_CANCELLED";
    }
}
