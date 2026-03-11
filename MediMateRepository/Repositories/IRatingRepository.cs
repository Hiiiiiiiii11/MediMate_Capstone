using MediMateRepository.Model;

namespace MediMateRepository.Repositories
{
    public interface IRatingRepository
    {
        Task<ConsultationSessions?> GetSessionByIdAsync(Guid sessionId);
        Task<Ratings?> GetRatingBySessionIdAsync(Guid sessionId);
        Task<List<Ratings>> GetRatingsByDoctorIdAsync(Guid doctorId);
        Task AddRatingAsync(Ratings rating);
    }
}
