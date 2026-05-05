namespace MediMateService.DTOs
{
    public class MembershipPackageDto
    {
        public Guid PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "VND";
        public int DurationDays { get; set; }
        public int MemberLimit { get; set; }
        public int OcrLimit { get; set; }
        public string? Description { get; set; }
        public int ActiveSubscriberCount { get; set; }
        public bool AllowVideoRecordingAccess { get; set; }
        public bool HealthAlertEnabled { get; set; }
    }

    public class CreateMembershipPackageDto
    {
        public string PackageName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "VND";
        public int DurationDays { get; set; }
        public int MemberLimit { get; set; }
        public int OcrLimit { get; set; }
        public string? Description { get; set; }
        public bool AllowVideoRecordingAccess { get; set; } = false;
        public bool HealthAlertEnabled { get; set; } = false;
    }

    public class UpdateMembershipPackageDto
    {
        public string? PackageName { get; set; }
        public bool? IsActive { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public int? DurationDays { get; set; }
        public int? MemberLimit { get; set; }
        public int? OcrLimit { get; set; }
        public string? Description { get; set; }
        public bool? AllowVideoRecordingAccess { get; set; }
        public bool? HealthAlertEnabled { get; set; }
    }
}
