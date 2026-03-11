namespace MediMateService.DTOs
{
    public class MembershipPackageDto
    {
        public Guid PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "VND";
        public int DurationDays { get; set; }
        public int MemberLimit { get; set; }
        public string? Description { get; set; }
    }

    public class CreateMembershipPackageDto
    {
        public string PackageName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "VND";
        public int DurationDays { get; set; }
        public int MemberLimit { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateMembershipPackageDto
    {
        public string? PackageName { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public int? DurationDays { get; set; }
        public int? MemberLimit { get; set; }
        public string? Description { get; set; }
    }
}
