using MediMateService.DTOs;
using Microsoft.AspNetCore.Http;

namespace MediMateService.Services
{
    public interface IUploadPhotoService
    {
        Task<FileUploadResult> UploadPhotoAsync(IFormFile file);
        Task<FileUploadResult> UploadPrescriptionPhotoAsync(IFormFile file);
        Task<string> UploadDocumentAsync(IFormFile file);

        /// <summary>
        /// Upload video (stream) lên Cloudinary vào folder chỉ định.
        /// Dùng cho Agora Cloud Recording video sau khi tải về từ Agora CDN.
        /// </summary>
        Task<string?> UploadVideoFromStreamAsync(Stream videoStream, string publicId, string folder = "consultation_recordings");
    }
}
