using MediMateService.DTOs;
using MediMateRepository.Repositories;
using MediMateService.Shared;

namespace MediMateService.Services.Implementations
{
    public class ConsultationService : IConsultationService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ConsultationService(
            IAppointmentRepository appointmentRepository,
            IDoctorRepository doctorRepository,
            IUnitOfWork unitOfWork)
        {
            _appointmentRepository = appointmentRepository;
            _doctorRepository = doctorRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ConsultationSessionDto> GetByAppointmentIdAsync(Guid appointmentId, Guid userId)
        {
            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
            if (appointment == null)
            {
                throw new NotFoundException("Không tìm thấy lịch hẹn.");
            }

            var doctor = await _doctorRepository.GetDoctorByIdAsync(appointment.DoctorId);
            var isDoctorOwner = doctor != null && doctor.UserId == userId;
            var member = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(appointment.MemberId);
            var isUserOwner = member?.UserId == userId;
            if (!isDoctorOwner && !isUserOwner)
            {
                throw new ForbiddenException("Bạn không có quyền xem phiên tư vấn này.");
            }

            var session = await _appointmentRepository.GetSessionByAppointmentIdAsync(appointmentId);
            if (session == null)
            {
                throw new NotFoundException("Không tìm thấy phiên tư vấn.");
            }

            return MapSession(session);
        }

        public async Task<ConsultationSessionDto> EndSessionAsync(Guid sessionId, Guid userId, EndConsultationDto request)
        {
            var session = await _appointmentRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                throw new NotFoundException("Không tìm thấy phiên tư vấn.");
            }

            var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);
            if (doctor == null || doctor.UserId != userId)
            {
                throw new ForbiddenException("Chỉ bác sĩ phụ trách mới được kết thúc phiên tư vấn.");
            }

            if (session.Status == "Ended")
            {
                throw new ConflictException("Phiên tư vấn đã kết thúc trước đó.");
            }

            session.EndedAt = request.EndedAt ?? DateTime.Now;
            session.Status = "Ended";
            await _appointmentRepository.UpdateSessionAsync(session);

            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(session.AppointmentId);
            if (appointment != null)
            {
                appointment.Status = "Completed";
                await _appointmentRepository.UpdateAppointmentAsync(appointment);
            }

            return MapSession(session);
        }

        public async Task<ConsultationSessionDto> AttachPrescriptionAsync(Guid sessionId, Guid userId, AttachPrescriptionDto request)
        {
            var session = await _appointmentRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                throw new NotFoundException("Không tìm thấy phiên tư vấn.");
            }

            var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);
            if (doctor == null || doctor.UserId != userId)
            {
                throw new ForbiddenException("Chỉ bác sĩ phụ trách mới được gắn đơn thuốc.");
            }

            var prefix = $"Prescription:{request.PrescriptionId}";
            session.DoctorNote = string.IsNullOrWhiteSpace(session.DoctorNote)
                ? prefix
                : $"{session.DoctorNote}; {prefix}";
            await _appointmentRepository.UpdateSessionAsync(session);

            return MapSession(session);
        }

        private static ConsultationSessionDto MapSession(MediMateRepository.Model.ConsultationSessions item)
        {
            return new ConsultationSessionDto
            {
                ConsultanSessionId = item.ConsultanSessionId,
                AppointmentId = item.AppointmentId,
                DoctorId = item.DoctorId,
                MemberId = item.MemberId,
                StartedAt = item.StartedAt,
                EndedAt = item.EndedAt,
                Status = item.Status,
                DoctorNote = item.DoctorNote
            };
        }
    }
}
