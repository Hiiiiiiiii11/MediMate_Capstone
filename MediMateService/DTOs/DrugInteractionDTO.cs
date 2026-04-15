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

    /// <summary>
    /// Payload được nhét vào data của response 409,
    /// FE dùng để gọi thẾ́ng /drug-interactions/explain
    /// </summary>
    public class DrugInteractionPayload
    {
        public string PrescriptionId { get; set; } = string.Empty;
        public string NewDrugName { get; set; } = string.Empty;
        public List<DrugInteractionConflict> Conflicts { get; set; } = new();
    }
}
