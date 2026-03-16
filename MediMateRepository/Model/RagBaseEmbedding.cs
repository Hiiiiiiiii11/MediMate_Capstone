using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class RagBaseEmbedding
    {
        public Guid EmbeddingId { get; set; }
        public Guid RagDocumentId { get; set; }
        public string Text { get; set; }
        public float[] Embedding { get; set; }
        public string Metadata { get; set; }
        public string ParentNodeId { get; set; }
        public string NodeId { get; set; }

        public int Level { get; set; } = 0;

        public int? ChunkSize { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public virtual RagBaseDocument RagBaseDocument { get; set; }
    }
}
