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

    public static class ConsultationSessionConstants
    {
        /// <summary>Session vừa được tạo (T-5 phút), chờ 2 bên join</summary>
        public const string PROCESSING = "Processing";
        /// <summary>Cả 2 bên đã join, đang trong cuộc gọi</summary>
        public const string IN_PROGRESS = "InProgress";
        /// <summary>Phiên đã kết thúc (user end hoặc timeout)</summary>
        public const string ENDED = "Ended";
    }

    public static class AppointmentActionTypes
    {
        public const string NEW_APPOINTMENT = "NEW_APPOINTMENT";
        public const string APPOINTMENT_UPDATED = "APPOINTMENT_UPDATE";
        public const string APPOINTMENT_CANCELLED = "APPOINTMENT_CANCELLED";
    }

    public static class ConsultationSessionActionTypes
    {
        public const string SESSION_STARTED = "SESSION_STARTED";
        public const string SESSION_IN_PROGRESS = "SESSION_IN_PROGRESS";
        public const string SESSION_ENDED = "SESSION_ENDED";
        public const string SESSION_TIMEOUT = "SESSION_TIMEOUT";
        public const string GUARDIAN_SESSION_INVITE = "GUARDIAN_SESSION_INVITE";
    }
}
