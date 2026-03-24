using MediMateRepository.Model;

namespace MediMateRepository.Repositories
{
    public interface IRatingRepository
    {
        Task<ConsultationSessions?> GetSessionByIdAsync(Guid sessionId);
        Task<Ratings?> GetRatingBySessionIdAsync(Guid sessionId);
        Task<Ratings?> GetRatingByIdAsync(Guid ratingId);
        Task<List<Ratings>> GetRatingsByDoctorIdAsync(Guid doctorId);
        Task AddRatingAsync(Ratings rating);
        Task UpdateRatingAsync(Ratings rating);
        Task DeleteRatingAsync(Ratings rating);
    }
}
