using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;

[Route("api/v1/upload")]
[ApiController]
[Authorize] // Vẫn cần đăng nhập mới được upload
public class UploadController : ControllerBase
{
    private readonly IUploadPhotoService _uploadPhotoService;

    public UploadController(IUploadPhotoService uploadPhotoService)
    {
        _uploadPhotoService = uploadPhotoService;
    }

    // POST: api/v1/upload/prescription-scan
    // API này dùng cho bước 1: Chụp ảnh -> Lấy link
    [HttpPost("prescription-scan")]
    public async Task<IActionResult> UploadForScan(IFormFile file)
    {
        try
        {
            var result = await _uploadPhotoService.UploadPhotoAsync(file);
            // Trả về URL để FE gọi AI hoặc hiển thị
            return Ok(new ApiResponse<FileUploadResult>
            {
                Success = true,
                Data = result,
                Message = "Upload thành công."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string> { Success = false, Message = ex.Message });
        }
    }
}