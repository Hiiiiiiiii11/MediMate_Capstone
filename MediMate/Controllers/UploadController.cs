using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using Share.Constants;

[Route("api/v1/upload")]
[ApiController]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IUploadPhotoService _uploadPhotoService;
    private readonly IOcrService _ocrService;

    public UploadController(IUploadPhotoService uploadPhotoService, IOcrService ocrService)
    {
        _uploadPhotoService = uploadPhotoService;
        _ocrService = ocrService;
    }


    [HttpPost("prescription-scan")]
    public async Task<IActionResult> UploadForScan(IFormFile file)
    {
        try
        {
            var result = await _ocrService.ScanPrescriptionAsync(file);
            return Ok(ApiResponse<OcrScanResponse>.Ok(result, "Quét đơn thuốc thành công."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail($"Lỗi xử lý: {ex.Message}", 500));
        }
    }

    [HttpPost("document")]
    //[Authorize(Roles = Roles.Doctor)]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        try
        {
            var fileUrl = await _uploadPhotoService.UploadDocumentAsync(file);

            return Ok(new
            {
                Success = true,
                Code = 200,
                Message = "Upload tài liệu thành công.",
                Data = new { FileUrl = fileUrl }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = "Lỗi upload: " + ex.Message });
        }
    }
}