using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MediMate.Models.Doctors
{
    public class CreateDoctorRequest
    {
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? CurrentHospitalName { get; set; }
    }

    public class CreateDoctorManagerRequest
    {
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class UpdateDoctorRequest
    {
        public string? FullName { get; set; }
        public string? Specialty { get; set; }
        public string? CurrentHospitalName { get; set; }
        public string? LicenseNumber { get; set; }
        public IFormFile? AvatarImage { get; set; }
        [MaxLength(3)]
        public List<IFormFile>? LicenseImage { get; set; }
        [Range(0, 80)]
        public int? YearsOfExperience { get; set; }
        public string? Bio { get; set; }
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
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Specialty { get; set; } = string.Empty;

        // Optional: bác sĩ thuộc Clinic, field này chỉ dùng như thông tin bổ sung
        public string? CurrentHospitalName { get; set; }

        [Required]
        public string LicenseNumber { get; set; } = string.Empty;

        public IFormFile? AvatarImage { get; set; }

        [MaxLength(3)]
        public List<IFormFile>? LicenseImage { get; set; }

        [Range(0, 80)]
        public int YearsOfExperience { get; set; }

        public string Bio { get; set; } = string.Empty;
    }
}
