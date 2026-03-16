using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MediMateService.Services.Implementations
{
    public class RagBaseConfigService : IRagBaseConfigService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RagBaseConfigService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<RagBaseConfigDto>> CreateConfigAsync(CreateRagBaseConfigRequest request)
        {
            // Kiểm tra xem đã có cấu hình nào trong DB chưa
            var existingConfig = (await _unitOfWork.Repository<RagBaseConfig>().GetAllAsync()).FirstOrDefault();

            // Nếu đã tồn tại -> Chặn luôn, bắt buộc dùng hàm Update
            if (existingConfig != null)
            {
                return ApiResponse<RagBaseConfigDto>.Fail("Hệ thống đã có cấu hình. Chỉ cho phép duy nhất 1 cấu hình RAG tồn tại. Vui lòng sử dụng chức năng cập nhật.", 400);
            }

            var config = new RagBaseConfig
            {
                ConfigId = Guid.NewGuid(),
                EmbeddingModel = request.EmbeddingModel,
                LLMModel = request.LLMModel,
                ChunkSize = request.ChunkSize,
                ChunkOverlap = request.ChunkOverlap,
                TopK = request.TopK,
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                ContextWindow = request.ContextWindow,
                PromptTemplate = request.PromptTemplate,
                ResponseType = request.ResponseType,
                IsUseApi = request.IsUseApi,
                UpdatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<RagBaseConfig>().AddAsync(config);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<RagBaseConfigDto>.Ok(MapToDto(config), "Tạo cấu hình RAG thành công.");
        }

        public async Task<ApiResponse<RagBaseConfigDto>> GetConfigAsync()
        {
            var config = (await _unitOfWork.Repository<RagBaseConfig>().GetAllAsync()).FirstOrDefault();

            if (config == null)
            {
                return ApiResponse<RagBaseConfigDto>.Fail("Hệ thống chưa được thiết lập cấu hình RAG nào.", 404);
            }

            return ApiResponse<RagBaseConfigDto>.Ok(MapToDto(config));
        }

        public async Task<ApiResponse<RagBaseConfigDto>> UpdateConfigAsync(UpdateRagBaseConfigRequest request)
        {
            var config = (await _unitOfWork.Repository<RagBaseConfig>().GetAllAsync()).FirstOrDefault();

            if (config == null)
            {
                return ApiResponse<RagBaseConfigDto>.Fail("Chưa có cấu hình nào để cập nhật. Vui lòng tạo cấu hình mới trước.", 404);
            }

            // Ghi đè dữ liệu
            config.EmbeddingModel = request.EmbeddingModel;
            config.LLMModel = request.LLMModel;
            config.ChunkSize = request.ChunkSize;
            config.ChunkOverlap = request.ChunkOverlap;
            config.TopK = request.TopK;
            config.Temperature = request.Temperature;
            config.MaxTokens = request.MaxTokens;
            config.ContextWindow = request.ContextWindow;
            config.PromptTemplate = request.PromptTemplate;
            config.ResponseType = request.ResponseType;
            config.IsUseApi = request.IsUseApi;
            config.UpdatedAt = DateTime.Now;

            _unitOfWork.Repository<RagBaseConfig>().Update(config);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<RagBaseConfigDto>.Ok(MapToDto(config), "Cập nhật cấu hình RAG thành công.");
        }

        private RagBaseConfigDto MapToDto(RagBaseConfig c)
        {
            return new RagBaseConfigDto
            {
                ConfigId = c.ConfigId,
                EmbeddingModel = c.EmbeddingModel,
                LLMModel = c.LLMModel,
                ChunkSize = c.ChunkSize,
                ChunkOverlap = c.ChunkOverlap,
                TopK = c.TopK,
                Temperature = c.Temperature,
                MaxTokens = c.MaxTokens,
                ContextWindow = c.ContextWindow,
                PromptTemplate = c.PromptTemplate,
                ResponseType = c.ResponseType,
                IsUseApi = c.IsUseApi,
                UpdatedAt = c.UpdatedAt
            };
        }
    }
}