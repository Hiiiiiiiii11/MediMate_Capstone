using System;
using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class ClinicDoctors
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid ClinicId { get; set; }
        public virtual Clinics Clinic { get; set; } = null!;
        
        public Guid DoctorId { get; set; }
        public virtual Doctors Doctor { get; set; } = null!;
        
        public string? Specialty { get; set; }
        
        // Giá khám tại phòng khám này
        public decimal ConsultationFee { get; set; }
        
        public string Status { get; set; } = "Active"; // Active, Inactive
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
