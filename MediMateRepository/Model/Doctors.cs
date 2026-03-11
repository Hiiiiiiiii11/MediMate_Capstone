using System.ComponentModel.DataAnnotations;
using Share.Constants;

namespace MediMateRepository.Model
{
    public class Doctors
    {
        [Key]
        public Guid DoctorId { get; set; } = Guid.NewGuid();
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string CurrentHospitalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string? LicenseImage { get; set; }           
        public int YearsOfExperience { get; set; }
        public string Bio { get; set; } = string.Empty;
        public double AverageRating { get; set; } = 0.0;
        public string Status { get; set; } = DoctorStatuses.Inactive;
        public string? RejectionReason { get; set; }       
        public DateTime? LastSeenAt { get; set; }       
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public virtual ICollection<DoctorAvailability> Availabilities { get; set; } = new List<DoctorAvailability>();
        public virtual ICollection<Appointments> Appointments { get; set; } = new List<Appointments>();
    }
}
