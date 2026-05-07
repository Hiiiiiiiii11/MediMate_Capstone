using System.Threading.Tasks;
using Share.Common;
using System.Collections.Generic;
using MediMateService.DTOs;

namespace MediMateService.Services
{
    public interface IDrugDataService
    {
        Task<ApiResponse<object>> ImportDrugsFromXmlAsync(string filePath);
        Task<ApiResponse<List<DrugDto>>> SearchDrugsAsync(string searchTerm, int limit = 10);
    }
}
