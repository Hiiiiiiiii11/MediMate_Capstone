namespace MediMate.Models.Doctors
{
    public class CreateDoctorRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string CurrentHospitalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public string Bio { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }

    public class UpdateDoctorRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string CurrentHospitalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public string Bio { get; set; } = string.Empty;
    }

    public class ChangeDoctorStatusRequest
    {
        public bool IsActive { get; set; }
    }

    public class VerifyDoctorLicenseRequest
    {
        public bool IsVerified { get; set; } = true;
    }

    public class ApproveDoctorRequest
    {
        public string Action { get; set; } = "approve";
        public string? Reason { get; set; }
    }

    public class CreateDoctorAvailabilityRequest
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }

    public class UpdateDoctorAvailabilityRequest
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public bool IsBooked { get; set; }
    }
}
