using System.ComponentModel.DataAnnotations;

namespace MediMateService.DTOs
{
    public class CreatePaymentRequest
    {
        public Guid PackageId { get; set; }
        public Guid FamilyId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public string BuyerPhone { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
    }

    public class PaymentLinkResponse
    {
        public string PaymentUrl { get; set; } = string.Empty;
        public int OrderCode { get; set; }
        public string? QrCode { get; set; }
        public string? Message { get; set; }
    }

    public class PaymentStatusResponse
    {
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? CreatedAt { get; set; }
        public string? PaidAt { get; set; }
        public string? TransactionId { get; set; }
    }

    public class PayOSApiResponse<T>
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string? Signature { get; set; }
    }

    public class PaymentLinkData
    {
        public string Bin { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public long OrderCode { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentLinkId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
    }

    public class PaymentInfoData
    {
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public int AmountPaid { get; set; }
        public int AmountRemaining { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string? PaidAt { get; set; }
        public string? TransactionId { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class PaymentFilterDto
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PaymentItemDto
    {
        public Guid PaymentId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;

        public long OrderCode { get; set; } // <--- ĐÃ THÊM ORDER CODE VÀO ĐÂY

        public decimal Amount { get; set; }
        public string PaymentContent { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UpdatePaymentStatusRequest
    {
        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        public string Status { get; set; } = string.Empty;
    }
}