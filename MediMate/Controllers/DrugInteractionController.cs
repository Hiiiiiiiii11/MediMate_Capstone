using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediMate.Controllers
{
    [ApiController]
    [Route("api/v1/drug-interactions")]
    [Authorize]
    public class DrugInteractionController : ControllerBase
    {
        private readonly IDrugInteractionAIService _aiService;

        public DrugInteractionController(IDrugInteractionAIService aiService)
        {
            _aiService = aiService;
        }

        /// <summary>
        /// Giải thích tương tác thuốc bằng AI (RAG từ DrugBank + Chẩn đoán bệnh nhân)
        /// </summary>
        [HttpPost("explain")]
        public async Task<IActionResult> Explain([FromBody] DrugInteractionExplainRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewDrugName) || !request.Conflicts.Any())
                return BadRequest(new { success = false, message = "Thiếu thông tin tương tác thuốc." });

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
