using MediMateService.DTOs;

namespace MediMateService.Services
{
    public interface IRatingService
    {
        Task<RatingDto> CreateRatingAsync(Guid userId, Guid sessionId, CreateRatingDto request);
        Task<List<DoctorReviewDto>> GetDoctorReviewsAsync(Guid doctorId);
    }
}
