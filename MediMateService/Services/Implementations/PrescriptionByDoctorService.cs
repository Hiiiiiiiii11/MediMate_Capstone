using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MediMateService.Services.Implementations
{
    public class PrescriptionByDoctorService : IPrescriptionByDoctorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public PrescriptionByDoctorService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<PrescriptionByDoctorDto>> CreateAsync(Guid doctorId, Guid currentUserId, CreatePrescriptionByDoctorRequest request)
        {
            var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(doctorId);
            if (doctor == null || doctor.UserId != currentUserId)
                return ApiResponse<PrescriptionByDoctorDto>.Fail("Bạn không có quyền kê đơn thuốc này.", 403);

            var session = await _unitOfWork.Repository<ConsultationSessions>().GetByIdAsync(request.ConsultanSessionId);
            if (session == null)
                return ApiResponse<PrescriptionByDoctorDto>.Fail("Không tìm thấy phiên tư vấn.", 404);

            if (session.DoctorId != doctorId)
                return ApiResponse<PrescriptionByDoctorDto>.Fail("Bạn không phải là bác sĩ phụ trách phiên khám này.", 403);

            if (session.MemberId != request.MemberId)
                return ApiResponse<PrescriptionByDoctorDto>.Fail("Thông tin bệnh nhân không khớp với phiên khám.", 400);

            // Serialize list thuốc thành chuỗi JSON
            string medicinesJson = JsonSerializer.Serialize(request.Medicines);

            var prescription = new PrescriptionsByDoctor
            {
                DigitalPrescriptionId = Guid.NewGuid(),
                ConsultanSessionId = request.ConsultanSessionId,
                DoctorId = doctorId,
                MemberId = request.MemberId,
                Diagnosis = request.Diagnosis,
                Advice = request.Advice,
                MedicinesList = medicinesJson,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<PrescriptionsByDoctor>().AddAsync(prescription);

            // Tùy chọn: Có thể tự động kết thúc (End) Session luôn nếu bác sĩ kê đơn xong
            // session.Status = "Ended";
            // _unitOfWork.Repository<ConsultationSessions>().Update(session);

            await _unitOfWork.CompleteAsync();

            return ApiResponse<PrescriptionByDoctorDto>.Ok(MapToDto(prescription), "Kê đơn thuốc thành công.");
        }

        public async Task<ApiResponse<PrescriptionByDoctorDto>> GetByIdAsync(Guid prescriptionId, Guid currentUserId)
        {
            var prescription = (await _unitOfWork.Repository<PrescriptionsByDoctor>()
                .FindAsync(p => p.DigitalPrescriptionId == prescriptionId, "Doctor,Member")).FirstOrDefault();

            if (prescription == null)
                return ApiResponse<PrescriptionByDoctorDto>.Fail("Không tìm thấy đơn thuốc.", 404);

            if (!await CheckAccessAsync(prescription, currentUserId))
                return ApiResponse<PrescriptionByDoctorDto>.Fail("Bạn không có quyền xem đơn thuốc này.", 403);

            return ApiResponse<PrescriptionByDoctorDto>.Ok(MapToDto(prescription));
        }

        public async Task<ApiResponse<IEnumerable<PrescriptionByDoctorDto>>> GetBySessionIdAsync(Guid sessionId, Guid currentUserId)
        {
            var prescriptions = await _unitOfWork.Repository<PrescriptionsByDoctor>()
                .FindAsync(p => p.ConsultanSessionId == sessionId, "Doctor,Member");

            if (prescriptions.Any() && !await CheckAccessAsync(prescriptions.First(), currentUserId))
                return ApiResponse<IEnumerable<PrescriptionByDoctorDto>>.Fail("Bạn không có quyền xem thông tin của phiên khám này.", 403);

            var response = prescriptions.OrderByDescending(p => p.CreatedAt).Select(MapToDto);
            return ApiResponse<IEnumerable<PrescriptionByDoctorDto>>.Ok(response);
        }

        public async Task<ApiResponse<IEnumerable<PrescriptionByDoctorDto>>> GetByMemberIdAsync(Guid memberId, Guid currentUserId)
        {
            if (!await _currentUserService.CheckAccess(memberId, currentUserId))
                return ApiResponse<IEnumerable<PrescriptionByDoctorDto>>.Fail("Bạn không có quyền xem đơn thuốc của thành viên này.", 403);

            var prescriptions = await _unitOfWork.Repository<PrescriptionsByDoctor>()
                .FindAsync(p => p.MemberId == memberId, "Doctor,Member");

            var response = prescriptions.OrderByDescending(p => p.CreatedAt).Select(MapToDto);
            return ApiResponse<IEnumerable<PrescriptionByDoctorDto>>.Ok(response);
        }

        public async Task<ApiResponse<PrescriptionByDoctorDto>> UpdateAsync(Guid prescriptionId, Guid currentUserId, UpdatePrescriptionByDoctorRequest request)
        {
            var prescription = (await _unitOfWork.Repository<PrescriptionsByDoctor>()
                .FindAsync(p => p.DigitalPrescriptionId == prescriptionId, "Doctor,Member")).FirstOrDefault();

            if (prescription == null)
                return ApiResponse<PrescriptionByDoctorDto>.Fail("Không tìm thấy đơn thuốc.", 404);

            // Chỉ bác sĩ kê đơn mới được sửa
            if (prescription.Doctor == null || prescription.Doctor.UserId != currentUserId)
                return ApiResponse<PrescriptionByDoctorDto>.Fail("Bạn không có quyền sửa đơn thuốc này.", 403);

            prescription.Diagnosis = request.Diagnosis;
            prescription.Advice = request.Advice;
            prescription.MedicinesList = JsonSerializer.Serialize(request.Medicines);

            _unitOfWork.Repository<PrescriptionsByDoctor>().Update(prescription);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<PrescriptionByDoctorDto>.Ok(MapToDto(prescription), "Cập nhật đơn thuốc thành công.");
        }

        // ================= HELPER METHODS =================

        private async Task<bool> CheckAccessAsync(PrescriptionsByDoctor prescription, Guid currentUserId)
        {
            // Bác sĩ kê đơn được xem
            if (prescription.Doctor != null && prescription.Doctor.UserId == currentUserId) return true;

            // Bệnh nhân (hoặc người nhà quản lý hồ sơ) được xem
            return await _currentUserService.CheckAccess(prescription.MemberId, currentUserId);
        }

        private PrescriptionByDoctorDto MapToDto(PrescriptionsByDoctor p)
        {
            var medicinesList = new List<DigitalMedicineItemDto>();
            if (!string.IsNullOrEmpty(p.MedicinesList))
            {
                try
                {
                    medicinesList = JsonSerializer.Deserialize<List<DigitalMedicineItemDto>>(p.MedicinesList) ?? new List<DigitalMedicineItemDto>();
                }
                catch { /* Bỏ qua lỗi parse nếu db dính dữ liệu rác */ }
            }

            return new PrescriptionByDoctorDto
            {
                DigitalPrescriptionId = p.DigitalPrescriptionId,
                ConsultanSessionId = p.ConsultanSessionId,
                DoctorId = p.DoctorId,
                DoctorName = p.Doctor?.FullName ?? "Unknown",
                MemberId = p.MemberId,
                MemberName = p.Member?.FullName ?? "Unknown",
                Diagnosis = p.Diagnosis,
                Advice = p.Advice,
                Medicines = medicinesList,
                CreatedAt = p.CreatedAt
            };
        }
    }
}