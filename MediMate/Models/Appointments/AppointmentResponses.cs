namespace MediMate.Models.Appointments
{
    public class AppointmentResponse
    {
        public Guid AppointmentId { get; set; }
        public Guid DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorAvatar { get; set; }
        public Guid? ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public Guid MemberId { get; set; }

        public string? MemberName { get; set; }
        public Guid UserId { get; set; }
        public Guid AvailabilityId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? CancelReason { get; set; }
        public decimal? Amount { get; set; }
        public Guid? ConsultationSessionId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
