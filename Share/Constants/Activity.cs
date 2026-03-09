using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Constants
{
    public static class ActivityActionTypes
    {
        public const string CREATE = "CREATE";
        public const string UPDATE = "UPDATE";
        public const string DELETE = "DELETE";
        public const string JOIN = "JOIN";
        public const string LEAVE = "LEAVE";
        public const string KICK = "KICK";
    }

    public static class ActivityEntityNames
    {
        public const string FAMILY = "Families";
        public const string MEMBER = "Members";
        public const string MEDICATION_SCHEDULE = "MedicationSchedules";
        public const string NOTIFICATION_SETTING = "NotificationSettings";
        public const string PRESCIPTION = "Prescriptions";
        public const string HEALTHPROFILE = "HealthProfiles";
        public const string HEALTHCONDITION = "HealthConditions";


    }
}
