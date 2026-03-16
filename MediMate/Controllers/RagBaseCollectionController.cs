using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Constants;
using System;
using System.Threading.Tasks;

namespace MediMate.Controllers
{
    [Route("api/v1/rag-collections")]
    [ApiController]
    // RAG System là phần lõi, chỉ nên cho Admin/Owner đụng vào
    //[Authorize(Roles = $"{Roles.Admin},{Roles.Owner}")]
    [Authorize]
    public class RagBaseCollectionController : ControllerBase
    {
        private readonly IRagBaseCollectionService _collectionService;

        public RagBaseCollectionController(IRagBaseCollectionService collectionService)
        {
            _collectionService = collectionService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRagBaseCollectionRequest request)
        {
            try
            {
                var response = await _collectionService.CreateAsync(request);
                if (!response.Success) return StatusCode(response.Code, response);
                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var response = await _collectionService.GetAllAsync();
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
                var response = await _collectionService.GetByIdAsync(id);
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRagBaseCollectionRequest request)
        {
            try
            {
                var response = await _collectionService.UpdateAsync(id, request);
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
                var response = await _collectionService.DeleteAsync(id);
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