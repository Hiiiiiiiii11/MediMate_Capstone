namespace MediMateService.DTOs
{
    public class CreateRatingDto
    {
        public int Score { get; set; }
        public string Comment { get; set; } = string.Empty;
    }

    public class RatingDto
    {
        public Guid RatingId { get; set; }
        public Guid SessionId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class DoctorReviewDto
    {
        public Guid RatingId { get; set; }
        public Guid SessionId { get; set; }
        public Guid MemberId { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
