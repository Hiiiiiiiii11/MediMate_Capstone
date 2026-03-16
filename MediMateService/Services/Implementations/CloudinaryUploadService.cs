using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MediMateService.Services.Implementations
{
    public class CloudinaryUploadService : ICloudinaryUploadService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CloudinaryUploadService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<PagedResult<DoctorDocumentDto>>> GetDocumentsWithPaginationAsync(DoctorDocumentFilter filter)
        {
            // Đã sửa thành GetQueryable() cho đúng với IGenericRepository
            var query = _unitOfWork.Repository<DoctorDocument>().GetQueryable();

            // 1. FILTER (Lọc)
            if (filter.DoctorId.HasValue)
                query = query.Where(d => d.DoctorId == filter.DoctorId.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(d => d.Status == filter.Status);

            if (!string.IsNullOrEmpty(filter.Type))
                query = query.Where(d => d.Type.Contains(filter.Type));

            // Đếm tổng số bản ghi TRƯỚC KHI phân trang
            int totalCount = query.Count();

            // 2. SORT (Sắp xếp)
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                query = filter.SortBy.ToLower() switch
                {
                    "status" => filter.IsDescending ? query.OrderByDescending(d => d.Status) : query.OrderBy(d => d.Status),
                    "type" => filter.IsDescending ? query.OrderByDescending(d => d.Type) : query.OrderBy(d => d.Type),
                    _ => filter.IsDescending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
                };
            }
            else
            {
                query = query.OrderByDescending(d => d.CreatedAt); // Mặc định
            }

            // 3. PAGINATION (Phân trang - Skip & Take)
            var items = query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var result = new PagedResult<DoctorDocumentDto>
            {
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                Items = items.Select(MapToDto).ToList()
            };

            return ApiResponse<PagedResult<DoctorDocumentDto>>.Ok(result);
        }

        public async Task<ApiResponse<PagedResult<PrescriptionImageDetailDto>>> GetPrescriptionImagesPaginatedAsync(PrescriptionImageFilter filter, Guid currentUserId)
        {
            // Đã sửa thành GetQueryable()
            var query = _unitOfWork.Repository<PrescriptionImages>()
                .GetQueryable()
                .Select(img => new { Image = img, img.Prescription });

            // 1. FILTER (Lọc)
            if (filter.PrescriptionId.HasValue)
                query = query.Where(x => x.Image.PrescriptionId == filter.PrescriptionId.Value);

            if (filter.MemberId.HasValue)
                query = query.Where(x => x.Prescription.MemberId == filter.MemberId.Value);

            if (filter.IsProcessed.HasValue)
                query = query.Where(x => x.Image.IsProcessed == filter.IsProcessed.Value);

            int totalCount = query.Count();

            // 2. SORT (Sắp xếp)
            query = filter.IsDescending
                ? query.OrderByDescending(x => x.Image.UploadedAt)
                : query.OrderBy(x => x.Image.UploadedAt);

            // 3. PAGINATION (Phân trang)
            var pagedData = query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => x.Image)
                .ToList();

            var resultList = pagedData.Select(i => new PrescriptionImageDetailDto
            {
                ImageId = i.ImageId,
                PrescriptionId = i.PrescriptionId,
                ImageUrl = i.ImageUrl,
                ThumbnailUrl = i.ThumbnailUrl ?? i.ImageUrl,
                OcrRawData = i.OcrRawData,
                IsProcessed = i.IsProcessed,
                UploadedAt = i.UploadedAt
            }).ToList();

            var result = new PagedResult<PrescriptionImageDetailDto>
            {
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                Items = resultList
            };

            return ApiResponse<PagedResult<PrescriptionImageDetailDto>>.Ok(result);
        }

        // Bổ sung hàm MapToDto ở đây
        private DoctorDocumentDto MapToDto(DoctorDocument d)
        {
            return new DoctorDocumentDto
            {
                DocumentId = d.DocumentId,
                DoctorId = d.DoctorId,
                FileUrl = d.FileUrl,
                Type = d.Type,
                Status = d.Status,
                ReviewBy = d.ReviewBy,
                ReviewAt = d.ReviewAt,
                Note = d.Note,
                CreatedAt = d.CreatedAt
            };
        }
    }
}