using MediMateService.DTOs;

namespace MediMateService.Services
{
    public interface IRatingService
    {
        Task<RatingDto> CreateRatingAsync(Guid callerId, bool isDependent, Guid sessionId, CreateRatingDto request);
        Task<RatingDto?> GetRatingByIdAsync(Guid ratingId);
        Task<RatingDto?> GetRatingBySessionAsync(Guid sessionId);
        Task<List<DoctorReviewDto>> GetDoctorReviewsAsync(Guid doctorId);
        Task<RatingDto> UpdateRatingAsync(Guid callerId, bool isDependent, Guid ratingId, CreateRatingDto request);
        Task DeleteRatingAsync(Guid callerId, bool isDependent, Guid ratingId);
    }
}
