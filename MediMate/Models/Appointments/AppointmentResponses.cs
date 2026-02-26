namespace MediMate.Models.Appointments
{
    public class AppointmentResponse
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
