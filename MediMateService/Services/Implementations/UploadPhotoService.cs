using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MediMateService.DTOs;
using Microsoft.AspNetCore.Http;

namespace MediMateService.Services.Implementations
{
    public class UploadPhotoService : IUploadPhotoService
    {
        private readonly Cloudinary _cloudinary;
        public UploadPhotoService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }
        public async Task<FileUploadResult> UploadPhotoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File cannot be null or empty.");
            }

            using var stream = file.OpenReadStream();

            // 1. Upload ảnh gốc
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream)
            };

            // Dùng UploadAsync cho chuẩn non-blocking
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Cloudinary Error: {uploadResult.Error.Message}");
            }

            // 2. Tạo link Thumbnail (resize 200x200)
            var thumbnailUrl = _cloudinary.Api.UrlImgUp
                .Transform(new Transformation()
                    .Width(200).Height(200).Crop("fill").Gravity("auto")
                    .Quality("auto").FetchFormat("auto"))
                .BuildUrl(uploadResult.PublicId);

            // 3. Trả về kết quả
            return new FileUploadResult
            {
                OriginalUrl = uploadResult.SecureUrl.AbsoluteUri,
                ThumbnailUrl = thumbnailUrl
            };
        }
        public async Task<FileUploadResult> UploadPrescriptionPhotoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File cannot be null or empty.");
            }

            using var stream = file.OpenReadStream();

            // 1. Upload ảnh gốc
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                // Bạn có thể giữ nguyên kích thước gốc hoặc resize nhẹ nếu ảnh quá lớn (ví dụ max 2000px)
                Transformation = new Transformation().Width(2000).Crop("limit")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams); // Dùng Async cho chuẩn

            if (uploadResult.Error != null)
            {
                throw new Exception($"Error uploading photo: {uploadResult.Error.Message}");
            }

            // 2. Tạo link Thumbnail từ PublicId vừa upload
            // Cấu hình: Rộng 200, Cao 200, Cắt vừa khít (fill), Tự động nén (q_auto)
            var thumbnailUrl = _cloudinary.Api.UrlImgUp
                .Transform(new Transformation()
                    .Width(200).Height(200).Crop("fill").Gravity("auto")
                    .Quality("auto").FetchFormat("auto"))
                .BuildUrl(uploadResult.PublicId);

            // 3. Trả về cả 2 link
            return new FileUploadResult
            {
                OriginalUrl = uploadResult.SecureUrl.AbsoluteUri,
                ThumbnailUrl = thumbnailUrl
            };
        }
    }
}
