namespace MediMateService.DTOs
{
    public class ConsultationSessionDto
    {
        public Guid ConsultanSessionId { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? DoctorNote { get; set; }
    }

    public class EndConsultationDto
    {
        public DateTime? EndedAt { get; set; }
    }

    public class AttachPrescriptionDto
    {
        public Guid PrescriptionId { get; set; }
    }
}
