using Microsoft.AspNetCore.Http;

namespace MediMateService.DTOs
{
    // ─── Clinic ───────────────────────────────────────────────
    public class CreateClinicDto
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public IFormFile LicenseFile { get; set; } 
        public IFormFile? LogoFile { get; set; } 

        // Banking: bắt buộc khi tạo phòng khám — dùng để nhận payout từ Admin
        [System.ComponentModel.DataAnnotations.Required]
        public string BankName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        public string BankAccountNumber { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        public string BankAccountHolder { get; set; } = string.Empty;
    }

    public class UpdateClinicDto
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public IFormFile? LicenseFile { get; set; }
        public IFormFile? LogoFile { get; set; }
        public bool? IsActive { get; set; }

        // Banking — tùy chọn khi cập nhật
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountHolder { get; set; }
    }

    public class ClinicDto
    {
        public Guid ClinicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string LicenseUrl { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int DoctorCount { get; set; }

        // Banking info
        public string BankName { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;
        public string BankAccountHolder { get; set; } = string.Empty;
    }

    // ─── ClinicDoctor ─────────────────────────────────────────
    public class AddDoctorToClinicDto
    {
        public Guid DoctorId { get; set; }
        public string? Specialty { get; set; }
        public decimal ConsultationFee { get; set; }
    }

    public class UpdateClinicDoctorDto
    {
        public string? Specialty { get; set; }
        public decimal? ConsultationFee { get; set; }
        public string? Status { get; set; }
    }

    public class ClinicDoctorDto
    {
        public Guid Id { get; set; }
        public Guid ClinicId { get; set; }
        public string ClinicName { get; set; } = string.Empty;
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string? DoctorAvatar { get; set; }
        public string? Specialty { get; set; }
        public decimal ConsultationFee { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
    }

    // ─── ClinicContract ───────────────────────────────────────
    public class CreateClinicContractDto
    {
        public Guid ClinicId { get; set; }
        public IFormFile? ContractFile { get; set; } 
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Note { get; set; }
    }

    public class ClinicContractDto
    {
        public Guid ContractId { get; set; }
        public Guid ClinicId { get; set; }
        public string ClinicName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
