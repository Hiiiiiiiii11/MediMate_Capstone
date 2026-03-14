using MediMateService.DTOs;
using MediMateService.Services;
using MediMateService.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Constants;

namespace MediMate.Controllers
{
    [ApiController]
    [Route("api/cloudinary")]
    public class CloudinaryUploadController : Controller
    {
        private readonly ICloudinaryUploadService _cloudinaryUploadService;
        private readonly ICurrentUserService _currentUserService;
        public CloudinaryUploadController(ICloudinaryUploadService cloudinaryUploadService, ICurrentUserService currentUserService)
        {
            _cloudinaryUploadService = cloudinaryUploadService;
            _currentUserService = currentUserService;
        }
        [HttpGet("images")]
        //[Authorize(Roles = $"{Roles.Admin},{Roles.DoctorManager}")] 
        [Authorize]
        public async Task<IActionResult> GetImagesPaginated([FromQuery] PrescriptionImageFilter filter)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _cloudinaryUploadService.GetPrescriptionImagesPaginatedAsync(filter, userId);
                return StatusCode(result.Code, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [HttpGet("document")]
        //[Authorize(Roles = $"{Roles.Admin},{Roles.DoctorManager}")]
        [Authorize]
        public async Task<IActionResult> GetDocumentsPaginated([FromQuery] DoctorDocumentFilter filter)
        {
            try
            {
                var response = await _cloudinaryUploadService.GetDocumentsWithPaginationAsync(filter);
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
