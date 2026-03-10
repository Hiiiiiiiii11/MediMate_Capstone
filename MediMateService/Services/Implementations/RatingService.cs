using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;

namespace MediMateService.Services.Implementations
{
    public class RatingService : IRatingService
    {
        private readonly IRatingRepository _ratingRepository;
        private readonly IDoctorRepository _doctorRepository;

        public RatingService(IRatingRepository ratingRepository, IDoctorRepository doctorRepository)
        {
            _ratingRepository = ratingRepository;
            _doctorRepository = doctorRepository;
        }

        public async Task<RatingDto> CreateRatingAsync(Guid userId, Guid sessionId, CreateRatingDto request)
        {
            if (request.Score is < 1 or > 5)
            {
                throw new BadRequestException("Score phải trong khoảng từ 1 đến 5.");
            }

            var session = await _ratingRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                throw new NotFoundException("Không tìm thấy phiên khám.");
            }

            //check if this user is the person who have this consultation session
            if (session.MemberId != userId)
            {
                throw new BadRequestException("Bạn không có quyền đánh giá phiên khám này.");
            }

            if (!string.Equals(session.Status, "Ended", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("Chỉ được đánh giá khi phiên khám đã hoàn tất.");
            }

            var existingRating = await _ratingRepository.GetRatingBySessionIdAsync(sessionId);
            if (existingRating != null)
            {
                throw new ConflictException("Phiên khám này đã được đánh giá.");
            }

            var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);
            if (doctor == null)
            {
                throw new NotFoundException("Không tìm thấy bác sĩ.");
            }

            var rating = new Ratings
            {
                RatingId = Guid.NewGuid(),
                SessionId = session.SessionId,
                DoctorId = session.DoctorId,
                MemberId = session.MemberId,
                Score = request.Score,
                Comment = request.Comment?.Trim() ?? string.Empty,
                CreatedAt = DateTime.Now
            };

            await _ratingRepository.AddRatingAsync(rating);

            var doctorRatings = await _ratingRepository.GetRatingsByDoctorIdAsync(session.DoctorId);
            doctor.AverageRating = doctorRatings.Count == 0 ? 0 : doctorRatings.Average(r => r.Score);
            await _doctorRepository.UpdateDoctorAsync(doctor);

            return MapToRatingDto(rating);
        }

        public async Task<List<DoctorReviewDto>> GetDoctorReviewsAsync(Guid doctorId)
        {
            var doctor = await _doctorRepository.GetDoctorByIdAsync(doctorId);
            if (doctor == null)
            {
                throw new NotFoundException("Không tìm thấy bác sĩ.");
            }

            var ratings = await _ratingRepository.GetRatingsByDoctorIdAsync(doctorId);
            return ratings.Select(MapToDoctorReviewDto).ToList();
        }

        private static RatingDto MapToRatingDto(Ratings rating)
        {
            return new RatingDto
            {
                RatingId = rating.RatingId,
                SessionId = rating.SessionId,
                DoctorId = rating.DoctorId,
                MemberId = rating.MemberId,
                Score = rating.Score,
                Comment = rating.Comment,
                CreatedAt = rating.CreatedAt
            };
        }

        private static DoctorReviewDto MapToDoctorReviewDto(Ratings rating)
        {
            return new DoctorReviewDto
            {
                RatingId = rating.RatingId,
                SessionId = rating.SessionId,
                MemberId = rating.MemberId,
                Score = rating.Score,
                Comment = rating.Comment,
                CreatedAt = rating.CreatedAt
            };
        }
    }
}
