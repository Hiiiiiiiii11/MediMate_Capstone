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
        Task<ApiResponse<bool>> DeletePrescriptionAsync(Guid prescriptionId, Guid userId);

        // 3. Upload thêm ảnh vào đơn thuốc đã có
        Task<ApiResponse<string>> AddImageToPrescriptionAsync(Guid prescriptionId, Guid userId, IFormFile file);
        // 1. Cập nhật đơn thuốc
        Task<ApiResponse<PrescriptionResponse>> UpdatePrescriptionAsync(Guid prescriptionId, Guid userId, UpdatePrescriptionRequest request);
        Task<ApiResponse<PrescriptionMedicineResponse>> AddMedicineAsync(Guid prescriptionId, Guid userId, AddMedicineRequest request);
        Task<ApiResponse<PrescriptionMedicineResponse>> UpdateMedicineAsync(Guid medicineId, Guid userId, UpdateMedicineRequest request);
        Task<ApiResponse<bool>> DeleteMedicineAsync(Guid medicineId, Guid userId);

        // 2. Xóa đơn thuốc

    }


    
}
