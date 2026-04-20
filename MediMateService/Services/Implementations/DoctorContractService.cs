using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services.Implementations
{
    public class DoctorContractService : IDoctorContractService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadPhotoService _uploadService;

        public DoctorContractService(IUnitOfWork unitOfWork, IUploadPhotoService uploadService)
        {
            _unitOfWork = unitOfWork;
            _uploadService = uploadService;
        }

        public async Task<ApiResponse<DoctorContractResponse>> CreateAsync(CreateContractRequest request)
        {
            if (request.File == null) return ApiResponse<DoctorContractResponse>.Fail("File hợp đồng là bắt buộc.", 400);

            // Upload lên Cloudinary folder doctor_documents
            string fileUrl = await _uploadService.UploadDocumentAsync(request.File);

            var contract = new DoctorContract
            {
                ContractId = Guid.NewGuid(),
                FileUrl = fileUrl,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = "Active",
                Note = request.Note,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<DoctorContract>().AddAsync(contract);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorContractResponse>.Ok(MapToResponse(contract));
        }

        public async Task<ApiResponse<DoctorContractResponse>> UpdateAsync(Guid id, UpdateContractRequest request)
        {
            var contract = await _unitOfWork.Repository<DoctorContract>().GetByIdAsync(id);
            if (contract == null) return ApiResponse<DoctorContractResponse>.Fail("Không tìm thấy hợp đồng.", 404);

            // [QUAN TRỌNG]: Nếu có gửi file mới thì upload, không thì giữ nguyên FileUrl cũ
            if (request.File != null && request.File.Length > 0)
            {
                contract.FileUrl = await _uploadService.UploadDocumentAsync(request.File);
            }

            if (request.StartDate.HasValue) contract.StartDate = request.StartDate;
            if (request.EndDate.HasValue) contract.EndDate = request.EndDate;
            if (!string.IsNullOrEmpty(request.Status)) contract.Status = request.Status;
            if (request.Note != null) contract.Note = request.Note;

            contract.UpdatedAt = DateTime.Now;

            _unitOfWork.Repository<DoctorContract>().Update(contract);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorContractResponse>.Ok(MapToResponse(contract));
        }

        public async Task<ApiResponse<DoctorContractResponse>> GetByIdAsync(Guid id)
        {
            var contract = await _unitOfWork.Repository<DoctorContract>().GetByIdAsync(id);
            return contract == null
                ? ApiResponse<DoctorContractResponse>.Fail("Không thấy dữ liệu.", 404)
                : ApiResponse<DoctorContractResponse>.Ok(MapToResponse(contract));
        }

        public async Task<ApiResponse<IEnumerable<DoctorContractResponse>>> GetAllAsync()
        {
            var list = await _unitOfWork.Repository<DoctorContract>().GetAllAsync();
            return ApiResponse<IEnumerable<DoctorContractResponse>>.Ok(list.Select(MapToResponse));
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var contract = await _unitOfWork.Repository<DoctorContract>().GetByIdAsync(id);
            if (contract == null) return ApiResponse<bool>.Fail("Không tìm thấy.", 404);

            _unitOfWork.Repository<DoctorContract>().Remove(contract);
            await _unitOfWork.CompleteAsync();
            return ApiResponse<bool>.Ok(true, "Đã xóa hợp đồng.");
        }

        private DoctorContractResponse MapToResponse(DoctorContract c) => new DoctorContractResponse
        {
            ContractId = c.ContractId,
            FileUrl = c.FileUrl,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            Status = c.Status,
            Note = c.Note
        };
    }
}
