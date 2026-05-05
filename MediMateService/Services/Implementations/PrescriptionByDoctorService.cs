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
            await _unitOfWork.CompleteAsync();

            // ═══════════════════════════════════════════════════════════
            // [PHASE 4] TỰ ĐỘNG GỬI TIN NHẮN HỆ THỐNG VÀO CHAT
            // Sau khi kê đơn, sinh System Message vào khung chat của phiên
            // ═══════════════════════════════════════════════════════════
            try
            {
                var member = await _unitOfWork.Repository<Members>().GetByIdAsync(request.MemberId);
                // Tái sử dụng biến doctor đã có từ validation phía trên

                // Xây dựng nội dung tin nhắn dạng text đầy đủ
                var medicineLines = new System.Text.StringBuilder();
                var medicines = new List<DigitalMedicineItemDto>();
                try { medicines = System.Text.Json.JsonSerializer.Deserialize<List<DigitalMedicineItemDto>>(medicinesJson) ?? new(); } catch { }

                for (int i = 0; i < medicines.Count; i++)
                {
                    var m = medicines[i];
                    medicineLines.AppendLine($"  {i + 1}. {m.MedicineName} ({m.Dosage}) - {m.Quantity} {m.Unit}");
                    if (!string.IsNullOrWhiteSpace(m.Instructions))
                        medicineLines.AppendLine($"     → {m.Instructions}");
                }

                var messageContent = $"""
📋 ĐƠN THUỐC ĐIỆN TỬ
━━━━━━━━━━━━━━━━━━━━━━━━
👤 Bệnh nhân : {member?.FullName ?? "Không rõ"}
👨‍⚕️ Bác sĩ     : {doctor?.FullName ?? "Không rõ"}
📅 Ngày kê   : {DateTime.Now:dd/MM/yyyy HH:mm}
━━━━━━━━━━━━━━━━━━━━━━━━
🔍 Chẩn đoán : {request.Diagnosis}

💊 DANH SÁCH THUỐC:
{medicineLines}
━━━━━━━━━━━━━━━━━━━━━━━━
📝 Lời dặn: {request.Advice}
""";

                var systemMessage = new ChatDoctorMessages
                {
                    ChatDoctorMessageId = Guid.NewGuid(),
                    ConsultanSessionId = request.ConsultanSessionId,
                    SenderId = doctorId,
                    Type = SenderType.Doctor,
                    Content = messageContent,  // [FIX] Dùng đúng nội dung đơn thuốc đã xây dựng
                    AttachmentUrl = null,
                    IsRead = false,
                    SendAt = DateTime.Now
                };

                await _unitOfWork.Repository<ChatDoctorMessages>().AddAsync(systemMessage);
                await _unitOfWork.CompleteAsync();
            }
            catch
            {
                // Không để lỗi gửi chat làm gián đoạn luồng chính
            }

            return ApiResponse<PrescriptionByDoctorDto>.Ok(MapToDto(prescription), "Kê đơn thuốc thành công và đã gửi vào chat.");
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

            if (prescription.IsLocked)
                return ApiResponse<PrescriptionByDoctorDto>.Fail("Đơn thuốc này đã bị khóa và không thể chỉnh sửa.", 400);

            // Chỉ bác sĩ kê đơn mới được sửa
            if (prescription.Doctor == null || prescription.Doctor.UserId != currentUserId)
                return ApiResponse<PrescriptionByDoctorDto>.Fail("Bạn không có quyền sửa đơn thuốc này.", 403);

            // Cập nhật từng trường nếu được truyền lên
            if (request.Diagnosis != null)
                prescription.Diagnosis = request.Diagnosis;
                
            if (request.Advice != null)
                prescription.Advice = request.Advice;
                
            if (request.Medicines != null)
                prescription.MedicinesList = JsonSerializer.Serialize(request.Medicines);

            if (!string.IsNullOrEmpty(request.Status))
            {
                prescription.Status = request.Status;
                if (request.Status == "Completed" || request.Status == "Cancelled")
                {
                    prescription.IsLocked = true;
                }
            }

            prescription.UpdatedAt = DateTime.Now;

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
                MemberDateOfBirth = p.Member?.DateOfBirth,
                MemberGender = p.Member?.Gender,
                Diagnosis = p.Diagnosis,
                Advice = p.Advice,
                Medicines = medicinesList,
                Status = p.Status,
                IsLocked = p.IsLocked,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };
        }
    }
}