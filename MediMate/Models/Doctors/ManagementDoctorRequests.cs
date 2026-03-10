using Microsoft.AspNetCore.Http;

namespace MediMate.Models.Doctors
{
    public class CreateDoctorRequest
    {
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class CreateDoctorManagerRequest
    {
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class UpdateDoctorRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string CurrentHospitalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public IFormFile? LicenseImage { get; set; }
        public int YearsOfExperience { get; set; }
        public string Bio { get; set; } = string.Empty;
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
        public bool IsActive { get; set; }
    }

    public class RejectDoctorRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class SubmitDoctorRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string CurrentHospitalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public IFormFile? LicenseImage { get; set; }
        public int YearsOfExperience { get; set; }
        public string Bio { get; set; } = string.Empty;
    }
}
