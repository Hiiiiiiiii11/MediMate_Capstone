using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Share.Constants;
using Microsoft.EntityFrameworkCore;

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

        public async Task<RatingDto> CreateRatingAsync(Guid callerId, Guid sessionId, CreateRatingDto request)
        {
            // 1. Kiểm tra điểm số hợp lệ
            if (request.Score is < 1 or > 5)
            {
                throw new BadRequestException("Điểm đánh giá phải trong khoảng từ 1 đến 5 sao.");
            }

            // 2. Lấy thông tin phiên khám
            var session = await _ratingRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                throw new NotFoundException("Không tìm thấy phiên tư vấn.");
            }

            // 3. KIỂM TRA QUYỀN TRUY CẬP (Hỗ trợ cả User và Member)
            // Lấy thông tin chi tiết của Member trong Session để biết ai là chủ sở hữu (UserId)
            var patientMember = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(session.MemberId);

            if (patientMember == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin bệnh nhân trong phiên khám.");
            }

            // Quyền đánh giá hợp lệ khi:
            // - TH1: callerId chính là MemberId của bệnh nhân (Bệnh nhân dùng mã QR/Profile riêng để đánh giá)
            // - TH2: callerId là UserId của người quản lý hồ sơ đó (Chủ hộ đánh giá hộ người thân)
            bool hasAccess = session.MemberId == callerId || patientMember.UserId == callerId;

            if (!hasAccess)
            {
                throw new ForbiddenException("Bạn không có quyền đánh giá phiên khám này.");
            }

            // 4. Kiểm tra trạng thái phiên khám
            if (!string.Equals(session.Status, ConsultationSessionConstants.ENDED, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(session.Status, AppointmentConstants.COMPLETED, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("Chỉ được đánh giá khi phiên khám đã hoàn tất.");
            }

            // 5. Kiểm tra xem đã đánh giá chưa (Tránh spam)
            var existingRating = await _ratingRepository.GetRatingBySessionIdAsync(sessionId);
            if (existingRating != null)
            {
                throw new ConflictException("Phiên khám này đã được đánh giá trước đó.");
            }

            // 6. Kiểm tra bác sĩ còn tồn tại không
            var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);
            if (doctor == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin bác sĩ để đánh giá.");
            }

            // 7. Tạo đánh giá mới
            var rating = new MediMateRepository.Model.Ratings
            {
                RatingId = Guid.NewGuid(),
                ConsultanSessionId = session.ConsultanSessionId,
                DoctorId = session.DoctorId,
                MemberId = session.MemberId, // Luôn gắn với bệnh nhân được khám
                Score = request.Score,
                Comment = request.Comment?.Trim() ?? string.Empty,
                ImageUrl = request.ImageUrl,
                CreatedAt = DateTime.Now
            };

            await _ratingRepository.AddRatingAsync(rating);

            // 8. Cập nhật lại điểm trung bình của bác sĩ (Async Background)
            await UpdateDoctorAverageRatingAsync(session.DoctorId);

            return MapToRatingDto(rating);
        }

        public async Task<RatingDto?> GetRatingByIdAsync(Guid ratingId)
        {
            var rating = await _ratingRepository.GetRatingByIdAsync(ratingId);
            return rating != null ? MapToRatingDto(rating) : null;
        }

        public async Task<RatingDto?> GetRatingBySessionAsync(Guid sessionId)
        {
            var rating = await _ratingRepository.GetRatingBySessionIdAsync(sessionId);
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

        public async Task<RatingDto> UpdateRatingAsync(Guid callerId, Guid ratingId, CreateRatingDto request)
        {
            // 1. Kiểm tra điểm số hợp lệ
            if (request.Score < 1 || request.Score > 5)
            {
                throw new BadRequestException("Điểm đánh giá phải nằm trong khoảng [1, 5].");
            }

            // 2. Tìm bản đánh giá
            var rating = await _ratingRepository.GetRatingByIdAsync(ratingId);
            if (rating == null)
            {
                throw new NotFoundException("Không tìm thấy đánh giá.");
            }

            // 3. KIỂM TRA QUYỀN TRUY CẬP (Truy vấn DB trực tiếp)
            var member = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(rating.MemberId);

            // Quyền: callerId là bệnh nhân (MemberId) HOẶC callerId là chủ sở hữu (UserId) của bệnh nhân đó
            bool hasAccess = rating.MemberId == callerId || (member != null && member.UserId == callerId);

            if (!hasAccess)
            {
                throw new ForbiddenException("Bạn không có quyền chỉnh sửa đánh giá của người khác.");
            }

            // 4. Cập nhật dữ liệu
            rating.Score = request.Score;
            rating.Comment = request.Comment?.Trim() ?? string.Empty;
            if (request.ImageUrl != null)
            {
                rating.ImageUrl = request.ImageUrl;
            }

            await _ratingRepository.UpdateRatingAsync(rating);

            // 5. Đồng bộ lại điểm trung bình bác sĩ
            await UpdateDoctorAverageRatingAsync(rating.DoctorId);

            return MapToRatingDto(rating);
        }

        public async Task DeleteRatingAsync(Guid callerId, Guid ratingId)
        {
            // 1. Tìm bản đánh giá
            var rating = await _ratingRepository.GetRatingByIdAsync(ratingId);
            if (rating == null)
            {
                throw new NotFoundException("Không tìm thấy đánh giá.");
            }

            // 2. KIỂM TRA QUYỀN TRUY CẬP
            var member = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(rating.MemberId);

            bool hasAccess = rating.MemberId == callerId || (member != null && member.UserId == callerId);

            if (!hasAccess)
            {
                throw new ForbiddenException("Bạn không có quyền xóa đánh giá này.");
            }

            // 3. Thực hiện xóa và cập nhật điểm bác sĩ
            var doctorId = rating.DoctorId;
            await _ratingRepository.DeleteRatingAsync(rating);
            await UpdateDoctorAverageRatingAsync(doctorId);
        }

        public async Task<PagedResult<RatingDto>> GetRatingsAsync(RatingFilter filter)
        {
            filter ??= new RatingFilter();
            if (filter.PageNumber <= 0) filter.PageNumber = 1;
            if (filter.PageSize <= 0) filter.PageSize = 10;

            var query = _unitOfWork.Repository<MediMateRepository.Model.Ratings>().GetQueryable()
                .Include(r => r.Member)
                .Include(r => r.Doctor)
                .AsQueryable();

            if (filter.DoctorId.HasValue)
                query = query.Where(r => r.DoctorId == filter.DoctorId.Value);

            if (filter.MemberId.HasValue)
                query = query.Where(r => r.MemberId == filter.MemberId.Value);

            if (filter.Score.HasValue)
                query = query.Where(r => r.Score == filter.Score.Value);

            if (filter.MinScore.HasValue)
                query = query.Where(r => r.Score >= filter.MinScore.Value);

            if (filter.MaxScore.HasValue)
                query = query.Where(r => r.Score <= filter.MaxScore.Value);

            var totalCount = query.Count();

            query = (filter.SortBy ?? string.Empty).ToLower() switch
            {
                "score" => filter.IsDescending ? query.OrderByDescending(r => r.Score) : query.OrderBy(r => r.Score),
                _ => filter.IsDescending ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt)
            };

            var items = query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var result = new PagedResult<RatingDto>
            {
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                Items = items.Select(MapToRatingDto).ToList()
            };

            return await Task.FromResult(result);
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
                DoctorName = rating.Doctor?.FullName,
                MemberId = rating.MemberId,
                MemberName = rating.Member?.FullName,
                Score = rating.Score,
                Comment = rating.Comment,
                ImageUrl = rating.ImageUrl,
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
                MemberName = rating.Member?.FullName,
                Score = rating.Score,
                Comment = rating.Comment,
                ImageUrl = rating.ImageUrl,
                CreatedAt = rating.CreatedAt
            };
        }
    }
}
