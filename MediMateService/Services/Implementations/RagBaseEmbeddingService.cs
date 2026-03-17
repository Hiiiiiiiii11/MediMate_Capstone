using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediMateService.Services.Implementations
{
    public class RagBaseEmbeddingService : IRagBaseEmbeddingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RagBaseEmbeddingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<RagBaseEmbeddingDto>> CreateAsync(CreateRagBaseEmbeddingRequest request)
        {
            // Kiểm tra Document gốc có tồn tại không
            var document = await _unitOfWork.Repository<RagBaseDocument>().GetByIdAsync(request.RagDocumentId);
            if (document == null)
                return ApiResponse<RagBaseEmbeddingDto>.Fail("Tài liệu gốc không tồn tại.", 404);

            var embedding = new RagBaseEmbedding
            {
                EmbeddingId = Guid.NewGuid(),
                RagDocumentId = request.RagDocumentId,
                Text = request.Text,
                Embedding = request.Embedding, // EF Core sẽ tự map mảng float[] (ví dụ dùng pgvector trong PostgreSQL)
                Metadata = request.Metadata,
                ParentNodeId = request.ParentNodeId,
                NodeId = request.NodeId,
                Level = request.Level,
                ChunkSize = request.ChunkSize,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<RagBaseEmbedding>().AddAsync(embedding);
            await _unitOfWork.CompleteAsync();

            embedding.RagBaseDocument = document; // Để map DTO lấy tên file

            return ApiResponse<RagBaseEmbeddingDto>.Ok(MapToDto(embedding), "Tạo Vector Embedding thành công.");
        }

        public async Task<ApiResponse<IEnumerable<RagBaseEmbeddingDto>>> GetByDocumentIdAsync(Guid documentId)
        {
            // Lấy tất cả các đoạn text thuộc về một tài liệu
            var embeddings = await _unitOfWork.Repository<RagBaseEmbedding>()
                .FindAsync(e => e.RagDocumentId == documentId, includeProperties: "RagBaseDocument");

            // Sắp xếp theo NodeId (hoặc thời gian tạo) để theo đúng thứ tự nội dung
            var response = embeddings.OrderBy(e => e.NodeId).Select(MapToDto);
            return ApiResponse<IEnumerable<RagBaseEmbeddingDto>>.Ok(response);
        }

        public async Task<ApiResponse<RagBaseEmbeddingDto>> GetByIdAsync(Guid embeddingId)
        {
            var embedding = (await _unitOfWork.Repository<RagBaseEmbedding>()
                .FindAsync(e => e.EmbeddingId == embeddingId, includeProperties: "RagBaseDocument")).FirstOrDefault();

            if (embedding == null)
                return ApiResponse<RagBaseEmbeddingDto>.Fail("Không tìm thấy đoạn Embedding này.", 404);

            return ApiResponse<RagBaseEmbeddingDto>.Ok(MapToDto(embedding));
        }

        public async Task<ApiResponse<RagBaseEmbeddingDto>> UpdateAsync(Guid embeddingId, UpdateRagBaseEmbeddingRequest request)
        {
            var embedding = (await _unitOfWork.Repository<RagBaseEmbedding>()
                .FindAsync(e => e.EmbeddingId == embeddingId, includeProperties: "RagBaseDocument")).FirstOrDefault();

            if (embedding == null)
                return ApiResponse<RagBaseEmbeddingDto>.Fail("Không tìm thấy đoạn Embedding này.", 404);

            embedding.Text = request.Text;
            embedding.Embedding = request.Embedding;
            embedding.Metadata = request.Metadata;

            _unitOfWork.Repository<RagBaseEmbedding>().Update(embedding);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<RagBaseEmbeddingDto>.Ok(MapToDto(embedding), "Cập nhật Vector Embedding thành công.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid embeddingId)
        {
            var embedding = await _unitOfWork.Repository<RagBaseEmbedding>().GetByIdAsync(embeddingId);
            if (embedding == null)
                return ApiResponse<bool>.Fail("Không tìm thấy đoạn Embedding này.", 404);

            _unitOfWork.Repository<RagBaseEmbedding>().Remove(embedding);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Xóa đoạn Vector Embedding thành công.");
        }

        private RagBaseEmbeddingDto MapToDto(RagBaseEmbedding e)
        {
            return new RagBaseEmbeddingDto
            {
                EmbeddingId = e.EmbeddingId,
                RagDocumentId = e.RagDocumentId,
                DocName = e.RagBaseDocument?.DocName ?? "Unknown",
                Text = e.Text,
                Embedding = e.Embedding,
                Metadata = e.Metadata,
                ParentNodeId = e.ParentNodeId,
                NodeId = e.NodeId,
                Level = e.Level,
                ChunkSize = e.ChunkSize,
                CreatedAt = e.CreatedAt
            };
        }
    }
}