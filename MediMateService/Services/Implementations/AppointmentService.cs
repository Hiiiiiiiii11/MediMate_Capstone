using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Share.Constants;

namespace MediMateService.Services.Implementations
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IDoctorRepository doctorRepository,
            IUnitOfWork unitOfWork)
        {
            _appointmentRepository = appointmentRepository;
            _doctorRepository = doctorRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<AppointmentDto> CreateAppointmentAsync(Guid userId, CreateAppointmentDto request)
        {
            if (!request.IsPremiumUser)
            {
                throw new ForbiddenException("Tính năng đặt lịch khám online yêu cầu tài khoản Premium.");
            }

            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(request.MemberId);
            if (member == null)
            {
                throw new NotFoundException("Không tìm thấy hồ sơ thành viên.");
            }
            if (member.UserId != userId)
            {
                throw new ForbiddenException("Bạn không có quyền đặt lịch cho hồ sơ này.");
            }

            var doctor = await _doctorRepository.GetDoctorByIdAsync(request.DoctorId);
            if (doctor == null || !string.Equals(doctor.Status, DoctorStatuses.Approved, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotFoundException("Không tìm thấy bác sĩ phù hợp để đặt lịch.");
            }

            var availability = await _doctorRepository.GetAvailabilityByIdAsync(request.DoctorId, request.AvailabilityId);
            if (availability == null || !availability.IsActive)
            {
                throw new NotFoundException("Không tìm thấy khung giờ làm việc khả dụng.");
            }

            var appointment = new Appointments
            {
                AppointmentId = Guid.NewGuid(),
                DoctorId = request.DoctorId,
                MemberId = request.MemberId,
                AvailabilityId = request.AvailabilityId,
                AppointmentDate = request.AppointmentDate,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            await _appointmentRepository.AddAppointmentAsync(appointment);

            var session = new ConsultationSessions
            {
                ConsultanSessionId = Guid.NewGuid(),
                AppointmentId = appointment.AppointmentId,
                DoctorId = appointment.DoctorId,
                MemberId = appointment.MemberId,
                StartedAt = appointment.AppointmentDate,
                EndedAt = null,
                Status = "Active",
                DoctorNote = null
            };
            await _appointmentRepository.AddSessionAsync(session);

            return MapAppointment(appointment);
        }

        public async Task<AppointmentDto> CancelAppointmentAsync(Guid appointmentId, Guid userId, CancelAppointmentDto request)
        {
            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
            if (appointment == null)
            {
                throw new NotFoundException("Không tìm thấy lịch hẹn.");
            }

            var doctor = await _doctorRepository.GetDoctorByIdAsync(appointment.DoctorId);
            var isDoctorOwner = doctor != null && doctor.UserId == userId;
            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(appointment.MemberId);
            var isUserOwner = member?.UserId == userId;
            if (!isDoctorOwner && !isUserOwner)
            {
                throw new ForbiddenException("Bạn không có quyền hủy lịch hẹn này.");
            }

            if (appointment.Status == "Cancelled")
            {
                throw new ConflictException("Lịch hẹn đã được hủy trước đó.");
            }

            appointment.Status = "Cancelled";
            appointment.CancelReason = request.Reason?.Trim();
            await _appointmentRepository.UpdateAppointmentAsync(appointment);

            return MapAppointment(appointment);
        }

        public async Task<List<AppointmentDto>> GetAppointmentsAsync(Guid userId)
        {
            var doctor = (await _doctorRepository.GetAllDoctorsAsync()).FirstOrDefault(d => d.UserId == userId);
            var doctorAppointments = doctor == null
                ? new List<Appointments>()
                : await _appointmentRepository.GetAppointmentsByDoctorIdAsync(doctor.DoctorId);

            var userMembers = await _unitOfWork.Repository<Members>().FindAsync(m => m.UserId == userId);
            var memberAppointments = new List<Appointments>();
            foreach (var member in userMembers)
            {
                var items = await _appointmentRepository.GetAppointmentsByMemberIdAsync(member.MemberId);
                memberAppointments.AddRange(items);
            }

            var merged = memberAppointments
                .Concat(doctorAppointments)
                .GroupBy(a => a.AppointmentId)
                .Select(g => g.First())
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();

            return merged.Select(MapAppointment).ToList();
        }

        private static AppointmentDto MapAppointment(Appointments item)
        {
            return new AppointmentDto
            {
                AppointmentId = item.AppointmentId,
                DoctorId = item.DoctorId,
                MemberId = item.MemberId,
                AvailabilityId = item.AvailabilityId,
                AppointmentDate = item.AppointmentDate,
                Status = item.Status,
                CancelReason = item.CancelReason,
                CreatedAt = item.CreatedAt
            };
        }
    }
}
