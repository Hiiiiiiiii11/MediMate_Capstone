using MediMateService.DTOs;
using Microsoft.AspNetCore.Http;

namespace MediMateService.Services
{
    public interface IUploadPhotoService
    {
        Task<FileUploadResult> UploadPhotoAsync(IFormFile file);
        Task<FileUploadResult> UploadPrescriptionPhotoAsync(IFormFile file);
    }
}
