namespace MediMate.Models.Packages
{
    public class CreateMembershipPackageRequest
    {
        public string PackageName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "VND";
        public int DurationDays { get; set; }
        public int MemberLimit { get; set; }
        public int OcrLimit { get; set; }
        public int ConsultantLimit { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateMembershipPackageRequest
    {
        public string? PackageName { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public int? DurationDays { get; set; }
        public int? MemberLimit { get; set; }
        public int? OcrLimit { get; set; }
        public int? ConsultantLimit { get; set; }
        public string? Description { get; set; }
    }
}
