using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using System.Security.Claims; // NHỚ THÊM DÒNG NÀY ĐỂ ĐỌC TOKEN

namespace MediMateApi.Controllers
{
    [Route("api/v1/upload")]
    [ApiController]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly IUploadPhotoService _uploadPhotoService;
        private readonly IOcrService _ocrService;
        private readonly ICurrentUserService _currentUserService;

        public UploadController(IUploadPhotoService uploadPhotoService, IOcrService ocrService, ICurrentUserService currentUserService)
        {
            _uploadPhotoService = uploadPhotoService;
            _ocrService = ocrService;
            _currentUserService = currentUserService;
        }

        [HttpPost("prescription-scan")]
        public async Task<IActionResult> UploadForScan(IFormFile file, [FromQuery] Guid? memberId) // <--- Đổi thành Guid?
        {
            try
            {
                var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("Role") ?? "User"; 
                Guid finalMemberId;
                Guid? callerUserId = null;
                Guid? callerMemberId = null;

                if (callerRole == "Dependent")
                {
                    // Trẻ em log bằng QR: Ép lấy MemberId từ Token, bỏ qua params
                    var memberIdClaim = User.FindFirstValue("MemberId");
                    if (!Guid.TryParse(memberIdClaim, out finalMemberId))
                        return Unauthorized(ApiResponse<string>.Fail("Token không hợp lệ.", 401));
                        
                    callerMemberId = finalMemberId;
                }
                else
                {
                    // Chủ hộ: Bắt buộc phải truyền ID hồ sơ muốn lưu thuốc
                    if (!memberId.HasValue || memberId.Value == Guid.Empty)
                        return BadRequest(ApiResponse<string>.Fail("Vui lòng truyền memberId để xác định hồ sơ cần lưu đơn thuốc.", 400));
                        
                    finalMemberId = memberId.Value;
                    callerUserId = _currentUserService.UserId;
                }

                var result = await _ocrService.ScanPrescriptionAsync(file, finalMemberId, callerRole, callerUserId, callerMemberId);
                return Ok(ApiResponse<OcrScanResponse>.Ok(result, "Quét đơn thuốc thành công."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message, 400));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<string>.Fail(ex.Message, 403));
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("QUOTA_EXHAUSTED:"))
            {
                var message = ex.Message["QUOTA_EXHAUSTED:".Length..];
                return StatusCode(402, ApiResponse<string>.Fail(message, 402));
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
}