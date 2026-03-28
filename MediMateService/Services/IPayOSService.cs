using MediMateService.DTOs;
using Share.Common;

namespace MediMateService.Services;

public interface IPayOSService
{
    Task<PaymentLinkResponse> CreatePaymentLinkAsync(Guid userId, CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentStatusResponse?> GetPaymentInfoAsync(int orderCode, CancellationToken cancellationToken = default);
    Task<bool> ProcessPaymentWebhookAsync(int orderCode, bool isSuccess, CancellationToken cancellationToken = default);
    Task<bool> VerifyWebhookSignatureAsync(string signature, string data, CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<PaymentItemDto>>> GetAllPaymentsAsync(PaymentFilterDto filter);
    Task<ApiResponse<PagedResult<PaymentItemDto>>> GetPaymentsByUserIdAsync(Guid userId, PaymentFilterDto filter);
    Task<ApiResponse<bool>> UpdatePaymentStatusAsync(int orderCode, string status, CancellationToken cancellationToken = default);
}
