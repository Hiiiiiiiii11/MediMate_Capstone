namespace MediMate.Models.Appointments
{
    public class CreateAppointmentRequest
    {
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public Guid AvailabilityId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public bool IsPremiumUser { get; set; }
    }

    public class CancelAppointmentRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
