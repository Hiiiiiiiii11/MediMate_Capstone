using MediMateService.DTOs;
using MediMateService.Services;
using MediMate.Models.Ratings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using System.Security.Claims;

namespace MediMate.Controllers
{
    [Route("api/v1/ratings")]
    [ApiController]
    [Authorize]
    public class RatingsController : ControllerBase
    {
        private readonly IRatingService _ratingService;
        private readonly ICurrentUserService _currentUserService;

        public RatingsController(IRatingService ratingService, ICurrentUserService currentUserService)
        {
            _ratingService = ratingService;
            _currentUserService = currentUserService;
        }

        [HttpPost("session/{sessionId}")]
        public async Task<IActionResult> CreateRating(Guid sessionId, [FromBody] CreateRatingRequest request)
        {
            // Dùng trực tiếp _currentUserService.UserId
            var result = await _ratingService.CreateRatingAsync(
                _currentUserService.UserId,
                sessionId,
                new CreateRatingDto
                {
                    Score = request.Score,
                    Comment = request.Comment
                });

            return Ok(ApiResponse<RatingResponse>.Ok(MapToResponse(result), "Đánh giá thành công."));
        }


        [HttpGet("{ratingId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRating(Guid ratingId)
        {
            try
            {
                var result = await _ratingService.GetRatingByIdAsync(ratingId);
                if (result == null) return NotFound(ApiResponse<string>.Fail("Không tìm thấy đánh giá.", 404));

                return Ok(ApiResponse<RatingResponse>.Ok(MapToResponse(result)));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("session/{sessionId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRatingBySession(Guid sessionId)
        {
            try
            {
                var result = await _ratingService.GetRatingBySessionAsync(sessionId);
                if (result == null) return NotFound(ApiResponse<string>.Fail("Phiên khám chưa được đánh giá.", 404));

                return Ok(ApiResponse<RatingResponse>.Ok(MapToResponse(result)));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("doctor/{doctorId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDoctorReviews(Guid doctorId)
        {
            try
            {
                var result = await _ratingService.GetDoctorReviewsAsync(doctorId);
                var response = result.Select(r => new DoctorReviewResponse
                {
                    RatingId = r.RatingId,
                    SessionId = r.SessionId,
                    MemberId = r.MemberId,
                    Score = r.Score,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList();

                return Ok(ApiResponse<List<DoctorReviewResponse>>.Ok(response, "Lấy danh sách đánh giá thành công."));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
        [HttpPut("{ratingId}")]
        public async Task<IActionResult> UpdateRating(Guid ratingId, [FromBody] CreateRatingRequest request)
        {
            // Không cần truyền isDependent nữa vì Service sẽ dùng hàm CheckAccess
            var result = await _ratingService.UpdateRatingAsync(
                _currentUserService.UserId,
                ratingId,
                new CreateRatingDto
                {
                    Score = request.Score,
                    Comment = request.Comment
                });

            return Ok(ApiResponse<RatingResponse>.Ok(MapToResponse(result), "Cập nhật thành công."));
        }

        [HttpDelete("{ratingId}")]
        public async Task<IActionResult> DeleteRating(Guid ratingId)
        {
            await _ratingService.DeleteRatingAsync(_currentUserService.UserId, ratingId);
            return Ok(ApiResponse<bool>.Ok(true, "Xóa đánh giá thành công."));
        }


        private static RatingResponse MapToResponse(RatingDto dto)
        {
            return new RatingResponse
            {
                RatingId = dto.RatingId,
                SessionId = dto.SessionId,
                DoctorId = dto.DoctorId,
                MemberId = dto.MemberId,
                Score = dto.Score,
                Comment = dto.Comment,
                CreatedAt = dto.CreatedAt
            };
        }

        private IActionResult HandleException(Exception ex)
        {
            var code = ex switch
            {
                UnauthorizedAccessException => 401,
                ArgumentException => 400,
                InvalidOperationException => 400,
                _ => 500
            };

            // Custom exceptions from Service layer (BadRequestException, NotFoundException, etc.)
            // usually have their own status codes if handled by middleware.
            // Here we map them roughly if local handling is needed.
            
            if (ex.Message.Contains("Không tìm thấy") || ex.GetType().Name == "NotFoundException") return NotFound(ApiResponse<string>.Fail(ex.Message, 404));
            if (ex.GetType().Name == "BadRequestException") return BadRequest(ApiResponse<string>.Fail(ex.Message, 400));
            if (ex.GetType().Name == "ForbiddenException") return StatusCode(403, ApiResponse<string>.Fail(ex.Message, 403));
            if (ex.GetType().Name == "ConflictException") return Conflict(ApiResponse<string>.Fail(ex.Message, 409));

            return StatusCode(code, ApiResponse<string>.Fail(ex.Message, code));
        }
    }
}
