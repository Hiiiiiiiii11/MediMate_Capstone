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
        public TimeSpan AppointmentTime { get; set; }
        
        // Trạng thái cuộc gọi (Pending, Confirmed, InProgress, Completed, Cancelled)
        public string Status { get; set; } = "Pending";
        
        // Trạng thái thanh toán: Pending, Paid, Refunded, Cancelled
        public string PaymentStatus { get; set; } = "Pending";
        
        // Ghi nhận lịch hẹn này thuộc về phòng khám nào
        public Guid? ClinicId { get; set; }
        public virtual Clinics? Clinic { get; set; }
        
        // Thời gian checkin checkout thực tế
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }

        public string? CancelReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public virtual Doctors Doctor { get; set; } = null!;
        public virtual Members Member { get; set; } = null!;
        public virtual DoctorAvailability Availability { get; set; } = null!;
        public virtual ICollection<Payments> Payments { get; set; } = new List<Payments>();
        public virtual ICollection<DoctorPayout> DoctorPayouts { get; set; } = new List<DoctorPayout>();
    }
}
