namespace MediMate.Models.Consultations
{
    public class ConsultationSessionResponse
    {
        public Guid SessionId { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? DoctorNotes { get; set; }
    }
}
