using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Constants;
using System;
using System.Threading.Tasks;

namespace MediMate.Controllers
{
    [Route("api/v1/rag-embeddings")]
    [ApiController]
    //[Authorize(Roles = $"{Roles.Admin},{Roles.Owner}")]
    [Authorize]
    public class RagBaseEmbeddingController : ControllerBase
    {
        private readonly IRagBaseEmbeddingService _embeddingService;

        public RagBaseEmbeddingController(IRagBaseEmbeddingService embeddingService)
        {
            _embeddingService = embeddingService;
        }

        // Thường AI Service/Tool ngoài (Python) sẽ gọi API này để insert vector vào DB
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRagBaseEmbeddingRequest request)
        {
            try
            {
                var response = await _embeddingService.CreateAsync(request);
                if (!response.Success) return StatusCode(response.Code, response);
                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("documents/{documentId}")]
        public async Task<IActionResult> GetByDocumentId(Guid documentId)
        {
            try
            {
                var response = await _embeddingService.GetByDocumentIdAsync(documentId);
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var response = await _embeddingService.GetByIdAsync(id);
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRagBaseEmbeddingRequest request)
        {
            try
            {
                var response = await _embeddingService.UpdateAsync(id, request);
                if (!response.Success) return StatusCode(response.Code, response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var response = await _embeddingService.DeleteAsync(id);
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