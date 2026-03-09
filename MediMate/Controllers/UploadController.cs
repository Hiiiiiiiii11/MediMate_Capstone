using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;

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
}