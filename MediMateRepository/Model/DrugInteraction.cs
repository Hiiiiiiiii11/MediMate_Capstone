using System;

namespace MediMateRepository.Model
{
    public class DrugInteraction
    {
        public Guid InteractionId { get; set; }
        public Guid DrugId { get; set; }
        public Drug Drug { get; set; }
        
        public string InteractingDrugBankId { get; set; } = string.Empty;
        public string InteractingDrugName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
