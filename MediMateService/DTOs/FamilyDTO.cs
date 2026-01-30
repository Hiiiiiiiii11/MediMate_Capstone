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
        public string FamilyName { get; set; }
    }

    // Response chung cho cả 2 chế độ
    public class FamilyResponse
    {
        public Guid FamilyId { get; set; }
        public string FamilyName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Personal" hoặc "Shared"
        public string JoinCode { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
