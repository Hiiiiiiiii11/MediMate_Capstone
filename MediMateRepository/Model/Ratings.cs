using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class Ratings
    {
        [Key]
        public Guid RatingId { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
