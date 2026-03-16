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
    public class DoctorDocumentService : IDoctorDocumentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DoctorDocumentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<DoctorDocumentDto>> CreateAsync(Guid doctorId, Guid currentUserId, CreateDoctorDocumentRequest request)
        {
            var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(doctorId);
            if (doctor == null)
                return ApiResponse<DoctorDocumentDto>.Fail("Không tìm thấy thông tin bác sĩ.", 404);

            if (doctor.UserId != currentUserId)
                return ApiResponse<DoctorDocumentDto>.Fail("Bạn không có quyền thêm tài liệu cho bác sĩ này.", 403);

            var document = new DoctorDocument
            {
                DocumentId = Guid.NewGuid(),
                DoctorId = doctorId,
                FileUrl = request.FileUrl,
                Type = request.Type,
                Status = "Pending", // Mặc định luôn là chờ duyệt
                Note = string.Empty,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<DoctorDocument>().AddAsync(document);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorDocumentDto>.Ok(MapToDto(document), "Nộp tài liệu thành công. Vui lòng chờ phê duyệt.");
        }

        public async Task<ApiResponse<IEnumerable<DoctorDocumentDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            var documents = await _unitOfWork.Repository<DoctorDocument>()
                .FindAsync(d => d.DoctorId == doctorId);

            var response = documents.OrderByDescending(d => d.CreatedAt).Select(MapToDto);
            return ApiResponse<IEnumerable<DoctorDocumentDto>>.Ok(response);
        }

        public async Task<ApiResponse<DoctorDocumentDto>> GetByIdAsync(Guid documentId)
        {
            var document = await _unitOfWork.Repository<DoctorDocument>().GetByIdAsync(documentId);
            if (document == null)
                return ApiResponse<DoctorDocumentDto>.Fail("Không tìm thấy tài liệu.", 404);

            return ApiResponse<DoctorDocumentDto>.Ok(MapToDto(document));
        }

        public async Task<ApiResponse<DoctorDocumentDto>> UpdateAsync(Guid documentId, Guid currentUserId, UpdateDoctorDocumentRequest request)
        {
            var document = (await _unitOfWork.Repository<DoctorDocument>()
                .FindAsync(d => d.DocumentId == documentId, "Doctor")).FirstOrDefault();

            if (document == null)
                return ApiResponse<DoctorDocumentDto>.Fail("Không tìm thấy tài liệu.", 404);

            if (document.Doctor.UserId != currentUserId)
                return ApiResponse<DoctorDocumentDto>.Fail("Bạn không có quyền sửa tài liệu này.", 403);

            // Bác sĩ cập nhật lại tài liệu -> Trạng thái phải quay về Pending để duyệt lại
            document.FileUrl = request.FileUrl;
            document.Type = request.Type;
            document.Status = "Pending";
            document.ReviewBy = string.Empty;
            document.ReviewAt = string.Empty;
            document.Note = "Tài liệu vừa được cập nhật, chờ duyệt lại.";

            _unitOfWork.Repository<DoctorDocument>().Update(document);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorDocumentDto>.Ok(MapToDto(document), "Cập nhật tài liệu thành công. Vui lòng chờ phê duyệt lại.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid documentId, Guid currentUserId)
        {
            var document = (await _unitOfWork.Repository<DoctorDocument>()
                .FindAsync(d => d.DocumentId == documentId, "Doctor")).FirstOrDefault();

            if (document == null)
                return ApiResponse<bool>.Fail("Không tìm thấy tài liệu.", 404);

            if (document.Doctor.UserId != currentUserId)
                return ApiResponse<bool>.Fail("Bạn không có quyền xóa tài liệu này.", 403);

            _unitOfWork.Repository<DoctorDocument>().Remove(document);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Xóa tài liệu thành công.");
        }

        // Dành riêng cho Admin / Quản lý
        public async Task<ApiResponse<DoctorDocumentDto>> ReviewDocumentAsync(Guid documentId, string reviewerName, ReviewDoctorDocumentRequest request)
        {
            var document = await _unitOfWork.Repository<DoctorDocument>().GetByIdAsync(documentId);
            if (document == null)
                return ApiResponse<DoctorDocumentDto>.Fail("Không tìm thấy tài liệu.", 404);

            if (request.Status != "Approved" && request.Status != "Rejected")
                return ApiResponse<DoctorDocumentDto>.Fail("Trạng thái duyệt không hợp lệ.", 400);

            if (request.Status == "Rejected" && string.IsNullOrWhiteSpace(request.Note))
            {
                return ApiResponse<DoctorDocumentDto>.Fail("Vui lòng nhập lý do từ chối (Note) để bác sĩ biết và khắc phục.", 400);
            }

            document.Status = request.Status;
            document.Note = request.Note ?? string.Empty;
            document.ReviewBy = reviewerName;
            document.ReviewAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            _unitOfWork.Repository<DoctorDocument>().Update(document);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorDocumentDto>.Ok(MapToDto(document), "Phê duyệt tài liệu thành công.");
        }

        private DoctorDocumentDto MapToDto(DoctorDocument d)
        {
            return new DoctorDocumentDto
            {
                DocumentId = d.DocumentId,
                DoctorId = d.DoctorId,
                FileUrl = d.FileUrl,
                Type = d.Type,
                Status = d.Status,
                ReviewBy = d.ReviewBy,
                ReviewAt = d.ReviewAt,
                Note = d.Note,
                CreatedAt = d.CreatedAt
            };
        }
    }
}