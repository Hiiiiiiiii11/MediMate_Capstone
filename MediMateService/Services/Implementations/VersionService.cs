using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Microsoft.EntityFrameworkCore;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediMateService.Services.Implementations
{
    public class VersionService : IVersionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public VersionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<IEnumerable<VersionDto>>> GetAllVersionsAsync(string? platform = null)
        {
            var query = _unitOfWork.Repository<Versions>().GetQueryable().AsNoTracking();

            if (!string.IsNullOrEmpty(platform))
            {
                query = query.Where(v => v.Platform.ToLower() == platform.ToLower());
            }

            var versions = await query.OrderByDescending(v => v.ReleaseDate).ToListAsync();

            var dtos = versions.Select(MapToDto);
            return ApiResponse<IEnumerable<VersionDto>>.Ok(dtos, "Lấy danh sách version thành công.");
        }

        public async Task<ApiResponse<VersionDto>> GetVersionByIdAsync(Guid versionId)
        {
            var version = await _unitOfWork.Repository<Versions>().GetByIdAsync(versionId);
            if (version == null)
            {
                return ApiResponse<VersionDto>.Fail("Không tìm thấy thông tin phiên bản.", 404);
            }

            return ApiResponse<VersionDto>.Ok(MapToDto(version));
        }

        public async Task<ApiResponse<VersionDto>> CheckLatestVersionAsync(string platform)
        {
            if (string.IsNullOrEmpty(platform))
            {
                return ApiResponse<VersionDto>.Fail("Vui lòng cung cấp tên Platform (Android/iOS).", 400);
            }

            // Lấy version Active mới nhất của platform đó
            var latestVersion = await _unitOfWork.Repository<Versions>().GetQueryable()
                .AsNoTracking()
                .Where(v => v.Platform.ToLower() == platform.ToLower() && v.Status == "Active")
                .OrderByDescending(v => v.ReleaseDate)
                .FirstOrDefaultAsync();

            if (latestVersion == null)
            {
                return ApiResponse<VersionDto>.Fail("Chưa có phiên bản nào khả dụng.", 404);
            }

            return ApiResponse<VersionDto>.Ok(MapToDto(latestVersion), "Lấy phiên bản mới nhất thành công.");
        }

        public async Task<ApiResponse<VersionDto>> CreateVersionAsync(CreateVersionDto request)
        {
            var newVersion = new Versions
            {
                VersionId = Guid.NewGuid(),
                VersionNumber = request.VersionNumber.Trim(),
                Platform = request.Platform.Trim(),
                ReleaseNotes = request.ReleaseNotes,
                DownloadUrl = request.DownloadUrl,
                IsForceUpdate = request.IsForceUpdate,
                Status = string.IsNullOrEmpty(request.Status) ? "Active" : request.Status,
                ReleaseDate = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<Versions>().AddAsync(newVersion);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<VersionDto>.Ok(MapToDto(newVersion), "Tạo phiên bản mới thành công.");
        }

        public async Task<ApiResponse<VersionDto>> UpdateVersionAsync(Guid versionId, UpdateVersionDto request)
        {
            var version = await _unitOfWork.Repository<Versions>().GetByIdAsync(versionId);
            if (version == null)
            {
                return ApiResponse<VersionDto>.Fail("Không tìm thấy thông tin phiên bản.", 404);
            }

            if (request.VersionNumber != null) version.VersionNumber = request.VersionNumber.Trim();
            if (request.Platform != null) version.Platform = request.Platform.Trim();
            if (request.ReleaseNotes != null) version.ReleaseNotes = request.ReleaseNotes;
            if (request.DownloadUrl != null) version.DownloadUrl = request.DownloadUrl;
            if (request.IsForceUpdate.HasValue) version.IsForceUpdate = request.IsForceUpdate.Value;
            if (request.Status != null) version.Status = request.Status;

            version.UpdatedAt = DateTime.Now;

            _unitOfWork.Repository<Versions>().Update(version);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<VersionDto>.Ok(MapToDto(version), "Cập nhật phiên bản thành công.");
        }

        public async Task<ApiResponse<bool>> DeleteVersionAsync(Guid versionId)
        {
            var version = await _unitOfWork.Repository<Versions>().GetByIdAsync(versionId);
            if (version == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy thông tin phiên bản.", 404);
            }

            _unitOfWork.Repository<Versions>().Remove(version);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Xóa phiên bản thành công.");
        }

        // Hàm ánh xạ nội bộ
        private static VersionDto MapToDto(Versions entity)
        {
            return new VersionDto
            {
                VersionId = entity.VersionId,
                VersionNumber = entity.VersionNumber,
                Platform = entity.Platform,
                ReleaseNotes = entity.ReleaseNotes,
                DownloadUrl = entity.DownloadUrl,
                IsForceUpdate = entity.IsForceUpdate,
                ReleaseDate = entity.ReleaseDate,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}