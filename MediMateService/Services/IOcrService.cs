using MediMateService.DTOs;
using Microsoft.AspNetCore.Http;

namespace MediMateService.Services
{
    public interface IOcrService
    {
      
        Task<OcrScanResponse> ScanPrescriptionAsync(IFormFile file);
    }
}
