using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Constants;
using System;
using System.Threading.Tasks;

namespace MediMate.Controllers
{
    [Route("api/v1/rag-documents")]
    [ApiController]
    //[Authorize(Roles = $"{Roles.Admin},{Roles.Owner}")]
    [Authorize]
    public class RagBaseDocumentController : ControllerBase
    {
        private readonly IRagBaseDocumentService _documentService;

        public RagBaseDocumentController(IRagBaseDocumentService documentService)
        {
            _documentService = documentService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRagBaseDocumentRequest request)
        {
            try
            {
                var response = await _documentService.CreateAsync(request);
                if (!response.Success) return StatusCode(response.Code, response);
                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [HttpGet("getall")]
        public async Task<IActionResult> GetAllDocument()
        {
            try
            {
                var response = await _documentService.GetAllDocument();
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }


        [HttpGet("collections/{collectionId}")]
        public async Task<IActionResult> GetByCollectionId(Guid collectionId)
        {
            try
            {
                var response = await _documentService.GetByCollectionIdAsync(collectionId);
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
                var response = await _documentService.GetByIdAsync(id);
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRagBaseDocumentRequest request)
        {
            try
            {
                var response = await _documentService.UpdateAsync(id, request);
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
                var response = await _documentService.DeleteAsync(id);
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