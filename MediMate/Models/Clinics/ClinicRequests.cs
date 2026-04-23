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
        public string License { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
    }

    public class UpdateClinicRequest
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? License { get; set; }
        public string? LogoUrl { get; set; }
        public bool? IsActive { get; set; }
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
        public string FileUrl { get; set; } = string.Empty;
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
        public string? TransferImageUrl { get; set; }
        public string? Note { get; set; }
    }
}
