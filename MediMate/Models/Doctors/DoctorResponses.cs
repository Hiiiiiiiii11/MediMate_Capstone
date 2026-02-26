namespace MediMate.Models.Doctors
{
    public class DoctorResponse
    {
        public Guid DoctorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string CurrentHospitalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public string Bio { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid UserId { get; set; }
    }

    public class DoctorAvailabilityResponse
    {
        public Guid DoctorAvailabilityId { get; set; }
        public Guid DoctorId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public bool IsBooked { get; set; }
    }

    public class DoctorAvailabilityExceptionResponse
    {
        public Guid ExceptionId { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime Date { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool IsAvailableOverride { get; set; }
    }
}
