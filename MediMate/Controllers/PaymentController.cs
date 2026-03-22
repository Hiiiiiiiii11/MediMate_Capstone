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

             
                var isValid = await _payOSService.VerifyWebhookSignatureAsync(signature, dataStr, cancellationToken);
                if (!isValid)
                {
                    _logger.LogWarning("Invalid webhook signature");
                 
                }

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
                    var success = await _payOSService.ProcessPaymentWebhookAsync(orderCode, cancellationToken);
                    if (success)
                    {
                        return Ok(new { message = "Webhook processed successfully" });
                    }
                    else
                    {
                        _logger.LogWarning("Webhook processed but transaction not found or already completed");
                        return Ok(new { message = "Transaction not found, but webhook acknowledged" });
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
    }
}
