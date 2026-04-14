using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediMate.Controllers
{
    [Route("api/v1/versions")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private readonly IVersionService _versionService;

        public VersionController(IVersionService versionService)
        {
            _versionService = versionService;
        }

        // API dành riêng cho Mobile App gọi kiểm tra (Không yêu cầu Token)
        [HttpGet("check")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<VersionDto>), 200)]
        public async Task<IActionResult> CheckLatestVersion([FromQuery] string platform)
        {
            var result = await _versionService.CheckLatestVersionAsync(platform);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }

        // ==============================================================
        // CÁC API DƯỚI ĐÂY DÀNH CHO ADMIN QUẢN LÝ (THÊM, SỬA, XÓA, XEM)
        // ==============================================================

        [HttpGet]
        [Authorize] // Có thể thêm [Authorize(Roles = "Admin")] nếu muốn khóa chặt
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VersionDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] string? platform)
        {
            var result = await _versionService.GetAllVersionsAsync(platform);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<VersionDto>), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _versionService.GetVersionByIdAsync(id);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<VersionDto>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateVersionDto request)
        {
            var result = await _versionService.CreateVersionAsync(request);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<VersionDto>), 200)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVersionDto request)
        {
            var result = await _versionService.UpdateVersionAsync(id, request);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _versionService.DeleteVersionAsync(id);
            if (!result.Success) return StatusCode(result.Code, result);
            return Ok(result);
        }
    }
}