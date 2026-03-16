using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class RagBaseEmbeddingDto
    {
        public Guid EmbeddingId { get; set; }
        public Guid RagDocumentId { get; set; }
        public string DocName { get; set; } = string.Empty; // Tên tài liệu gốc
        public string Text { get; set; } = string.Empty; // Đoạn text đã được cắt
        public float[] Embedding { get; set; } = Array.Empty<float>(); // Vector số
        public string Metadata { get; set; } = string.Empty; // Thông tin thêm (Trang số mấy, tác giả...)
        public string ParentNodeId { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public int Level { get; set; }
        public int? ChunkSize { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateRagBaseEmbeddingRequest
    {
        public Guid RagDocumentId { get; set; }
        public string Text { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public string Metadata { get; set; } = string.Empty;
        public string ParentNodeId { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public int Level { get; set; } = 0;
        public int? ChunkSize { get; set; }
    }

    public class UpdateRagBaseEmbeddingRequest
    {
        public string Text { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public string Metadata { get; set; } = string.Empty;
    }
}
