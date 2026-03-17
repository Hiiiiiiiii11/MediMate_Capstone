using System;

namespace MediMateService.DTOs
{
    public class RagBaseDocumentDto
    {
        public Guid RagDocumentId { get; set; }
        public Guid CollectionId { get; set; }
        public string CollectionName { get; set; } = string.Empty; // Tên collection (join bảng)
        public string DocName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // PDF, DOCX, TXT...
        public string Status { get; set; } = string.Empty; // Uploaded, Processing, Completed, Failed
        public int FileSize { get; set; } // Tính bằng KB hoặc Byte
        public string CheckSum { get; set; } = string.Empty; // Mã băm để check trùng lặp file
        public DateTime CreateAt { get; set; }
    }

    public class CreateRagBaseDocumentRequest
    {
        public Guid CollectionId { get; set; }
        public string DocName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty; // Link Cloudinary
        public string Type { get; set; } = string.Empty;
        public int FileSize { get; set; }
        public string CheckSum { get; set; } = string.Empty;
    }

    public class UpdateRagBaseDocumentRequest
    {
        public string DocName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Thường dùng để cập nhật trạng thái khi tool AI cắn/đọc file xong
    }
}
