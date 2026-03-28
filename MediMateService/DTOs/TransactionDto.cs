using System;
using System.ComponentModel.DataAnnotations;

namespace MediMateService.DTOs
{
    public class TransactionFilterDto
    {
        public string? SearchTerm { get; set; }
        public string? Type { get; set; } 
        public string? Status { get; set; }
        public string? SortBy { get; set; } 
        public bool IsDescending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class TransactionItemDto
    {
        public Guid TransactionId { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class TransactionDetailDto
    {
        public Guid TransactionId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = "MediMate";
        public string TransactionType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal TransactionFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public string PaymentCode { get; set; } = string.Empty;
        public DateTime? AppointmentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
    }
    public class UpdateTransactionStatusRequest
    {
        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        public string Status { get; set; } = string.Empty;
    }
}
