using MediMate.Models.Ratings;
using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using Share.Constants;

namespace MediMate.Controllers
{
    [Route("api/v1/consultations")]
    [ApiController]
    public class ConsultationController : ControllerBase
    {
        private readonly IRatingService _ratingService;

        public ConsultationController(IRatingService ratingService)
        {
            _ratingService = ratingService;
        }

        [HttpPost("{sessionId}/rating")]
        [Authorize(Roles = Roles.User)]
        public async Task<IActionResult> CreateRating(Guid sessionId, [FromBody] CreateRatingRequest request)
        {
            var data = await _ratingService.CreateRatingAsync(sessionId, new CreateRatingDto
            {
                Score = request.Score,
                Comment = request.Comment
            });

            return Ok(ApiResponse<RatingResponse>.Ok(MapRatingResponse(data), "Đánh giá phiên khám thành công."));
        }

        private static RatingResponse MapRatingResponse(RatingDto dto)
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
    }
}
