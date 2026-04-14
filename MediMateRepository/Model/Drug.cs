using System;
using System.Collections.Generic;

namespace MediMateRepository.Model
{
    public class Drug
    {
        public Guid DrugId { get; set; }
        public string DrugBankId { get; set; } = string.Empty; // e.g. DB00001
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Navigation Property
        public ICollection<DrugInteraction> DrugInteractions { get; set; } = new List<DrugInteraction>();
    }
}
