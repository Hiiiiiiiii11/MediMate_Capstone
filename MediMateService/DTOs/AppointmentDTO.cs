using System.ComponentModel.DataAnnotations;

namespace MediMateService.DTOs
{
    public class CreateAppointmentDto
    {
        public Guid DoctorId { get; set; }

        public Guid MemberId { get; set; }

        public Guid AvailabilityId { get; set; }

        public DateTime AppointmentDate { get; set; }

        public TimeSpan AppointmentTime { get; set; }
    }

    public class CancelAppointmentDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class AppointmentPaymentResponseDto
    {
        public AppointmentDto Appointment { get; set; } = null!;
        public string CheckoutUrl { get; set; } = string.Empty;
        public long OrderCode { get; set; }
    }

    public class AppointmentDto
    {
        public Guid AppointmentId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid? ClinicId { get; set; }   // Nullable vì Appointments.ClinicId là Guid?
        public Guid MemberId { get; set; }
        public string? MemberName { get; set; }
        public Guid AvailabilityId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? CancelReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }


    public class AvailableSlotDto
    {
        // Thêm trường này để Frontend lấy được ID truyền vào API Create Appointment
        public Guid AvailabilityId { get; set; }

        public TimeSpan Time { get; set; }
        public string DisplayTime { get; set; } = string.Empty;
        public bool IsBooked { get; set; }
    }
    public class UpdateAppointmentDto
    {
        public string Status { get; set; } = string.Empty; // "Approved", "Rejected", "Completed"
    }

    public class AppointmentDetailDto
    {
        public Guid AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CancelReason { get; set; }
        public DateTime CreatedAt { get; set; }

        // Thông tin Bác sĩ
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string? DoctorAvatar { get; set; }
        public string? Specialty { get; set; }

        // Thông tin Bệnh nhân (Member)
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string? MemberAvatar { get; set; }
        public string? MemberGender { get; set; }
        public DateTime? MemberDateOfBirth { get; set; }
    }
}
