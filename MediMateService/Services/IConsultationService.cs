using MediMateService.DTOs;

namespace MediMateService.Services
{
    public interface IConsultationService
    {
        Task<ConsultationSessionDto> GetByAppointmentIdAsync(Guid appointmentId, Guid userId);
        Task<ConsultationSessionDto> EndSessionAsync(Guid sessionId, Guid userId, EndConsultationDto request);
        Task<ConsultationSessionDto> AttachPrescriptionAsync(Guid sessionId, Guid userId, AttachPrescriptionDto request);
    }
}
