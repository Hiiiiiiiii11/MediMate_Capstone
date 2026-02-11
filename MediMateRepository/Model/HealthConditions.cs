using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class HealthConditions
    {
        public Guid ConditionId { get; set; }
        public Guid HealthProfileId { get; set; }
        public string ConditionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DiagnosedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public virtual HealthProfiles HealthProfile { get; set; }
    }
}
