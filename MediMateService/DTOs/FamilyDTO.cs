using Microsoft.AspNetCore.Http;
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
        public IFormFile? FamilyAvatar { get; set; }
    }

    // Response chung cho cả 2 chế độ
    public class FamilyResponse
    {
        public Guid FamilyId { get; set; }
        public string FamilyName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Personal" hoặc "Shared"
        public string JoinCode { get; set; }
        public bool IsOpenJoin { get; set; }
        public string FamilyAvatarUrl { get; set; } = string.Empty;
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

    public class AdminFamilySubscriptionResponse
    {
        public Guid SubscriptionId { get; set; }
        public Guid FamilyId { get; set; }
        public string FamilyName { get; set; } = string.Empty;
        public string? FamilyAvatarUrl { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int RemainingOcrCount { get; set; }
        public int RemainingConsultantCount { get; set; }
        public decimal Price { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
    }

    public class AdminFamilySubscriptionFilter
    {
        public string? Status { get; set; } 
        public Guid? PackageId { get; set; }
        public string? SearchTerm { get; set; } 
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class UpdateSubscriptionStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class RefundableSubscriptionDto
    {
        public Guid SubscriptionId { get; set; }
        public Guid FamilyId { get; set; }
        public string FamilyName { get; set; } = string.Empty;
        public Guid PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public decimal Amount { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
