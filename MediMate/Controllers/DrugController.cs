using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediMate.Controllers
{
    [Route("api/v1/drugs")]
    [ApiController]
    [Authorize] // Can be accessed by Doctors
    public class DrugController : ControllerBase
    {
        private readonly IDrugDataService _drugDataService;

        public DrugController(IDrugDataService drugDataService)
        {
            _drugDataService = drugDataService;
        }

      
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<DrugDto>>), 200)]
        public async Task<IActionResult> SearchDrugs([FromQuery] string query, [FromQuery] int limit = 10)
        {
            var result = await _drugDataService.SearchDrugsAsync(query ?? string.Empty, limit);
            if (!result.Success)
            {
                return StatusCode(result.Code, result);
            }
            return Ok(result);
        }
    }
}
