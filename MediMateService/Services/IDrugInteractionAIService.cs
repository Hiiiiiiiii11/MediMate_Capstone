using System.Collections.Generic;
using System.Threading.Tasks;
using MediMateService.DTOs;

namespace MediMateService.Services
{
    public class DrugInteractionExplainRequest
    {
        public string PrescriptionId { get; set; } = string.Empty;
        public string NewDrugName { get; set; } = string.Empty;
        public List<DrugInteractionConflict> Conflicts { get; set; } = new();
    }

    public interface IDrugInteractionAIService
    {
        Task<string> ExplainInteractionAsync(DrugInteractionExplainRequest request);
    }
}
