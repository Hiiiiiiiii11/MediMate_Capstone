using System.ComponentModel.DataAnnotations;

namespace MediMate.Models.Clinics
{
    public class CreateClinicRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public IFormFile LicenseFile { get; set; }

        public IFormFile? LogoFile { get; set; }
        public string Email { get; set; } = string.Empty;

        // ── Thông tin ngân hàng — bắt buộc để nhận payout từ Admin ──────────
        [Required]
        public string BankName { get; set; } = string.Empty;

        [Required]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required]
        public string BankAccountHolder { get; set; } = string.Empty;
    }

    public class UpdateClinicRequest
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public IFormFile? LicenseFile { get; set; }
        public IFormFile? LogoFile { get; set; }
        public bool? IsActive { get; set; }
        public string Email { get; set; } = string.Empty;

        // Banking — tùy chọn khi cập nhật
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountHolder { get; set; }
    }

    public class AddDoctorToClinicRequest
    {
        [Required]
        public Guid DoctorId { get; set; }
        public string? Specialty { get; set; }
        [Required]
        public decimal ConsultationFee { get; set; }
    }

    public class UpdateClinicDoctorRequest
    {
        public string? Specialty { get; set; }
        public decimal? ConsultationFee { get; set; }
        public string? Status { get; set; }
    }

    public class CreateClinicContractRequest
    {
        [Required]
        public IFormFile? ContractFile { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Note { get; set; }
    }

    public class UpdateClinicContractStatusRequest
    {
        [Required]
        [RegularExpression("(?i)^(Active|Expired|Terminated)$",
            ErrorMessage = "Trạng thái không hợp lệ. Chỉ chấp nhận: Active, Expired, Terminated.")]
        public string Status { get; set; } = string.Empty;
    }

    public class ProcessPayoutRequest
    {
        public IFormFile? TransferImage { get; set; }
        public IFormFile? ReportFile { get; set; }
        public string? Note { get; set; }
    }
}
