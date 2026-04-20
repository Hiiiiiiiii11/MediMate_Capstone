using MediMate.Models.Doctors;
using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;

namespace MediMate.Controllers
{
    [Route("api/v1/doctor-contracts")]
    [ApiController]
    public class DoctorContractController : ControllerBase
    {
        private readonly IDoctorContractService _contractService;

        public DoctorContractController(IDoctorContractService contractService)
        {
            _contractService = contractService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<List<DoctorContractResponse>>), 200)]
        [Authorize]
        public async Task<IActionResult> Create([FromForm] CreateContractRequest request)
        {
            var result = await _contractService.CreateAsync(request);
            return StatusCode(result.Code, result);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<List<DoctorContractResponse>>), 200)]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, [FromForm] UpdateContractRequest request)
        {
            var result = await _contractService.UpdateAsync(id, request);
            return StatusCode(result.Code, result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<List<DoctorContractResponse>>), 200)]
        [Authorize]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await _contractService.GetByIdAsync(id);
            return StatusCode(result.Code, result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<DoctorContractResponse>>), 200)]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var result = await _contractService.GetAllAsync();
            return StatusCode(result.Code, result);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<List<DoctorContractResponse>>), 200)]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _contractService.DeleteAsync(id);
            return StatusCode(result.Code, result);
        }
    }
}
