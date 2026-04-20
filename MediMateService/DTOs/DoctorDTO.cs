namespace MediMateService.DTOs
{
    public class DoctorDto
    {
        public Guid DoctorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string CurrentHospitalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string? LicenseImage { get; set; }
        public int YearsOfExperience { get; set; }
        public string Bio { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public bool IsOnline => LastSeenAt.HasValue && LastSeenAt.Value > DateTime.Now.AddMinutes(-2);
        public DateTime CreatedAt { get; set; }
        public Guid UserId { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class CreateDoctorDto
    {
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
    }

    public class SubmitDoctorDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string CurrentHospitalName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public string? LicenseImage { get; set; }
        public int YearsOfExperience { get; set; }
        public string Bio { get; set; } = string.Empty;
    }

    public class UpdateDoctorDto
    {
        public string? FullName { get; set; }
        public string? Specialty { get; set; }
        public string? CurrentHospitalName { get; set; }
        public string? LicenseNumber { get; set; }
        public string? LicenseImage { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class RejectDoctorDto
    {
        public string? Reason { get; set; }
    }



    public class DoctorBankAccountDto
    {
        public Guid BankAccountId { get; set; }
        public Guid DoctorId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateDoctorBankAccountRequest
    {
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
    }

    public class UpdateDoctorBankAccountRequest
    {
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
    }


    public class DoctorDocumentDto
    {
        public Guid DocumentId { get; set; }
        public Guid DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorSpecialty { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string? DocumentName { get; set; }
        public string? DocumentType { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Pending", "Approved", "Rejected"
        public string? RejectReason { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? FileMimeType { get; set; }
        public string? FileExtension { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ReviewBy { get; set; }
        public string? ReviewAt { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateDoctorDocumentRequest
    {
        public string FileUrl { get; set; } = string.Empty; // Gửi link từ API Upload ảnh/file
        public string Type { get; set; } = string.Empty;
    }

    public class UpdateDoctorDocumentRequest
    {
        public string FileUrl { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class ReviewDoctorDocumentRequest
    {
        public string Status { get; set; } = string.Empty; // Chỉ nhận "Approved" hoặc "Rejected"
        public string? Note { get; set; }
    }


    public class DoctorAvailabilityDto
    {
        public Guid DoctorAvailabilityId { get; set; }
        public Guid DoctorId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;

        // Sửa TimeSpan thành string
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    public class CreateDoctorAvailabilityRequest
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class UpdateDoctorAvailabilityRequest
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsActive { get; set; }
    }


    public class DoctorAvailabilityExceptionDto
    {
        public Guid ExceptionId { get; set; }
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; }
        public DateTime Date { get; set; }

        // Sửa TimeSpan? thành string?
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;
        public bool IsAvailableOverride { get; set; }
    }

    public class CreateDoctorAvailabilityExceptionRequest
    {
        public DateTime Date { get; set; }
        public TimeSpan? StartTime { get; set; } // Nếu null nghĩa là nghỉ/tăng ca cả ngày
        public TimeSpan? EndTime { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool IsAvailableOverride { get; set; } = false;
    }

    public class UpdateDoctorAvailabilityExceptionRequest
    {
        public DateTime Date { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Status { get; set; } = string.Empty; // Chỉ nhận "Pending", "Approved", "Rejected"
        public string? Reason { get; set; } = string.Empty;
        public bool IsAvailableOverride { get; set; }
    }

    public class DoctorAvailabilityExceptionFilter
    {
        public Guid? DoctorId { get; set; }
        public bool? IsAvailableOverride { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Status { get; set; }
        public bool IsDescending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }



    public class DigitalMedicineItemDto
    {
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty; // VD: 500mg
        public int Quantity { get; set; }
        public string Unit { get; set; } = string.Empty; // VD: Viên, Lọ
        public string Instructions { get; set; } = string.Empty; // VD: Sáng 1 viên, tối 1 viên
    }

    public class PrescriptionByDoctorDto
    {
        public Guid DigitalPrescriptionId { get; set; }
        public Guid ConsultanSessionId { get; set; }
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty; // Tên bác sĩ kê đơn
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty; // Tên bệnh nhân
        public string? Diagnosis { get; set; }
        public string? Advice { get; set; }
        public List<DigitalMedicineItemDto> Medicines { get; set; } = new(); // Danh sách thuốc (đã parse từ JSON)
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePrescriptionByDoctorRequest
    {
        public Guid ConsultanSessionId { get; set; }
        public Guid MemberId { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string Advice { get; set; } = string.Empty;
        public List<DigitalMedicineItemDto> Medicines { get; set; } = new();
    }

    public class UpdatePrescriptionByDoctorRequest
    {
        public string Diagnosis { get; set; } = string.Empty;
        public string Advice { get; set; } = string.Empty;
        public List<DigitalMedicineItemDto> Medicines { get; set; } = new();
    }
}
