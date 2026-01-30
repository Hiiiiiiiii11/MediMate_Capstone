using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Npgsql.BackendMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{

    public interface IUploadPhotoService
    {
        string UploadPhotoAsync(IFormFile file);
    }
    public class UploadPhotoService : IUploadPhotoService
    {
        private readonly Cloudinary _cloudinary;
        public UploadPhotoService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }
        public string UploadPhotoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File cannot be null or empty.");
            }
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
            };
            var uploadResult = _cloudinary.Upload(uploadParams);
            if (uploadResult.Error != null)
            {
                throw new Exception($"Error uploading photo: {uploadResult.Error.Message}");
            }
            return uploadResult.SecureUrl.AbsoluteUri;
        }
    }
}
