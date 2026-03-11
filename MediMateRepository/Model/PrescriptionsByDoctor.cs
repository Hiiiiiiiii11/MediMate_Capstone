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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual Doctors Doctor { get; set; }
        public virtual Members Member { get; set; }



        public virtual ConsultationSessions Session { get; set; } = null!;
    }
}
