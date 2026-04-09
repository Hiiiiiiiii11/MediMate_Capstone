using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    // 2. Filter cho Hồ sơ Bác sĩ (DoctorDocument)
    public class DoctorDocumentFilter
    {
        public Guid? DoctorId { get; set; }
        public string? Status { get; set; } // Pending, Approved, Rejected
        public string? Type { get; set; }
        public string? SortBy { get; set; } // "CreatedAt", "Status", "Type"
        public bool IsDescending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // 3. Filter cho Ảnh đơn thuốc (PrescriptionImages)
    public class PrescriptionImageFilter
    {
        public Guid? PrescriptionId { get; set; }
        public Guid? MemberId { get; set; }
        public bool? IsProcessed { get; set; }
        public string? SortBy { get; set; } // "UploadedAt"
        public bool IsDescending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // 4. DTO hiển thị cho Ảnh đơn thuốc (Vì ban đầu bạn chưa có DTO riêng cho ảnh độc lập)
    public class PrescriptionImageDetailDto
    {
        public Guid ImageId { get; set; }
        public Guid PrescriptionId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string OcrRawData { get; set; } = string.Empty;
        public bool IsProcessed { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class RatingFilter
    {
        public Guid? DoctorId { get; set; }
        public Guid? MemberId { get; set; }
        public int? Score { get; set; }
        public int? MinScore { get; set; }
        public int? MaxScore { get; set; }
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
