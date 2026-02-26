namespace MediMateRepository.Model.Mock
{
    public static class RatingMockData
    {
        public static readonly List<Appointments> Appointments = new()
        {
            new()
            {
                AppointmentId = Guid.Parse("77777777-7777-7777-7777-777777777701"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                MemberId = Guid.Parse("55555555-5555-5555-5555-555555555501"),
                AvailabilityId = Guid.Parse("22222222-2222-2222-2222-222222222201"),
                AppointmentDate = new DateTime(2026, 2, 10, 8, 0, 0, DateTimeKind.Utc),
                Status = "Completed",
                CreatedAt = new DateTime(2026, 2, 9, 14, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                AppointmentId = Guid.Parse("77777777-7777-7777-7777-777777777702"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                MemberId = Guid.Parse("55555555-5555-5555-5555-555555555502"),
                AvailabilityId = Guid.Parse("22222222-2222-2222-2222-222222222204"),
                AppointmentDate = new DateTime(2026, 2, 11, 9, 0, 0, DateTimeKind.Utc),
                Status = "Completed",
                CreatedAt = new DateTime(2026, 2, 10, 14, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                AppointmentId = Guid.Parse("77777777-7777-7777-7777-777777777703"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                MemberId = Guid.Parse("55555555-5555-5555-5555-555555555503"),
                AvailabilityId = Guid.Parse("22222222-2222-2222-2222-222222222202"),
                AppointmentDate = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc),
                Status = "Pending",
                CreatedAt = new DateTime(2026, 2, 11, 14, 0, 0, DateTimeKind.Utc)
            }
        };

        public static readonly List<ConsultationSessions> Sessions = new()
        {
            new()
            {
                SessionId = Guid.Parse("44444444-4444-4444-4444-444444444401"),
                AppointmentId = Guid.Parse("77777777-7777-7777-7777-777777777701"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                MemberId = Guid.Parse("55555555-5555-5555-5555-555555555501"),
                StartedAt = new DateTime(2026, 2, 10, 8, 0, 0, DateTimeKind.Utc),
                EndedAt = new DateTime(2026, 2, 10, 8, 30, 0, DateTimeKind.Utc),
                Status = "Ended",
                DoctorNotes = "Benh nhan on dinh sau tu van."
            },
            new()
            {
                SessionId = Guid.Parse("44444444-4444-4444-4444-444444444402"),
                AppointmentId = Guid.Parse("77777777-7777-7777-7777-777777777702"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                MemberId = Guid.Parse("55555555-5555-5555-5555-555555555502"),
                StartedAt = new DateTime(2026, 2, 11, 9, 0, 0, DateTimeKind.Utc),
                EndedAt = new DateTime(2026, 2, 11, 9, 30, 0, DateTimeKind.Utc),
                Status = "Ended",
                DoctorNotes = null
            },
            new()
            {
                SessionId = Guid.Parse("44444444-4444-4444-4444-444444444403"),
                AppointmentId = Guid.Parse("77777777-7777-7777-7777-777777777703"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                MemberId = Guid.Parse("55555555-5555-5555-5555-555555555503"),
                StartedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc),
                EndedAt = null,
                Status = "Active",
                DoctorNotes = null
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
