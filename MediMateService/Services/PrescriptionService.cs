using MediMateService.DTOs;
using Microsoft.AspNetCore.Http;
using Share.Common;

namespace MediMateService.Services
{
    public interface IPrescriptionService
    {
        // Tạo đơn thuốc mới (Import từ kết quả quét của UI)
        Task<ApiResponse<PrescriptionResponse>> CreatePrescriptionAsync(Guid memberId, Guid userId, CreatePrescriptionRequest request);

        // Lấy danh sách đơn thuốc của 1 thành viên
        Task<ApiResponse<IEnumerable<PrescriptionResponse>>> GetPrescriptionsByMemberAsync(Guid memberId, Guid userId);

        // Lấy chi tiết 1 đơn thuốc
        Task<ApiResponse<PrescriptionResponse>> GetPrescriptionByIdAsync(Guid prescriptionId, Guid userId);
        // 1. Cập nhật đơn thuốc
        Task<ApiResponse<PrescriptionResponse>> UpdatePrescriptionAsync(Guid prescriptionId, Guid userId, UpdatePrescriptionRequest request);

        // 2. Xóa đơn thuốc
        Task<ApiResponse<bool>> DeletePrescriptionAsync(Guid prescriptionId, Guid userId);

        // 3. Upload thêm ảnh vào đơn thuốc đã có
        Task<ApiResponse<string>> AddImageToPrescriptionAsync(Guid prescriptionId, Guid userId, IFormFile file);
    }
}
