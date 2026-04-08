using MediMateRepository.Data;
using MediMateRepository.Model;
using Microsoft.EntityFrameworkCore;

namespace MediMateRepository.Repositories.Implementations
{
    public class RatingRepository : IRatingRepository
    {
        private readonly MediMateDbContext _context;

        public RatingRepository(MediMateDbContext context)
        {
            _context = context;
        }

        public async Task<ConsultationSessions?> GetSessionByIdAsync(Guid sessionId)
        {
            return await _context.ConsultationSessions
                .FirstOrDefaultAsync(s => s.ConsultanSessionId == sessionId);
        }

        public async Task<Ratings?> GetRatingBySessionIdAsync(Guid sessionId)
        {
            return await _context.Ratings
                .FirstOrDefaultAsync(r => r.ConsultanSessionId == sessionId);
        }

        public async Task<Ratings?> GetRatingByIdAsync(Guid ratingId)
        {
            return await _context.Ratings
                .FirstOrDefaultAsync(r => r.RatingId == ratingId);
        }

        public async Task<List<Ratings>> GetRatingsByDoctorIdAsync(Guid doctorId)
        {
            return await _context.Ratings
                .Where(r => r.DoctorId == doctorId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task AddRatingAsync(Ratings rating)
        {
            await _context.Ratings.AddAsync(rating);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRatingAsync(Ratings rating)
        {
            _context.Ratings.Update(rating);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRatingAsync(Ratings rating)
        {
            _context.Ratings.Remove(rating);
            await _context.SaveChangesAsync();
        }
    }
}
