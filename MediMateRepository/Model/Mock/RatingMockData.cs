namespace MediMateRepository.Model.Mock
{
    public static class RatingMockData
    {
        public static readonly List<ConsultationSessions> Sessions = new()
        {
            new()
            {
                SessionId = Guid.Parse("44444444-4444-4444-4444-444444444401"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                MemberId = Guid.Parse("55555555-5555-5555-5555-555555555501"),
                StartedAt = new DateTime(2026, 2, 10, 8, 0, 0, DateTimeKind.Utc),
                EndedAt = new DateTime(2026, 2, 10, 8, 30, 0, DateTimeKind.Utc),
                IsCompleted = true
            },
            new()
            {
                SessionId = Guid.Parse("44444444-4444-4444-4444-444444444402"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                MemberId = Guid.Parse("55555555-5555-5555-5555-555555555502"),
                StartedAt = new DateTime(2026, 2, 11, 9, 0, 0, DateTimeKind.Utc),
                EndedAt = new DateTime(2026, 2, 11, 9, 30, 0, DateTimeKind.Utc),
                IsCompleted = true
            },
            new()
            {
                SessionId = Guid.Parse("44444444-4444-4444-4444-444444444403"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                MemberId = Guid.Parse("55555555-5555-5555-5555-555555555503"),
                StartedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc),
                EndedAt = null,
                IsCompleted = false
            }
        };

        public static readonly List<Ratings> Ratings = new()
        {
            new()
            {
                RatingId = Guid.Parse("66666666-6666-6666-6666-666666666601"),
                SessionId = Guid.Parse("44444444-4444-4444-4444-444444444402"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                MemberId = Guid.Parse("55555555-5555-5555-5555-555555555502"),
                Score = 5,
                Comment = "Bac si tu van rat tan tam.",
                CreatedAt = new DateTime(2026, 2, 11, 10, 0, 0, DateTimeKind.Utc)
            }
        };
    }
}
