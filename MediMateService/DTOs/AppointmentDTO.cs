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

    public class AppointmentDto
    {
        public Guid AppointmentId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public Guid AvailabilityId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
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
}
