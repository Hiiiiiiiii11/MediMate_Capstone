namespace MediMateService.DTOs
{
    public class CreateAppointmentDto
    {
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public Guid AvailabilityId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public bool IsPremiumUser { get; set; }
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
}
