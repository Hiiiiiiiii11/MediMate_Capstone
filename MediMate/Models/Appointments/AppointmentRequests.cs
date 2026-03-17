using System.ComponentModel.DataAnnotations;

namespace MediMate.Models.Appointments
{
    public class CreateAppointmentRequest
    {
        [Required]
        public Guid DoctorId { get; set; }

        [Required]
        public Guid MemberId { get; set; }

        [Required]
        public Guid AvailabilityId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        // THÊM MỚI: Nhận giờ khám (Ví dụ: 8, 9, 14, 15)
        [Required]
        public TimeSpan AppointmentTime { get; set; }
    }

    public class CancelAppointmentRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
    public class UpdateAppointmentRequest
    {
        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [RegularExpression("(?i)^(Approved|Rejected|Completed|Cancelled)$",
            ErrorMessage = "Trạng thái không hợp lệ. Chỉ chấp nhận: Approved, Rejected, Completed, Cancelled.")]
        public string Status { get; set; } = string.Empty;
    }
}
