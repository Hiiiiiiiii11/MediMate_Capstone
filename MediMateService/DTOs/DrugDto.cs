using System;

namespace MediMateService.DTOs
{
    public class DrugDto
    {
        public Guid DrugId { get; set; }
        public string DrugBankId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Synonyms { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
