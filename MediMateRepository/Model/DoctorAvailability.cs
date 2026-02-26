namespace MediMateRepository.Model
{
    public class DoctorAvailability
    {
        public Guid DoctorAvailabilityId { get; set; } = Guid.NewGuid();
        public Guid DoctorId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsBooked { get; set; } = false;
        public virtual Doctors Doctor { get; set; } = null!;
    }
}
