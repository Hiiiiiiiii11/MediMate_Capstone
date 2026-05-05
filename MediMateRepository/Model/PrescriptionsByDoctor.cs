using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MediMateRepository.Model
{
    public class PrescriptionsByDoctor
    {
        [Key]
        public Guid DigitalPrescriptionId { get; set; } = Guid.NewGuid();
        public Guid ConsultanSessionId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public string? Diagnosis { get; set; }
        public string? Advice { get; set; }
        public string? MedicinesList { get; set; } // JSON
        
        public string Status { get; set; } = "Active";
        public bool IsLocked { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public virtual Doctors Doctor { get; set; }
        public virtual Members Member { get; set; }
        public virtual ConsultationSessions Session { get; set; } = null!;
    }
}
