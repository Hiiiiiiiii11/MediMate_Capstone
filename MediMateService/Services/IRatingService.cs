using MediMateService.DTOs;

namespace MediMateService.Services
{
    public interface IRatingService
    {
        Task<RatingDto?> GetRatingByIdAsync(Guid ratingId);
        Task<RatingDto?> GetRatingBySessionAsync(Guid sessionId);
        Task<List<DoctorReviewDto>> GetDoctorReviewsAsync(Guid doctorId);
        Task<RatingDto> CreateRatingAsync(Guid callerId, Guid sessionId, CreateRatingDto request);
        Task<RatingDto> UpdateRatingAsync(Guid callerId, Guid ratingId, CreateRatingDto request);
        Task DeleteRatingAsync(Guid callerId, Guid ratingId);
    }
}
