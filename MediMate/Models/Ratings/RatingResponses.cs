namespace MediMate.Models.Ratings
{
    public class RatingResponse
    {
        public Guid RatingId { get; set; }
        public Guid SessionId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public string? MemberName { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DoctorReviewResponse
    {
        public Guid RatingId { get; set; }
        public Guid SessionId { get; set; }
        public Guid MemberId { get; set; }
        public string? MemberName { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
