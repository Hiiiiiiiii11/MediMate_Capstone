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
    public class RagBaseDocumentService : IRagBaseDocumentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RagBaseDocumentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<RagBaseDocumentDto>> CreateAsync(CreateRagBaseDocumentRequest request)
        {
            // Kiểm tra Collection có tồn tại không
            var collection = await _unitOfWork.Repository<RagBaseCollection>().GetByIdAsync(request.CollectionId);
            if (collection == null)
                return ApiResponse<RagBaseDocumentDto>.Fail("Collection không tồn tại.", 404);

            // Tùy chọn: Check xem file (dựa vào CheckSum) đã từng được upload vào collection này chưa để tránh rác DB
            if (!string.IsNullOrEmpty(request.CheckSum))
            {
                var isDuplicate = (await _unitOfWork.Repository<RagBaseDocument>()
                    .FindAsync(d => d.CollectionId == request.CollectionId && d.CheckSum == request.CheckSum)).Any();

                if (isDuplicate)
                    return ApiResponse<RagBaseDocumentDto>.Fail("Tài liệu này đã tồn tại trong Collection.", 400);
            }

            var document = new RagBaseDocument
            {
                RagDocumentId = Guid.NewGuid(),
                CollectionId = request.CollectionId,
                DocName = request.DocName,
                FilePath = request.FilePath,
                Type = request.Type,
                FileSize = request.FileSize,
                CheckSum = request.CheckSum,
                Status = "Uploaded", // Trạng thái mặc định mới đưa link lên
                CreateAt = DateTime.Now
            };

            await _unitOfWork.Repository<RagBaseDocument>().AddAsync(document);
            await _unitOfWork.CompleteAsync();

            // Gán object để MapToDto có thể lấy được Tên Collection
            document.RagBaseCollection = collection;

            return ApiResponse<RagBaseDocumentDto>.Ok(MapToDto(document), "Thêm tài liệu vào Collection thành công.");
        }

        public async Task<ApiResponse<IEnumerable<RagBaseDocumentDto>>> GetByCollectionIdAsync(Guid collectionId)
        {
            var documents = await _unitOfWork.Repository<RagBaseDocument>()
                .FindAsync(d => d.CollectionId == collectionId, includeProperties: "RagBaseCollection");

            var response = documents.OrderByDescending(d => d.CreateAt).Select(MapToDto);
            return ApiResponse<IEnumerable<RagBaseDocumentDto>>.Ok(response);
        }

        public async Task<ApiResponse<RagBaseDocumentDto>> GetByIdAsync(Guid documentId)
        {
            var document = (await _unitOfWork.Repository<RagBaseDocument>()
                .FindAsync(d => d.RagDocumentId == documentId, includeProperties: "RagBaseCollection")).FirstOrDefault();

            if (document == null)
                return ApiResponse<RagBaseDocumentDto>.Fail("Không tìm thấy tài liệu.", 404);

            return ApiResponse<RagBaseDocumentDto>.Ok(MapToDto(document));
        }

        public async Task<ApiResponse<RagBaseDocumentDto>> UpdateAsync(Guid documentId, UpdateRagBaseDocumentRequest request)
        {
            var document = (await _unitOfWork.Repository<RagBaseDocument>()
                .FindAsync(d => d.RagDocumentId == documentId, includeProperties: "RagBaseCollection")).FirstOrDefault();

            if (document == null)
                return ApiResponse<RagBaseDocumentDto>.Fail("Không tìm thấy tài liệu.", 404);

            // Thường người ta chỉ đổi tên hoặc AI xử lý xong thì đổi Status, không đổi FilePath (đổi File thì phải upload lại cái mới)
            document.DocName = request.DocName;
            document.Status = request.Status;

            _unitOfWork.Repository<RagBaseDocument>().Update(document);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<RagBaseDocumentDto>.Ok(MapToDto(document), "Cập nhật tài liệu thành công.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid documentId)
        {
            var document = await _unitOfWork.Repository<RagBaseDocument>().GetByIdAsync(documentId);
            if (document == null)
                return ApiResponse<bool>.Fail("Không tìm thấy tài liệu.", 404);

            // Nhờ Cascade Delete, khi xóa Document thì các Embedding (chunk text) liên quan cũng sẽ tự động bay màu
            _unitOfWork.Repository<RagBaseDocument>().Remove(document);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Xóa tài liệu và các đoạn dữ liệu liên quan thành công.");
        }

        private RagBaseDocumentDto MapToDto(RagBaseDocument d)
        {
            return new RagBaseDocumentDto
            {
                RagDocumentId = d.RagDocumentId,
                CollectionId = d.CollectionId,
                CollectionName = d.RagBaseCollection?.Name ?? "Unknown",
                DocName = d.DocName,
                FilePath = d.FilePath,
                Type = d.Type,
                Status = d.Status,
                FileSize = d.FileSize,
                CheckSum = d.CheckSum,
                CreateAt = d.CreateAt
            };
        }

        public async Task<ApiResponse<IEnumerable<RagBaseDocumentDto>>> GetAllDocument()
        {
            var documents = await _unitOfWork.Repository<RagBaseDocument>()
                .GetAllAsync();

            var response = documents.OrderByDescending(d => d.CreateAt).Select(MapToDto);
            return ApiResponse<IEnumerable<RagBaseDocumentDto>>.Ok(response);
        }
    }
}