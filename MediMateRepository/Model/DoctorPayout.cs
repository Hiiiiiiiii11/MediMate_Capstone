using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class DoctorPayout
    {
        [Key]
        public Guid PayoutId { get; set; } = Guid.NewGuid();
        public Guid? ConsultationId { get; set; }
        public virtual ConsultationSessions? ConsultationSession { get; set; }

        public Guid? ClinicId { get; set; }
        public virtual Clinics? Clinic { get; set; }

        public Guid? AppointmentId { get; set; }
        public virtual Appointments? Appointment { get; set; }

        public decimal Amount { get; set; }
        
        // Trạng thái: Hold (Tạm giữ), ReadyToPay (Chờ thanh toán), Paid (Đã trả), Cancelled (Huỷ)
        public string Status { get; set; } = "Hold";
        
        public DateTime CalculatedAt { get; set; } = DateTime.Now;
        public DateTime? PaidAt { get; set; }
        
        public string? TransferImageUrl { get; set; }
        public string? ReportFileUrl { get; set; }
    }
}
