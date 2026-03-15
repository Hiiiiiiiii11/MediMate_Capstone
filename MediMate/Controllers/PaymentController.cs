using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> PaymentWebhook([FromBody] JsonElement webhookData, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Received PayOS Webhook");

                // Get signature from header
                if (!Request.Headers.TryGetValue("x-api-validate-signature", out var signature))
                {
                    _logger.LogWarning("Webhook missing signature header");
                    return BadRequest("Missing signature");
                }

                var dataStr = webhookData.GetRawText();
                var isValid = await _payOSService.VerifyWebhookSignatureAsync(signature.ToString(), dataStr, cancellationToken);
                
                if (!isValid)
                {
                    _logger.LogWarning("Invalid webhook signature");
                    return BadRequest("Invalid signature");
                }

                // Extract orderCode from data
                if (webhookData.TryGetProperty("data", out var dataElement) && 
                    dataElement.TryGetProperty("orderCode", out var orderCodeElement))
                {
                    int orderCode = orderCodeElement.GetInt32();
                    var success = await _payOSService.ProcessPaymentWebhookAsync(orderCode, cancellationToken);
                    
                    if (success)
                    {
                        return Ok(new { message = "Webhook processed successfully" });
                    }
                }

                return BadRequest(new { message = "Failed to process webhook or invalid data" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
