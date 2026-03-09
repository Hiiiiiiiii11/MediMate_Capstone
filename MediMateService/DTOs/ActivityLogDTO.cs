using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class ActivityLogResponse
    {
        public Guid LogId { get; set; }
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty; // Người thực hiện
        public string ActionType { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OldDataJson { get; set; } = string.Empty;
        public string NewDataJson { get; set; } = string.Empty;
        public DateTime CreateAt { get; set; }
    }
}
