using System.ComponentModel.DataAnnotations;

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
        public bool IsVerified { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
    }
}
