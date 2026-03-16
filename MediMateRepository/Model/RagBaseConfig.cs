using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class RagBaseConfig
    {
        public Guid ConfigId { get; set; }
        public string EmbeddingModel { get; set; }
        public string LLMModel { get; set; }
        public int ChunkSize { get; set; }
        public int ChunkOverlap { get; set; }
        public int TopK { get; set; }
        public float Temperature { get; set; }
        public int MaxTokens { get; set; }
        public int ContextWindow { get; set; }
        public string PromptTemplate { get; set; }
        public string ResponseType { get; set; }
        public bool IsUseApi { get; set; }
        public DateTime UpdatedAt { get; set; }


    }
}
