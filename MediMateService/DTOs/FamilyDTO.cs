using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class CreateSharedFamilyRequest
    {
        public string FamilyName { get; set; } = string.Empty;
    }
    public class UpdateFamilyRequest
    {
        public string? FamilyName { get; set; }
        public bool? IsOpenJoin { get; set; }
    }

    // Response chung cho cả 2 chế độ
    public class FamilyResponse
    {
        public Guid FamilyId { get; set; }
        public string FamilyName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Personal" hoặc "Shared"
        public string JoinCode { get; set; } 
        public bool IsOpenJoin { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class FamilySubscriptionResponse
    {
        public Guid SubscriptionId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int RemainingOcrCount { get; set; }
        public int RemainingConsultantCount { get; set; }
        public int OcrLimit { get; set; }
        public int ConsultantLimit { get; set; }
    }
}
