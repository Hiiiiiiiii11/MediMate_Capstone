namespace MediMateService.DTOs
{
    public class PayoutItemDto
    {
        public Guid PayoutId { get; set; }
        public Guid? ClinicId { get; set; }
        public string ClinicName { get; set; } = string.Empty;
        public Guid? AppointmentId { get; set; }
        public Guid? ConsultationId { get; set; }

        public decimal Amount { get; set; }
        // Hold, ReadyToPay, Paid, Cancelled
        public string Status { get; set; } = string.Empty;
        public DateTime CalculatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? TransferImageUrl { get; set; }
    }

    public class PayoutSummaryDto
    {
        public Guid ClinicId { get; set; }
        public string ClinicName { get; set; } = string.Empty;
        public decimal TotalPendingAmount { get; set; }     // Tổng công nợ ReadyToPay
        public int PendingPayoutCount { get; set; }
        public decimal TotalPaidAmount { get; set; }        // Tổng đã thanh toán
    }

    public class ProcessPayoutDto
    {
        // URL ảnh chuyển khoản (bằng chứng thanh toán)
        public string? TransferImageUrl { get; set; }
        // Ghi chú của admin
        public string? Note { get; set; }
    }

    public class PayoutFilterDto
    {
        public Guid? ClinicId { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
