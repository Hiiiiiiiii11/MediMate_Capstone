using Microsoft.AspNetCore.Http;

namespace MediMateService.DTOs
{
    public class PayoutItemDto
    {
        public Guid PayoutId { get; set; }
        public Guid? ClinicId { get; set; }
        public string ClinicName { get; set; } = string.Empty;
        public Guid? AppointmentId { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public TimeSpan? AppointmentTime { get; set; }
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }

        public string? PaymentStatus { get; set; }
        public string? PayerName { get; set; }
        public string? PayerPhoneNumber { get; set; }
        public string? PayerBankName { get; set; }
        public string? PayerBankAccountNumber { get; set; }
        public string? PayerBankAccountHolder { get; set; }
        public Guid? ConsultationId { get; set; }

        public decimal Amount { get; set; }
        // Hold, ReadyToPay, Paid, Cancelled
        public string Status { get; set; } = string.Empty;
        public DateTime CalculatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? TransferImageUrl { get; set; }
        public string? ReportFileUrl { get; set; }
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
        public IFormFile? TransferImage { get; set; }
        public IFormFile? ReportFile { get; set; }
        public string? Note { get; set; }
    }

    public class PayoutFilterDto
    {
        public Guid? ClinicId { get; set; }
        public Guid? DoctorId { get; set; }   // Lọc theo bác sĩ (qua Appointment.DoctorId hoặc ConsultationSession.DoctorId)
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
