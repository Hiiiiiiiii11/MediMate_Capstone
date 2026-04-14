using System.Threading.Tasks;
using Share.Common;

namespace MediMateService.Services
{
    public interface IDrugDataService
    {
        Task<ApiResponse<object>> ImportDrugsFromXmlAsync(string filePath);
    }
}
