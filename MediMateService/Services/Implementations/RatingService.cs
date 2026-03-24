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
        private readonly IUnitOfWork _unitOfWork;

        public RatingService(IRatingRepository ratingRepository, IDoctorRepository doctorRepository, IUnitOfWork unitOfWork)
        {
            _ratingRepository = ratingRepository;
            _doctorRepository = doctorRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<RatingDto> CreateRatingAsync(Guid callerId, bool isDependent, Guid sessionId, CreateRatingDto request)
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

            // Dependent: callerId chính là MemberId → so sánh thẳng
            // User thường: callerId là UserId → tìm Member và so sánh member.UserId
            bool hasAccess = isDependent
                ? session.MemberId == callerId
                : (await _unitOfWork.Repository<Members>().GetByIdAsync(session.MemberId))?.UserId == callerId;

            if (!hasAccess)
            {
                throw new ForbiddenException("Bạn không có quyền đánh giá phiên khám này.");
            }

            if (!string.Equals(session.Status, "Ended", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(session.Status, "Completed", StringComparison.OrdinalIgnoreCase))
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
                ConsultanSessionId = session.ConsultanSessionId,
                DoctorId = session.DoctorId,
                MemberId = session.MemberId,
                Score = request.Score,
                Comment = request.Comment?.Trim() ?? string.Empty,
                CreatedAt = DateTime.Now
            };

            await _ratingRepository.AddRatingAsync(rating);
            await UpdateDoctorAverageRatingAsync(session.DoctorId);

            return MapToRatingDto(rating);
        }

        public async Task<RatingDto?> GetRatingByIdAsync(Guid ratingId)
        {
            var rating = await _ratingRepository.GetRatingByIdAsync(ratingId);
            return rating != null ? MapToRatingDto(rating) : null;
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

        public async Task<RatingDto> UpdateRatingAsync(Guid callerId, bool isDependent, Guid ratingId, CreateRatingDto request)
        {
            if (request.Score is < 1 or > 5)
            {
                throw new BadRequestException("Score phải trong khoảng từ 1 đến 5.");
            }

            var rating = await _ratingRepository.GetRatingByIdAsync(ratingId);
            if (rating == null)
            {
                throw new NotFoundException("Không tìm thấy đánh giá.");
            }

            bool hasAccess = isDependent
                ? rating.MemberId == callerId
                : (await _unitOfWork.Repository<Members>().GetByIdAsync(rating.MemberId))?.UserId == callerId;

            if (!hasAccess)
            {
                throw new ForbiddenException("Bạn không có quyền chỉnh sửa đánh giá này.");
            }

            rating.Score = request.Score;
            rating.Comment = request.Comment?.Trim() ?? string.Empty;

            await _ratingRepository.UpdateRatingAsync(rating);
            await UpdateDoctorAverageRatingAsync(rating.DoctorId);

            return MapToRatingDto(rating);
        }

        public async Task DeleteRatingAsync(Guid callerId, bool isDependent, Guid ratingId)
        {
            var rating = await _ratingRepository.GetRatingByIdAsync(ratingId);
            if (rating == null)
            {
                throw new NotFoundException("Không tìm thấy đánh giá.");
            }

            bool hasAccess = isDependent
                ? rating.MemberId == callerId
                : (await _unitOfWork.Repository<Members>().GetByIdAsync(rating.MemberId))?.UserId == callerId;

            if (!hasAccess)
            {
                throw new ForbiddenException("Bạn không có quyền xóa đánh giá này.");
            }

            var doctorId = rating.DoctorId;
            await _ratingRepository.DeleteRatingAsync(rating);
            await UpdateDoctorAverageRatingAsync(doctorId);
        }

        private async Task UpdateDoctorAverageRatingAsync(Guid doctorId)
        {
            var doctor = await _doctorRepository.GetDoctorByIdAsync(doctorId);
            if (doctor != null)
            {
                var doctorRatings = await _ratingRepository.GetRatingsByDoctorIdAsync(doctorId);
                doctor.AverageRating = doctorRatings.Count == 0 ? 0 : (float)Math.Round(doctorRatings.Average(r => r.Score), 1);
                await _doctorRepository.UpdateDoctorAsync(doctor);
            }
        }

        private static RatingDto MapToRatingDto(Ratings rating)
        {
            return new RatingDto
            {
                RatingId = rating.RatingId,
                SessionId = rating.ConsultanSessionId,
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
                SessionId = rating.ConsultanSessionId,
                MemberId = rating.MemberId,
                Score = rating.Score,
                Comment = rating.Comment,
                CreatedAt = rating.CreatedAt
            };
        }
    }
}
