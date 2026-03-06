using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class ActivityLogs
    {
        public Guid LogId { get; set; }
        public Guid MemberId { get; set; }
        public Guid FamilyId { get; set; }
        public string ActionType { get; set; }
        public string EntityName { get; set; }
        public Guid EntityId { get; set; }
        public string Description { get; set; }
        public string OldDataJson { get; set; }
        public string NewDataJson { get; set; }
        public DateTime CreateAt { get; set; }
        public virtual Members? Member { get; set; }
        public virtual Families? Family { get; set; }
}
}
