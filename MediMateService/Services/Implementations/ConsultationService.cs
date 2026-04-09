using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Microsoft.EntityFrameworkCore;
using Share.Constants;

namespace MediMateService.Services.Implementations
{
    public class ConsultationService : IConsultationService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public ConsultationService(
            IAppointmentRepository appointmentRepository,
            IDoctorRepository doctorRepository,
            IUnitOfWork unitOfWork,
            INotificationService notificationService)
        {
            _appointmentRepository = appointmentRepository;
            _doctorRepository = doctorRepository;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        // ─────────────────────────────────────────────
        // GET SESSION BY APPOINTMENT
        // ─────────────────────────────────────────────
        public async Task<ConsultationSessionDto> GetByAppointmentIdAsync(Guid appointmentId, Guid userId)
        {
            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
            if (appointment == null)
                throw new NotFoundException("Không tìm thấy lịch hẹn.");

            var doctor = await _doctorRepository.GetDoctorByIdAsync(appointment.DoctorId);
            var isDoctorOwner = doctor != null && doctor.UserId == userId;

            // 2. Lấy thông tin hồ sơ bệnh nhân (Người chủ của lịch hẹn)
            var patientMember = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(appointment.MemberId);

            if (patientMember == null) throw new NotFoundException("Không tìm thấy thông tin bệnh nhân.");

            // 3. Kiểm tra quyền truy cập dựa trên Gia đình
            bool hasAccess = false;

            if (isDoctorOwner)
            {
                hasAccess = true;
            }
            else if (patientMember.FamilyId.HasValue)
            {
                // Nếu bệnh nhân thuộc một gia đình: 
                // Cho phép nếu người gọi (userId) là bất kỳ thành viên nào trong gia đình đó
                hasAccess = (await _unitOfWork.Repository<MediMateRepository.Model.Members>()
                    .FindAsync(m => m.FamilyId == patientMember.FamilyId
                                 && (m.UserId == userId || m.MemberId == userId))).Any();
            }
            else
            {
                // Nếu bệnh nhân chưa vào gia đình (hồ sơ cá nhân):
                // Chỉ chính họ mới được xem
                hasAccess = patientMember.UserId == userId || patientMember.MemberId == userId;
            }

            // 4. Quyết định cuối cùng
            if (!hasAccess)
            {
                throw new ForbiddenException("Bạn không có quyền xem phiên tư vấn này.");
            }

            var session = await _appointmentRepository.GetSessionByAppointmentIdAsync(appointmentId);
            if (session == null)
                throw new NotFoundException("Không tìm thấy phiên tư vấn.");

            return MapSession(session);
        }

        // ─────────────────────────────────────────────
        // LIST SESSIONS FOR CURRENT DOCTOR (/me)
        // ─────────────────────────────────────────────
        public async Task<IReadOnlyList<ConsultationSessionDto>> GetSessionsForCurrentDoctorAsync(Guid userId)
        {
            var doctor = (await _unitOfWork.Repository<Doctors>()
                .FindAsync(d => d.UserId == userId)).FirstOrDefault();
            if (doctor == null)
                throw new ForbiddenException("Tài khoản này không phải bác sĩ hoặc chưa có hồ sơ bác sĩ.");

            var sessions = await _unitOfWork.Repository<ConsultationSessions>()
                .GetQueryable()
                .Include(c => c.Member)
                .AsNoTracking()
                .Where(s => s.DoctorId == doctor.DoctorId)
                .OrderByDescending(s => s.StartedAt)
                .ToListAsync();

            return sessions.Select(MapSession).ToList();
        }

        // ─────────────────────────────────────────────
        // JOIN SESSION (event-driven → InProgress)
        // ─────────────────────────────────────────────
        public async Task<ConsultationSessionDto> JoinSessionAsync(Guid sessionId, Guid userId, string role)
        {
            var session = await _appointmentRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
                throw new NotFoundException("Không tìm thấy phiên tư vấn.");

            if (session.Status == ConsultationSessionConstants.ENDED)
                throw new ConflictException("Phiên tư vấn đã kết thúc, không thể tham gia.");

            var normalizedRole = role.ToLower().Trim();

            if (normalizedRole == "doctor")
            {
                var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);
                if (doctor == null || doctor.UserId != userId)
                    throw new ForbiddenException("Bạn không phải bác sĩ phụ trách phiên này.");

                session.DoctorJoined = true;
            }
            else if (normalizedRole == "user")
            {
                var member = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(session.MemberId);
                if (member == null || member.UserId != userId)
                    throw new ForbiddenException("Bạn không phải bệnh nhân của phiên này.");

                session.UserJoined = true;
            }
            else
            {
                throw new BadRequestException("Role phải là 'user' hoặc 'doctor'.");
            }

            // Event-driven: Tự động chuyển sang InProgress khi cả 2 bên đã join
            if (session.UserJoined && session.DoctorJoined)
            {
                session.Status = ConsultationSessionConstants.IN_PROGRESS;

                // Thông báo cho cả 2 bên
                var member = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(session.MemberId);
                var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);

                if (member?.UserId != null)
                {
                    await _notificationService.SendNotificationAsync(
                        userId: member.UserId.Value,
                        title: "📞 Phiên tư vấn đã bắt đầu!",
                        message: "Bác sĩ đã tham gia. Cuộc tư vấn đang diễn ra.",
                        type: ConsultationSessionActionTypes.SESSION_IN_PROGRESS,
                        referenceId: session.ConsultanSessionId
                    );
                }

                if (doctor != null)
                {
                    await _notificationService.SendNotificationAsync(
                        userId: doctor.UserId,
                        title: "📞 Phiên tư vấn đã bắt đầu!",
                        message: "Bệnh nhân đã tham gia. Cuộc tư vấn đang diễn ra.",
                        type: ConsultationSessionActionTypes.SESSION_IN_PROGRESS,
                        referenceId: session.ConsultanSessionId
                    );
                }
            }

            await _appointmentRepository.UpdateSessionAsync(session);
            await _unitOfWork.CompleteAsync();

            return MapSession(session);
        }

        // ─────────────────────────────────────────────
        // MARK DOCTOR LATE
        // ─────────────────────────────────────────────
        public async Task<ConsultationSessionDto> MarkDoctorLateAsync(Guid sessionId, Guid userId, int lateMinutes)
        {
            var session = await _appointmentRepository.GetSessionByIdAsync(sessionId);
            if (session == null) throw new NotFoundException("Không tìm thấy phiên tư vấn.");

            var patientMember = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(session.MemberId);

            // Quyền: Hoặc là chính MemberId đó, hoặc là chủ sở hữu (UserId) của Member đó
            bool canMark = session.MemberId == userId || (patientMember != null && patientMember.UserId == userId);

            if (!canMark) throw new ForbiddenException("Bạn không có quyền thực hiện hành động này.");

            var lateNote = $"Bác sĩ đi trễ {lateMinutes} phút";
            session.Note = string.IsNullOrWhiteSpace(session.Note) ? lateNote : $"{session.Note}; {lateNote}";

            await _appointmentRepository.UpdateSessionAsync(session);
            await _unitOfWork.CompleteAsync();

            return MapSession(session);
        }

        // ─────────────────────────────────────────────
        // CANCEL NO-SHOW (User huỷ vì không gặp bác sĩ)
        // ─────────────────────────────────────────────
        public async Task<ConsultationSessionDto> CancelNoShowAsync(Guid sessionId, Guid userId)
        {
            var session = await _appointmentRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
                throw new NotFoundException("Không tìm thấy phiên tư vấn.");

            // Chỉ bệnh nhân mới được cancel no-show
            var member = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(session.MemberId);
            if (session.MemberId != userId)
            {
                throw new ForbiddenException("Chỉ hồ sơ bệnh nhân trực tiếp mới được huỷ phiên do bác sĩ không đến.");
            }

            if (session.Status == ConsultationSessionConstants.ENDED)
                throw new ConflictException("Phiên tư vấn đã kết thúc trước đó.");

            if (session.DoctorJoined)
                throw new BadRequestException("Bác sĩ đã tham gia phiên này. Không thể huỷ theo lý do không gặp bác sĩ.");

            session.Status = ConsultationSessionConstants.ENDED;
            session.EndedAt = DateTime.Now;
            session.Note = "Khách huỷ vì lý do không gặp bác sĩ";

            await _appointmentRepository.UpdateSessionAsync(session);

            // Cập nhật Appointment → Cancelled
            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(session.AppointmentId);
            if (appointment != null)
            {
                appointment.Status = AppointmentConstants.CANCELLED;
                appointment.CancelReason = "Bệnh nhân huỷ do bác sĩ không tham gia đúng giờ";
                await _appointmentRepository.UpdateAppointmentAsync(appointment);

                // Hoàn trả lượt khám cho gia đình
                if (member.FamilyId != null)
                {
                    var currentDate = DateOnly.FromDateTime(DateTime.Now);
                    var activeSubscription = (await _unitOfWork.Repository<MediMateRepository.Model.FamilySubscriptions>()
                        .FindAsync(s => s.FamilyId == member.FamilyId
                                        && s.Status == "Active"
                                        && s.EndDate >= currentDate)).FirstOrDefault();

                    if (activeSubscription != null)
                    {
                        activeSubscription.RemainingConsultantCount += 1;
                        _unitOfWork.Repository<MediMateRepository.Model.FamilySubscriptions>().Update(activeSubscription);
                    }
                }
            }

            await _unitOfWork.CompleteAsync();

            // Thông báo cho bác sĩ
            var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);
            if (doctor != null)
            {
                await _notificationService.SendNotificationAsync(
                    userId: doctor.UserId,
                    title: "❌ Bệnh nhân đã huỷ phiên tư vấn",
                    message: $"Bệnh nhân {member.FullName} đã huỷ phiên tư vấn vì lý do không gặp được bác sĩ.",
                    type: ConsultationSessionActionTypes.SESSION_ENDED,
                    referenceId: session.ConsultanSessionId
                );
            }

            return MapSession(session);
        }

        // ─────────────────────────────────────────────
        // END SESSION BY USER (chỉ User mới được gọi)
        // ─────────────────────────────────────────────
        public async Task<ConsultationSessionDto> EndSessionByUserAsync(Guid sessionId, Guid callerMemberId)
        {
            var session = await _appointmentRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
                throw new NotFoundException("Không tìm thấy phiên tư vấn.");
            var member = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(session.MemberId);
            // Xác minh người gọi là bệnh nhân (User) của phiên này
            if (session.MemberId != callerMemberId)
            {
                throw new ForbiddenException("Chỉ bệnh nhân trực tiếp của lịch hẹn này mới có quyền kết thúc phiên tư vấn.");
            }

            if (session.Status == ConsultationSessionConstants.ENDED)
                throw new ConflictException("Phiên tư vấn đã kết thúc trước đó.");

            session.Status = ConsultationSessionConstants.ENDED;
            session.EndedAt = DateTime.Now;

            await _appointmentRepository.UpdateSessionAsync(session);

            // Cập nhật Appointment → Completed
            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(session.AppointmentId);
            if (appointment != null)
            {
                appointment.Status = AppointmentConstants.COMPLETED;
                await _appointmentRepository.UpdateAppointmentAsync(appointment);

                // =========================================================
                // [NEW] TỰ ĐỘNG TẠO PHIẾU TRẢ TIỀN (PAYOUT) CHO BÁC SĨ
                // =========================================================
                var activeRate = await _unitOfWork.Repository<MediMateRepository.Model.DoctorPayoutRate>().GetQueryable()
                    .Where(r => r.IsActive)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();

                if (activeRate != null)
                {
                    var payout = new MediMateRepository.Model.DoctorPayout
                    {
                        PayoutId = Guid.NewGuid(),
                        ConsultationId = session.ConsultanSessionId,
                        RateId = activeRate.RateId,
                        Amount = activeRate.AmountPerSession,
                        Status = "Pending",
                        CalculatedAt = DateTime.Now
                    };

                    await _unitOfWork.Repository<MediMateRepository.Model.DoctorPayout>().AddAsync(payout);
                }
            }

            await _unitOfWork.CompleteAsync();

            var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);
            if (doctor != null)
            {
                await _notificationService.SendNotificationAsync(
                    userId: doctor.UserId,
                    title: "✅ Bệnh nhân đã kết thúc phiên",
                    message: $"Bệnh nhân {member.FullName} đã kết thúc phiên tư vấn. Hệ thống đã ghi nhận doanh thu phiên khám.",
                    type: ConsultationSessionActionTypes.SESSION_ENDED,
                    referenceId: session.ConsultanSessionId
                );
            }

            return MapSession(session);
        }
        // ─────────────────────────────────────────────
        // ATTACH PRESCRIPTION (chỉ bác sĩ)
        // ─────────────────────────────────────────────
        public async Task<ConsultationSessionDto> AttachPrescriptionAsync(Guid sessionId, Guid userId, AttachPrescriptionDto request)
        {
            var session = await _appointmentRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
                throw new NotFoundException("Không tìm thấy phiên tư vấn.");

            var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);
            if (doctor == null || doctor.UserId != userId)
                throw new ForbiddenException("Chỉ bác sĩ phụ trách mới được gắn đơn thuốc.");

            var prefix = $"Prescription:{request.PrescriptionId}";
            session.DoctorNote = string.IsNullOrWhiteSpace(session.DoctorNote)
                ? prefix
                : $"{session.DoctorNote}; {prefix}";

            await _appointmentRepository.UpdateSessionAsync(session);
            await _unitOfWork.CompleteAsync();

            return MapSession(session);
        }

        // ─────────────────────────────────────────────
        // HELPER: MAP TO DTO
        // ─────────────────────────────────────────────
        private static ConsultationSessionDto MapSession(ConsultationSessions item)
        {
            return new ConsultationSessionDto
            {
                ConsultanSessionId = item.ConsultanSessionId,
                AppointmentId = item.AppointmentId,
                DoctorId = item.DoctorId,
                MemberId = item.MemberId,
                MemberName = item.Member?.FullName,
                StartedAt = item.StartedAt,
                EndedAt = item.EndedAt,
                Status = item.Status,
                UserJoined = item.UserJoined,
                DoctorJoined = item.DoctorJoined,
                Note = item.Note,
                DoctorNote = item.DoctorNote
            };
        }
    }
}
