using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using Share.Constants;
using System;
using System.Threading.Tasks;

namespace MediMate.Controllers
{
    [Route("api/v1/rag-config")]
    [ApiController]

    //[Authorize(Roles = $"{Roles.Admin},{Roles.Owner}")]
    [Authorize]
    public class RagBaseConfigController : ControllerBase
    {
        private readonly IRagBaseConfigService _configService;

        public RagBaseConfigController(IRagBaseConfigService configService)
        {
            _configService = configService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<RagBaseConfigDto>), 201)]
        public async Task<IActionResult> CreateConfig([FromBody] CreateRagBaseConfigRequest request)
        {
            try
            {
                var response = await _configService.CreateConfigAsync(request);
                if (!response.Success) return StatusCode(response.Code, response);
                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<RagBaseConfigDto>), 200)]
        public async Task<IActionResult> GetConfig()
        {
            try
            {
                var response = await _configService.GetConfigAsync();
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<RagBaseConfigDto>), 200)]
        public async Task<IActionResult> UpdateConfig([FromBody] UpdateRagBaseConfigRequest request)
        {
            try
            {
                var response = await _configService.UpdateConfigAsync(request);
                if (!response.Success) return StatusCode(response.Code, response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}

