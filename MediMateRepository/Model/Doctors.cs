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
        public int YearsOfExperience { get; set; }
        public string Bio { get; set; } = string.Empty;
        public double AverageRating { get; set; } = 0.0;
        public string Status { get; set; } = DoctorStatuses.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
    }
}
