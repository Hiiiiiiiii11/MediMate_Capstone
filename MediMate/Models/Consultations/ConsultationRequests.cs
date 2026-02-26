namespace MediMate.Models.Consultations
{
    public class EndConsultationRequest
    {
        public DateTime? EndedAt { get; set; }
    }

    public class AttachPrescriptionRequest
    {
        public Guid PrescriptionId { get; set; }
    }
}
