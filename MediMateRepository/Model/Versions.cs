using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class Versions
    {
        public Guid VersionId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string VersionNumber { get; set; } = string.Empty;
        // Ví dụ: "1.0.0", "1.2.3"

        [Required]
        [MaxLength(20)]
        public string Platform { get; set; } = string.Empty;
        // Ví dụ: "Android", "iOS", "Backend" (Phân biệt vì version của iOS và Android có thể lệch nhau)

        public string? ReleaseNotes { get; set; }
        // Nội dung bản cập nhật: "Sửa lỗi UI, thêm tính năng chat..."

        public string? DownloadUrl { get; set; }
        // Link dẫn tới App Store / CH Play hoặc link tải file APK trực tiếp

        public bool IsForceUpdate { get; set; } = false;
        // Cực kỳ quan trọng: Nếu true -> App phải khóa màn hình và bắt người dùng update mới cho dùng tiếp

        public DateTime ReleaseDate { get; set; } = DateTime.Now;
        // Ngày chính thức phát hành bản này

        [MaxLength(20)]
        public string Status { get; set; } = "Active";
        // Trạng thái: "Active" (Đang dùng), "Deprecated" (Đã cũ), "Beta" (Đang test)

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
