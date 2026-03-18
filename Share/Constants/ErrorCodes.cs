namespace Share.Constants
{
    public static class ErrorCodes
    {
        // Generic
        public const string UnknownError = "UNKNOWN_ERROR";

        // HTTP-level defaults (dùng khi không cần code chi tiết)
        public const string NotFound = "NOT_FOUND";
        public const string BadRequest = "BAD_REQUEST";
        public const string Conflict = "CONFLICT";
        public const string Forbidden = "FORBIDDEN";

        // Auth / User
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string EmailExists = "EMAIL_EXISTS";
        public const string PhoneExists = "PHONE_EXISTS";
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string AccountInactive = "ACCOUNT_INACTIVE";
        public const string AccountLocked = "ACCOUNT_LOCKED";

        // OTP
        public const string OtpInvalid = "OTP_INVALID";
        public const string OtpExpired = "OTP_EXPIRED";

        // QR login
        public const string QrInvalid = "QR_INVALID";
        public const string QrExpired = "QR_EXPIRED";

        // Doctor / Manager
        public const string DoctorAlreadyExists = "DOCTOR_ALREADY_EXISTS";
        public const string DoctorManagerAlreadyExists = "DOCTOR_MANAGER_ALREADY_EXISTS";

        // Membership package
        public const string MembershipPackageInUse = "MEMBERSHIP_PACKAGE_IN_USE";
    }
}

