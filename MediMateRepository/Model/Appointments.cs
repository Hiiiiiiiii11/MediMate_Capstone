using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class Appointments
    {
        [Key]
        public Guid AppointmentId { get; set; } = Guid.NewGuid();
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public Guid AvailabilityId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int AppointmentTime { get; set; }
        public string Status { get; set; } = "Pending";
        public string? CancelReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public virtual Doctors Doctor { get; set; } = null!;
        public virtual Members Member { get; set; } = null!;
        public virtual DoctorAvailability Availability { get; set; } = null!;
    }
}
