using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class RagBaseConfigDto
    {
        public Guid ConfigId { get; set; }
        public string EmbeddingModel { get; set; } = string.Empty;
        public string LLMModel { get; set; } = string.Empty;
        public int ChunkSize { get; set; }
        public int ChunkOverlap { get; set; }
        public int TopK { get; set; }
        public float Temperature { get; set; }
        public int MaxTokens { get; set; }
        public int ContextWindow { get; set; }
        public string PromptTemplate { get; set; } = string.Empty;
        public string ResponseType { get; set; } = string.Empty;
        public bool IsUseApi { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // THÊM MỚI DTO CREATE
    public class CreateRagBaseConfigRequest
    {
        public string EmbeddingModel { get; set; } = string.Empty;
        public string LLMModel { get; set; } = string.Empty;
        public int ChunkSize { get; set; }
        public int ChunkOverlap { get; set; }
        public int TopK { get; set; }
        public float Temperature { get; set; }
        public int MaxTokens { get; set; }
        public int ContextWindow { get; set; }
        public string PromptTemplate { get; set; } = string.Empty;
        public string ResponseType { get; set; } = string.Empty;
        public bool IsUseApi { get; set; }
    }

    public class UpdateRagBaseConfigRequest
    {
        public string EmbeddingModel { get; set; } = string.Empty;
        public string LLMModel { get; set; } = string.Empty;
        public int ChunkSize { get; set; }
        public int ChunkOverlap { get; set; }
        public int TopK { get; set; }
        public float Temperature { get; set; }
        public int MaxTokens { get; set; }
        public int ContextWindow { get; set; }
        public string PromptTemplate { get; set; } = string.Empty;
        public string ResponseType { get; set; } = string.Empty;
        public bool IsUseApi { get; set; }
    }
}
