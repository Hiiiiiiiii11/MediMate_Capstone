using MediMateService.DTOs;

namespace MediMateService.Services;

public interface IPayOSService
{
    Task<PaymentLinkResponse> CreatePaymentLinkAsync(Guid userId, CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentStatusResponse?> GetPaymentInfoAsync(int orderCode, CancellationToken cancellationToken = default);
    Task<bool> ProcessPaymentWebhookAsync(int orderCode, CancellationToken cancellationToken = default);
    Task<bool> VerifyWebhookSignatureAsync(string signature, string data, CancellationToken cancellationToken = default);
}
