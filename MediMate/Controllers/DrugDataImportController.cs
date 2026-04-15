using System.Threading.Tasks;
using MediMateService.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediMate.Controllers
{
    [ApiController]
    [Route("api/v1/drugs")]
    public class DrugDataImportController : ControllerBase
    {
        private readonly IDrugDataService _drugDataService;

        public DrugDataImportController(IDrugDataService drugDataService)
        {
            _drugDataService = drugDataService;
        }

        // POST api/v1/drugs/import
        [HttpPost("import")]
        public async Task<IActionResult> ImportDrugs([FromBody] ImportRequest request)
        {
            var result = await _drugDataService.ImportDrugsFromXmlAsync(request.FilePath);
            return StatusCode(result.Code, result);
        }
    }

    public class ImportRequest
    {
        public string FilePath { get; set; }
    }
}
