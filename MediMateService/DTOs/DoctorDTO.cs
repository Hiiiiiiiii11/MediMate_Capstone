namespace MediMateService.DTOs
{
    public class DoctorDto
    {
        public Guid DoctorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string CurrentHospitalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string? LicenseImage { get; set; }
        public int YearsOfExperience { get; set; }
        public string Bio { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public DateTime? LastSeenAt { get; set; }
        // IsOnline = computed: LastSeenAt > now - 2 phút
        public bool IsOnline => LastSeenAt.HasValue && LastSeenAt.Value > DateTime.UtcNow.AddMinutes(-2);
        public DateTime CreatedAt { get; set; }
        public Guid UserId { get; set; }
    }

    public class CreateDoctorDto
    {
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
    }

    // Doctor tự submit hồ sơ đầy đủ (Inactive → Pending)
    public class SubmitDoctorDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string CurrentHospitalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string? LicenseImage { get; set; }   // Cloudinary URL
        public int YearsOfExperience { get; set; }
        public string Bio { get; set; } = string.Empty;
    }

    public class UpdateDoctorDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string CurrentHospitalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string? LicenseImage { get; set; }
        public int YearsOfExperience { get; set; }
        public string Bio { get; set; } = string.Empty;
    }

    public class RejectDoctorDto
    {
        public string? Reason { get; set; }
    }

    // Kept for backward compat (used in old approve endpoint)
    public class ApproveDoctorDto
    {
        public string Action { get; set; } = "approve";
        public string? Reason { get; set; }
    }

    public class CreateDoctorAvailabilityDto
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }

    public class UpdateDoctorAvailabilityDto
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class DoctorAvailabilityDto
    {
        public Guid DoctorAvailabilityId { get; set; }
        public Guid DoctorId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class DoctorAvailabilityExceptionDto
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
