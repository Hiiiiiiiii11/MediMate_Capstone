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
    public class RagBaseCollectionService : IRagBaseCollectionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RagBaseCollectionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<RagBaseCollectionDto>> CreateAsync(CreateRagBaseCollectionRequest request)
        {
            // Kiểm tra trùng tên Collection (nếu cần)
            var isExist = (await _unitOfWork.Repository<RagBaseCollection>()
                .FindAsync(c => c.Name.ToLower() == request.Name.ToLower())).Any();

            if (isExist)
                return ApiResponse<RagBaseCollectionDto>.Fail("Tên bộ sưu tập đã tồn tại. Vui lòng chọn tên khác.", 400);

            var collection = new RagBaseCollection
            {
                CollectionId = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                IsActive = request.IsActive,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<RagBaseCollection>().AddAsync(collection);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<RagBaseCollectionDto>.Ok(MapToDto(collection), "Tạo bộ sưu tập AI thành công.");
        }

        public async Task<ApiResponse<IEnumerable<RagBaseCollectionDto>>> GetAllAsync()
        {
            var collections = await _unitOfWork.Repository<RagBaseCollection>().GetAllAsync();
            var response = collections.OrderByDescending(c => c.CreatedAt).Select(MapToDto);
            return ApiResponse<IEnumerable<RagBaseCollectionDto>>.Ok(response);
        }

        public async Task<ApiResponse<RagBaseCollectionDto>> GetByIdAsync(Guid collectionId)
        {
            var collection = await _unitOfWork.Repository<RagBaseCollection>().GetByIdAsync(collectionId);
            if (collection == null)
                return ApiResponse<RagBaseCollectionDto>.Fail("Không tìm thấy bộ sưu tập này.", 404);

            return ApiResponse<RagBaseCollectionDto>.Ok(MapToDto(collection));
        }

        public async Task<ApiResponse<RagBaseCollectionDto>> UpdateAsync(Guid collectionId, UpdateRagBaseCollectionRequest request)
        {
            var collection = await _unitOfWork.Repository<RagBaseCollection>().GetByIdAsync(collectionId);
            if (collection == null)
                return ApiResponse<RagBaseCollectionDto>.Fail("Không tìm thấy bộ sưu tập này.", 404);

            // Kiểm tra trùng tên (Bỏ qua chính nó)
            var isExist = (await _unitOfWork.Repository<RagBaseCollection>()
                .FindAsync(c => c.Name.ToLower() == request.Name.ToLower() && c.CollectionId != collectionId)).Any();

            if (isExist)
                return ApiResponse<RagBaseCollectionDto>.Fail("Tên bộ sưu tập đã tồn tại. Vui lòng chọn tên khác.", 400);

            collection.Name = request.Name;
            collection.Description = request.Description;
            collection.IsActive = request.IsActive;

            _unitOfWork.Repository<RagBaseCollection>().Update(collection);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<RagBaseCollectionDto>.Ok(MapToDto(collection), "Cập nhật bộ sưu tập thành công.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid collectionId)
        {
            var collection = await _unitOfWork.Repository<RagBaseCollection>().GetByIdAsync(collectionId);
            if (collection == null)
                return ApiResponse<bool>.Fail("Không tìm thấy bộ sưu tập này.", 404);

            // Ở đây bạn đã set Cascade Delete ở DbContext
            // Xóa Collection -> Tự động xóa RagBaseDocument -> Tự động xóa RagBaseEmbedding
            _unitOfWork.Repository<RagBaseCollection>().Remove(collection);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Xóa bộ sưu tập và toàn bộ tài liệu bên trong thành công.");
        }

        private RagBaseCollectionDto MapToDto(RagBaseCollection c)
        {
            return new RagBaseCollectionDto
            {
                CollectionId = c.CollectionId,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            };
        }
    }
}