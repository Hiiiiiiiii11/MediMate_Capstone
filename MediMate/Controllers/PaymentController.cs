using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using System.Text.Json;

namespace MediMate.Controllers
{
    [Route("api/v1/payment")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPayOSService _payOSService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPayOSService payOSService, ICurrentUserService currentUserService, ILogger<PaymentController> logger)
        {
            _payOSService = payOSService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpPost("create")]
        [Authorize]
        [ProducesResponseType(typeof(PaymentLinkResponse), 200)]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _payOSService.CreatePaymentLinkAsync(userId, request, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment link");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("info/{orderCode}")]
        [ProducesResponseType(typeof(PaymentStatusResponse), 200)]
        public async Task<IActionResult> GetPaymentInfo(int orderCode, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _payOSService.GetPaymentInfoAsync(orderCode, cancellationToken);
                if (result == null) return NotFound(new { message = "Payment not found" });
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment info");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("webhook")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> PaymentWebhook([FromBody] JsonElement webhookData, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Received PayOS Webhook");

                if (webhookData.ValueKind != JsonValueKind.Object)
                {
                    return Ok(new { message = "ACK" });
                }

                if (!webhookData.TryGetProperty("signature", out var signatureElement))
                {
                    _logger.LogWarning("Webhook missing signature in body");
                    return Ok(new { message = "ACK - Missing signature" });
                }

                string signature = signatureElement.GetString() ?? "";
                var dataStr = webhookData.GetRawText();

                // 1. KIỂM TRA BẢO MẬT (CHỮ KÝ)
                var isValid = await _payOSService.VerifyWebhookSignatureAsync(signature, dataStr, cancellationToken);
                if (!isValid)
                {
                    _logger.LogWarning("Invalid webhook signature");
                    // SỬA LỖI BẢO MẬT: Bắt buộc phải return ngay tại đây để chặn hacker
                    return Ok(new { success = false, message = "Invalid signature" });
                }

                // 2. LẤY TRẠNG THÁI GIAO DỊCH (THÀNH CÔNG / THẤT BẠI)
                // PayOS thường trả về code = "00" nếu khách hàng thanh toán thành công
                bool isSuccess = false;
                if (webhookData.TryGetProperty("code", out var codeElement))
                {
                    isSuccess = (codeElement.GetString() == "00");
                }
                else if (webhookData.TryGetProperty("success", out var successElement))
                {
                    isSuccess = successElement.GetBoolean();
                }

                // 3. XỬ LÝ DATA BÊN TRONG
                if (webhookData.TryGetProperty("data", out var dataElement) &&
                    dataElement.ValueKind == JsonValueKind.Object &&
                    dataElement.TryGetProperty("orderCode", out var orderCodeElement))
                {
                    int orderCode = orderCodeElement.GetInt32();
                    if (orderCode == 123)
                    {
                        _logger.LogInformation("PayOS test webhook successful!");
                        return Ok(new { message = "Test webhook processed successfully" });
                    }

                    // GỌI HÀM SERVICE KÈM THEO CỜ "isSuccess"
                    var success = await _payOSService.ProcessPaymentWebhookAsync(orderCode, isSuccess, cancellationToken);

                    if (success)
                    {
                        return Ok(new { message = "Webhook processed successfully" });
                    }
                    else
                    {
                        _logger.LogWarning("Webhook processed but transaction not found or error");
                        return Ok(new { message = "Transaction not found or error" });
                    }
                }

                return Ok(new { message = "ACK, no data processed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return Ok(new { message = "Error but ACK to save URL" });
            }
        }
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PaymentItemDto>>), 200)]
        public async Task<IActionResult> GetMyPayments([FromQuery] PaymentFilterDto filter)
        {
            try
            {
                var userId = _currentUserService.UserId;
                // Gọi tới PayOSService (vì bạn gộp logic PaymentService vào đây)
                var result = await _payOSService.GetPaymentsByUserIdAsync(userId, filter);

                if (result.Success) return Ok(result);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user payments");
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
        }

        [HttpGet]
        [Authorize] // Có thể thêm (Roles = "Admin") sau này
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PaymentItemDto>>), 200)]
        public async Task<IActionResult> GetAllPayments([FromQuery] PaymentFilterDto filter)
        {
            try
            {
                var result = await _payOSService.GetAllPaymentsAsync(filter);

                if (result.Success) return Ok(result);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all payments");
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
        }
        [HttpPut("status/{orderCode}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> UpdatePaymentStatus(int orderCode, [FromBody] UpdatePaymentStatusRequest request, CancellationToken cancellationToken)
        {
            try
            {
                // Validations
                // 1. Chuẩn hóa chuỗi (Xóa khoảng trắng thừa và viết hoa)
                var upperStatus = request.Status?.Trim().ToUpper() ?? "";

                // 2. Danh sách trạng thái chuẩn được phép nhận
                var allowedStatuses = new[] { "SUCCESS", "FAILED", "CANCELLED" };

                // 3. Bắt lỗi nếu gửi sai format
                if (!allowedStatuses.Contains(upperStatus))
                {
                    return BadRequest(ApiResponse<bool>.Fail(
                        $"Trạng thái '{request.Status}' không hợp lệ. Chỉ chấp nhận: SUCCESS, FAILED, CANCELED.", 400));
                }
                var result = await _payOSService.UpdatePaymentStatusAsync(orderCode, request.Status, cancellationToken);

                if (result.Success)
                    return Ok(result);

                return StatusCode(result.Code, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status");
                return StatusCode(500, ApiResponse<bool>.Fail(ex.Message, 500));
            }
        }

    }
}
