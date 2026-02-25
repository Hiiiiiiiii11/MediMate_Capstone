using MediMateRepository.Model;
using MediMateRepository.Model.Mock;

namespace MediMateRepository.Repositories.Implementations
{
    public class MockRatingRepository : IMockRatingRepository
    {
        public Task<ConsultationSessions?> GetSessionByIdAsync(Guid sessionId)
        {
            var session = RatingMockData.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
            return Task.FromResult(session);
        }

        public Task<Ratings?> GetRatingBySessionIdAsync(Guid sessionId)
        {
            var rating = RatingMockData.Ratings.FirstOrDefault(r => r.SessionId == sessionId);
            return Task.FromResult(rating);
        }

        public Task<List<Ratings>> GetRatingsByDoctorIdAsync(Guid doctorId)
        {
            var ratings = RatingMockData.Ratings
                .Where(r => r.DoctorId == doctorId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
            return Task.FromResult(ratings);
        }

        public Task AddRatingAsync(Ratings rating)
        {
            RatingMockData.Ratings.Add(rating);
            return Task.CompletedTask;
        }
    }
}
