using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Share.Common;

namespace MediMate.Controllers
{
    [ApiController]
    [Route("api/v1/drug-interactions")]
    [Authorize]
    public class DrugInteractionController : ControllerBase
    {
        private readonly IDrugInteractionAIService _aiService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public DrugInteractionController(
            IDrugInteractionAIService aiService,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _aiService = aiService;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Giải thích tương tác thuốc bằng AI (RAG từ DrugBank + Chẩn đoán bệnh nhân).
        /// Yêu cầu gói đăng ký có bật HealthAlertEnabled.
        /// </summary>
        [HttpPost("explain")]
        public async Task<IActionResult> Explain([FromBody] DrugInteractionExplainRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewDrugName) || !request.Conflicts.Any())
                return BadRequest(new { success = false, message = "Thiếu thông tin tương tác thuốc." });

            // ── Kiểm tra gói đăng ký có bật cảnh báo thuốc không ──────────────
            var userId = _currentUserService.UserId;
            var member = await _unitOfWork.Repository<Members>()
                .GetQueryable().AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == userId && m.FamilyId != null);

            if (member != null)
            {
                var hasAccess = await _unitOfWork.Repository<FamilySubscriptions>()
                    .GetQueryable()
                    .Include(fs => fs.Package)
                    .AsNoTracking()
                    .AnyAsync(fs =>
                        fs.FamilyId == member.FamilyId &&
                        fs.Status == "Active" &&
                        fs.Package.HealthAlertEnabled);

                if (!hasAccess)
                    return StatusCode(403, ApiResponse<object>.Fail(
                        "Tính năng cảnh báo tương tác thuốc chỉ dành cho gói Premium trở lên. " +
                        "Vui lòng nâng cấp gói để sử dụng tính năng này.", 403));
            }
            // ─────────────────────────────────────────────────────────────

            var result = await _aiService.ExplainInteractionAsync(request);
            return Ok(new
            {
                success = true,
                code = 200,
                data = result
            });
        }
    }
}
