namespace MediMateRepository.Model
{
    public class DoctorAvailabilityExceptions
    {
        public Guid ExceptionId { get; set; } = Guid.NewGuid();
        public Guid DoctorId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool IsAvailableOverride { get; set; } = false;
        public string Status { get; set; } = "Pending";
        public virtual Doctors Doctor { get; set; } = null!;
    }
}
