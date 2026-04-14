using System;
using System.Collections.Generic;

namespace MediMateService.DTOs
{
    public class DrugInteractionConflict
    {
        public string NewDrugName { get; set; } = string.Empty;
        public string ConflictingDrugName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class DrugInteractionCheckResult
    {
        public bool HasInteraction { get; set; }
        public List<DrugInteractionConflict> Conflicts { get; set; } = new();
    }
}
