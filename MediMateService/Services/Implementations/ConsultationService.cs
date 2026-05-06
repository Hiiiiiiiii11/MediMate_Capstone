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
        private readonly IAgoraRecordingService _agoraRecordingService;

        public ConsultationService(
            IAppointmentRepository appointmentRepository,
            IDoctorRepository doctorRepository,
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IAgoraRecordingService agoraRecordingService)
        {
            _appointmentRepository = appointmentRepository;
            _doctorRepository = doctorRepository;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _agoraRecordingService = agoraRecordingService;
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

            var session = await _unitOfWork.Repository<ConsultationSessions>()
        .GetQueryable()
        .Include(c => c.Member)
        .Include(c => c.Appointment)
        .Include(c => c.Doctor).ThenInclude(d => d.User)
        .FirstOrDefaultAsync(s => s.AppointmentId == appointmentId);
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
                .Include(c => c.Appointment) // Quan trọng: Load lịch hẹn
                .Include(c => c.Doctor).ThenInclude(d => d.User) // Load thông tin bác sĩ
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

                // Nếu là lần đầu tiên bác sĩ join, và bệnh nhân chưa có trong đó -> gửi thông báo
                if (!session.DoctorJoined && !session.UserJoined)
                {
                    var member = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(session.MemberId);
                    if (member != null)
                    {
                        // [QUAN TRỌNG]: Tìm chủ hộ (Family Head) để lưu Notification
                        Guid? headUserId = member.UserId;
                        if (!headUserId.HasValue && member.FamilyId != null)
                        {
                            var familyManager = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetQueryable()
                                .AsNoTracking()
                                .FirstOrDefaultAsync(m => m.FamilyId == member.FamilyId && m.UserId != null);
                            headUserId = familyManager?.UserId;
                        }

                        if (headUserId.HasValue)
                        {
                            await _notificationService.SendNotificationAsync(
                                userId: headUserId.Value,
                                title: "👨‍⚕️ Bác sĩ đã vào phòng!",
                                message: "Bác sĩ đang đợi tư vấn trực tuyến cho bạn. Hãy tham gia ngay nhé!",
                                type: ConsultationSessionActionTypes.SESSION_STARTED,
                                referenceId: session.ConsultanSessionId
                            );
                        }
                        await _notificationService.SendNotificationAsync(
                            userId: null,
                            title: "👨‍⚕️ Bác sĩ đã vào phòng!",
                            message: "Bác sĩ đang đợi tư vấn trực tuyến cho bạn. Hãy tham gia ngay nhé!",
                            type: ConsultationSessionActionTypes.SESSION_STARTED,
                            referenceId: session.ConsultanSessionId,
                            memberId: member.MemberId
                        );
                    }
                }

                session.DoctorJoined = true;
            }
            else if (normalizedRole == "user" || normalizedRole == "member")
            {
                var member = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(session.MemberId);
                bool isPatientOrHead = false;

                if (member != null)
                {
                    if (member.UserId == userId || member.MemberId == userId)
                    {
                        isPatientOrHead = true;
                    }
                    else if (member.FamilyId.HasValue)
                    {
                        isPatientOrHead = await _unitOfWork.Repository<MediMateRepository.Model.Members>()
                            .GetQueryable()
                            .AnyAsync(m => m.FamilyId == member.FamilyId && (m.UserId == userId || m.MemberId == userId));
                    }
                }

                if (!isPatientOrHead)
                    throw new ForbiddenException("Bạn không có quyền tham gia phiên khám này.");

                // Nếu là lần đầu tiên bệnh nhân join, và bác sĩ chưa có trong đó -> gửi thông báo
                if (!session.UserJoined && !session.DoctorJoined)
                {
                    var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);
                    if (doctor != null)
                    {
                        await _notificationService.SendNotificationAsync(
                            userId: doctor.UserId,
                            title: "🙋‍♂️ Bệnh nhân đã có mặt!",
                            message: $"Bệnh nhân {member.FullName} đã tham gia phòng chờ tư vấn. Vui lòng tham gia để bắt đầu.",
                            type: ConsultationSessionActionTypes.SESSION_STARTED,
                            referenceId: session.ConsultanSessionId
                        );
                    }
                }

                session.UserJoined = true;
            }
            else
            {
                throw new BadRequestException("Role phải là 'user', 'member' hoặc 'doctor'.");
            }

            // Event-driven: Tự động chuyển sang InProgress khi cả 2 bên đã join
            if (session.UserJoined && session.DoctorJoined && session.Status != ConsultationSessionConstants.IN_PROGRESS)
            {
                session.Status = ConsultationSessionConstants.IN_PROGRESS;

                // Thông báo cho cả 2 bên
                var member = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(session.MemberId);
                var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);

                if (member != null)
                {
                    // [QUAN TRỌNG]: Tìm chủ hộ (Family Head) để lưu Notification
                    Guid? headUserId = member.UserId;
                    if (!headUserId.HasValue && member.FamilyId != null)
                    {
                        var familyManager = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetQueryable()
                            .AsNoTracking()
                            .FirstOrDefaultAsync(m => m.FamilyId == member.FamilyId && m.UserId != null);
                        headUserId = familyManager?.UserId;
                    }

                    if (headUserId.HasValue)
                    {
                        await _notificationService.SendNotificationAsync(
                            userId: headUserId.Value,
                            title: "📞 Phiên tư vấn đã bắt đầu!",
                            message: "Cả bác sĩ và bệnh nhân đã ở trong phòng. Cuộc tư vấn đang diễn ra.",
                            type: ConsultationSessionActionTypes.SESSION_IN_PROGRESS,
                            referenceId: session.ConsultanSessionId
                        );
                    }

                    await _notificationService.SendNotificationAsync(
                        userId: null,
                        title: "📞 Phiên tư vấn đã bắt đầu!",
                        message: "Cả bác sĩ và bệnh nhân đã ở trong phòng. Cuộc tư vấn đang diễn ra.",
                        type: ConsultationSessionActionTypes.SESSION_IN_PROGRESS,
                        referenceId: session.ConsultanSessionId,
                        memberId: member.MemberId
                    );
                }

                if (doctor != null)
                {
                    await _notificationService.SendNotificationAsync(
                        userId: doctor.UserId,
                        title: "📞 Phiên tư vấn đã bắt đầu!",
                        message: "Cả bác sĩ và bệnh nhân đã ở trong phòng. Cuộc tư vấn đang diễn ra.",
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
        // END SESSION (Bác sĩ hoặc Bệnh nhân đều được gọi)
        // ─────────────────────────────────────────────
        public async Task<ConsultationSessionDto> EndSessionAsync(Guid sessionId, Guid userId)
        {
            var session = await _appointmentRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
                throw new NotFoundException("Không tìm thấy phiên tư vấn.");

            var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);
            bool isDoctor = doctor != null && doctor.UserId == userId;

            var patientMember = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(session.MemberId);
            bool isPatient = patientMember != null && patientMember.UserId == userId;

            if (!isDoctor && !isPatient)
            {
                throw new ForbiddenException("Chỉ bác sĩ phụ trách hoặc bệnh nhân trực tiếp mới có quyền kết thúc phiên tư vấn.");
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
                // [NEW] CẬP NHẬT PHIẾU TRẢ TIỀN (PAYOUT) CHO PHÒNG KHÁM
                // =========================================================
                var payout = await _unitOfWork.Repository<MediMateRepository.Model.DoctorPayout>().GetQueryable()
                    .FirstOrDefaultAsync(p => p.AppointmentId == appointment.AppointmentId);

                if (payout != null)
                {
                    payout.ConsultationId = session.ConsultanSessionId;
                    payout.Status = "ReadyToPay";
                    _unitOfWork.Repository<MediMateRepository.Model.DoctorPayout>().Update(payout);

                    // Lấy tên Clinic thông qua ClinicDoctors
                    var clinicDoctor = await _unitOfWork.Repository<MediMateRepository.Model.ClinicDoctors>().GetQueryable()
                        .Include(cd => cd.Clinic)
                        .FirstOrDefaultAsync(cd => cd.DoctorId == session.DoctorId && cd.Status == "Active");

                    string clinicName = clinicDoctor?.Clinic?.Name ?? "Phòng khám";
                    string docName = doctor?.User?.FullName ?? "Unknown";

                    await _notificationService.SendNotificationToRoleAsync(
                        Roles.Admin,
                        "Yêu cầu thanh toán mới",
                        $"Bác sĩ {docName} thuộc {clinicName} vừa hoàn tất phiên khám {appointment.AppointmentId.ToString()[..8].ToUpper()} và có giao dịch cần thanh toán.",
                        "Warning"
                    );
                }
            }

            await _unitOfWork.CompleteAsync();


            // Thông báo cho các bên liên quan
            string enderName = isDoctor ? "Bác sĩ" : "Bệnh nhân";

            // Thông báo cho Bác sĩ (nếu bệnh nhân kết thúc) hoặc gửi thông báo chung
            if (doctor != null)
            {
                await _notificationService.SendNotificationAsync(
                    userId: doctor.UserId,
                    title: "✅ Phiên tư vấn đã kết thúc",
                    message: $"{enderName} đã kết thúc phiên tư vấn. Hệ thống đã ghi nhận doanh thu phiên khám.",
                    type: ConsultationSessionActionTypes.SESSION_ENDED,
                    referenceId: session.ConsultanSessionId
                );
            }

            // Thông báo cho bệnh nhân (chủ hộ)
            if (patientMember != null)
            {
                Guid? headUserId = patientMember.UserId;
                if (!headUserId.HasValue && patientMember.FamilyId != null)
                {
                    var familyManager = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetQueryable()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.FamilyId == patientMember.FamilyId && m.UserId != null);
                    headUserId = familyManager?.UserId;
                }

                if (headUserId.HasValue)
                {
                    await _notificationService.SendNotificationAsync(
                        userId: headUserId.Value,
                        title: "✅ Phiên tư vấn đã kết thúc",
                        message: $"{enderName} đã kết thúc phiên tư vấn.",
                        type: ConsultationSessionActionTypes.SESSION_ENDED,
                        referenceId: session.ConsultanSessionId
                    );
                }
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
        // REQUEST END SESSION & RETRY RECORDING
        // ─────────────────────────────────────────────
        public async Task<bool> RequestEndSessionAsync(Guid sessionId, Guid userId)
        {
            var session = await _appointmentRepository.GetSessionByIdAsync(sessionId);
            if (session == null) throw new NotFoundException("Không tìm thấy phiên tư vấn.");

            var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);
            if (doctor == null || doctor.UserId != userId)
                throw new ForbiddenException("Chỉ bác sĩ phụ trách mới có quyền yêu cầu kết thúc phiên.");

            await _notificationService.SendNotificationAsync(
                userId: null,
                title: "Yêu cầu kết thúc",
                message: "Bác sĩ đã yêu cầu kết thúc phiên khám.",
                type: ConsultationSessionActionTypes.SESSION_REQUEST_END,
                referenceId: session.ConsultanSessionId,
                memberId: session.MemberId
            );

            return true;
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
                MemberAvatar = item.Member?.AvatarUrl,

                // Map thông tin từ Appointment
                AppointmentDate = item.Appointment?.AppointmentDate ?? DateTime.MinValue,
                AppointmentTime = item.Appointment?.AppointmentTime.ToString(@"hh\:mm"),
                AppointmentStatus = item.Appointment?.Status,

                // Map thông tin bác sĩ (thông qua bảng Doctor -> User)
                DoctorName = item.Doctor?.User?.FullName,
                DoctorAvatar = item.Doctor?.User?.AvatarUrl,

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
