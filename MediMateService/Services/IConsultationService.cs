using MediMateService.DTOs;

namespace MediMateService.Services
{
    public interface IConsultationService
    {
        Task<ConsultationSessionDto> GetByAppointmentIdAsync(Guid appointmentId, Guid userId);

        /// <summary>User hoặc Doctor join session. Khi cả 2 join → tự động chuyển sang InProgress.</summary>
        Task<ConsultationSessionDto> JoinSessionAsync(Guid sessionId, Guid userId, string role);

        /// <summary>Ghi nhận bác sĩ trễ X phút vào field Note.</summary>
        Task<ConsultationSessionDto> MarkDoctorLateAsync(Guid sessionId, Guid userId, int lateMinutes);

        /// <summary>User huỷ phiên vì không gặp bác sĩ (no-show). Note được ghi tự động.</summary>
        Task<ConsultationSessionDto> CancelNoShowAsync(Guid sessionId, Guid userId);

        /// <summary>Chỉ User (Member) mới được kết thúc phiên meet.</summary>
        Task<ConsultationSessionDto> EndSessionByUserAsync(Guid sessionId, Guid userId);

        Task<ConsultationSessionDto> AttachPrescriptionAsync(Guid sessionId, Guid userId, AttachPrescriptionDto request);
    }
}
