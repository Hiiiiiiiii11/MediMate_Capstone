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
                                title: "Bác sĩ đã vào phòng!",
                                message: "Bác sĩ đang đợi tư vấn trực tuyến cho bạn. Hãy tham gia ngay nhé!",
                                type: ConsultationSessionActionTypes.SESSION_STARTED,
                                referenceId: session.ConsultanSessionId
                            );
                        }
                        await _notificationService.SendNotificationAsync(
                            userId: null,
                            title: "Bác sĩ đã vào phòng!",
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
                            title: "Bệnh nhân đã có mặt!",
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
                            title: "Phiên tư vấn đã bắt đầu!",
                            message: "Cả bác sĩ và bệnh nhân đã ở trong phòng. Cuộc tư vấn đang diễn ra.",
                            type: ConsultationSessionActionTypes.SESSION_IN_PROGRESS,
                            referenceId: session.ConsultanSessionId
                        );
                    }

                    await _notificationService.SendNotificationAsync(
                        userId: null,
                        title: "Phiên tư vấn đã bắt đầu!",
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
                        title: "Phiên tư vấn đã bắt đầu!",
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
            bool canMark = false;
            if (patientMember != null)
            {
                if (patientMember.UserId == userId || patientMember.MemberId == userId)
                {
                    canMark = true;
                }
                else if (patientMember.FamilyId.HasValue)
                {
                    canMark = await _unitOfWork.Repository<MediMateRepository.Model.Members>()
                        .GetQueryable()
                        .AnyAsync(m => m.FamilyId == patientMember.FamilyId && (m.UserId == userId || m.MemberId == userId));
                }
            }

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
            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(session.AppointmentId);
            if (appointment == null)
                throw new NotFoundException("Không tìm thấy lịch hẹn liên quan.");

            // 2. KIỂM TRA LOGIC 10 PHÚT
            var scheduledStartTime = appointment.AppointmentDate.Date.Add(appointment.AppointmentTime);
            var allowCancelTime = scheduledStartTime.AddMinutes(10); // Mốc được phép hủy

            if (DateTime.Now < allowCancelTime)
            {
                var waitTime = (allowCancelTime - DateTime.Now).TotalMinutes;
                throw new BadRequestException(
                    $"Bạn chỉ có thể báo lỗi bác sĩ vắng mặt sau {allowCancelTime:HH:mm} (10 phút kể từ khi bắt đầu). " +
                    $"Vui lòng đợi thêm {Math.Ceiling(waitTime)} phút nữa để hỗ trợ bác sĩ.");
            }

            // 1. Kiểm tra quyền (Chỉ bệnh nhân hoặc chủ hộ mới được cancel no-show)
            var member = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetByIdAsync(session.MemberId);
            bool canCancel = false;

            if (member != null)
            {
                if (member.UserId == userId || member.MemberId == userId)
                {
                    canCancel = true;
                }
                else if (member.FamilyId.HasValue)
                {
                    canCancel = await _unitOfWork.Repository<MediMateRepository.Model.Members>()
                        .GetQueryable()
                        .AnyAsync(m => m.FamilyId == member.FamilyId && (m.UserId == userId || m.MemberId == userId));
                }
            }

            if (!canCancel)
            {
                throw new ForbiddenException("Chỉ hồ sơ bệnh nhân trực tiếp hoặc chủ hộ gia đình mới được huỷ phiên do bác sĩ không đến.");
            }

            // 2. Kiểm tra trạng thái hợp lệ để hủy No-show
            if (session.Status == ConsultationSessionConstants.ENDED)
                throw new ConflictException("Phiên tư vấn đã kết thúc trước đó.");

            if (session.DoctorJoined)
                throw new BadRequestException("Bác sĩ đã tham gia phiên này. Không thể huỷ theo lý do không gặp bác sĩ.");

            // 3. Cập nhật Session
            session.Status = ConsultationSessionConstants.ENDED;
            session.EndedAt = DateTime.Now;
            session.Note = "Khách huỷ vì lý do bác sĩ không xuất hiện (No-show)";

            await _appointmentRepository.UpdateSessionAsync(session);

            // 4. Cập nhật Appointment & Xử lý Tài chính (Refund)
            if (appointment != null)
            {
                appointment.Status = AppointmentConstants.CANCELLED;
                appointment.CancelReason = "Bệnh nhân huỷ do bác sĩ không tham gia đúng giờ (No-show)";

                // ── LOGIC HOÀN TIỀN (REFUND) ──
                if (appointment.PaymentStatus == "Paid")
                {
                    // Chuyển trạng thái sang Refunded để Admin đối soát trong Flow 6
                    appointment.PaymentStatus = "Refunded";

                    // Tìm và hủy Payout đang Hold của bác sĩ/phòng khám (nếu có)
                    var payout = await _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                        .FirstOrDefaultAsync(p => p.AppointmentId == appointment.AppointmentId && p.Status == "Hold");

                    if (payout != null)
                    {
                        payout.Status = "Cancelled";
                        _unitOfWork.Repository<DoctorPayout>().Update(payout);
                    }

                    // Gửi thông báo cho Admin hệ thống để thực hiện chuyển khoản hoàn tiền
                    await _notificationService.SendNotificationToRoleAsync(
                        Roles.Admin,
                        "Yêu cầu hoàn tiền (No-show)",
                        $"Lịch hẹn {appointment.AppointmentId.ToString()[..8].ToUpper()} bị hủy do bác sĩ vắng mặt. Cần hoàn tiền gấp cho bệnh nhân.",
                        "Warning"
                    );
                }

                await _appointmentRepository.UpdateAppointmentAsync(appointment);
            }

            await _unitOfWork.CompleteAsync();

            // 5. Gửi thông báo cho các bên

            // Tìm chủ hộ để báo cáo tài chính/hủy lịch
            Guid? headUserId = member?.UserId;
            if (!headUserId.HasValue && member?.FamilyId != null)
            {
                var familyManager = await _unitOfWork.Repository<MediMateRepository.Model.Members>().GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.FamilyId == member.FamilyId && m.UserId != null);
                headUserId = familyManager?.UserId;
            }

            // Thông báo cho Bác sĩ (Báo lỗi vắng mặt)
            var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);
            if (doctor != null)
            {
                await _notificationService.SendNotificationAsync(
                    userId: doctor.UserId,
                    title: "Bạn đã bỏ lỡ phiên tư vấn",
                    message: $"Lịch hẹn của bệnh nhân {member?.FullName} đã bị hủy vì bạn không tham gia đúng giờ. Hệ thống đã ghi nhận lỗi No-show.",
                    type: ConsultationSessionActionTypes.SESSION_ENDED,
                    referenceId: session.ConsultanSessionId
                );
            }

            // Thông báo cho Bệnh nhân/Chủ hộ
            if (headUserId.HasValue)
            {
                string refundMsg = appointment?.PaymentStatus == "Refunded"
                    ? " Hệ thống đang thực hiện quy trình hoàn tiền cho bạn."
                    : "";

                await _notificationService.SendNotificationAsync(
                    userId: headUserId.Value,
                    title: "Đã hủy phiên tư vấn (Bác sĩ vắng mặt)",
                    message: $"Phiên khám của {member?.FullName} đã được hủy thành công do bác sĩ không có mặt.{refundMsg}",
                    type: ConsultationSessionActionTypes.SESSION_ENDED,
                    referenceId: session.ConsultanSessionId
                );

                // Kiểm tra xem User đã có tài khoản ngân hàng để nhận tiền chưa
                var hasBank = await _unitOfWork.Repository<UserBankAccount>().GetQueryable()
                    .AnyAsync(b => b.UserId == headUserId.Value);

                if (appointment?.PaymentStatus == "Refunded" && !hasBank)
                {
                    await _notificationService.SendNotificationAsync(
                        userId: headUserId.Value,
                        title: "Cần cập nhật thông tin ngân hàng",
                        message: "Lịch hẹn của bạn sẽ được hoàn tiền, nhưng bạn chưa có thông tin ngân hàng trong hồ sơ. Vui lòng cập nhật ngay.",
                        type: "BANKING_INFO_MISSING",
                        referenceId: appointment.AppointmentId
                    );
                }
            }

            return MapSession(session);
        }

        // ─────────────────────────────────────────────
        // END SESSION (Bác sĩ hoặc Bệnh nhân đều được gọi)
        // ─────────────────────────────────────────────
        public async Task<ConsultationSessionDto> EndSessionAsync(Guid sessionId, Guid? userId)
        {
            var session = await _appointmentRepository.GetSessionByIdAsync(sessionId);
            if (session == null) throw new NotFoundException("Không tìm thấy phiên tư vấn.");

            // 1. Kiểm tra quyền (Nếu có userId truyền vào - tức là người dùng chủ động bấm)
            if (userId.HasValue)
            {
                var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);
                bool isDoctor = doctor != null && doctor.UserId == userId;

                var patientMember = await _unitOfWork.Repository<Members>().GetByIdAsync(session.MemberId);
                bool isPatientOrFamily = false;
                if (patientMember != null)
                {
                    isPatientOrFamily = patientMember.UserId == userId ||
                                       (patientMember.FamilyId.HasValue && await _unitOfWork.Repository<Members>()
                                        .GetQueryable().AnyAsync(m => m.FamilyId == patientMember.FamilyId && m.UserId == userId));
                }

                if (!isDoctor && !isPatientOrFamily)
                    throw new ForbiddenException("Bạn không có quyền kết thúc phiên này.");
            }

            if (session.Status == ConsultationSessionConstants.ENDED)
                return MapSession(session);

            // 2. Cập nhật trạng thái Session
            session.Status = ConsultationSessionConstants.ENDED;
            session.EndedAt = DateTime.Now;
            await _appointmentRepository.UpdateSessionAsync(session);

            // 3. Cập nhật Appointment sang Completed
            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(session.AppointmentId);
            if (appointment != null)
            {
                if (appointment.Status != AppointmentConstants.COMPLETED)
                {
                    appointment.Status = AppointmentConstants.COMPLETED;
                    await _appointmentRepository.UpdateAppointmentAsync(appointment);
                }

                // ✅ QUAN TRỌNG: Kích hoạt trạng thái ReadyToPay cho Payout (Flow 6)
                await ProcessDoctorPayoutAsync(appointment, session.ConsultanSessionId);
            }

            await _unitOfWork.CompleteAsync();

            // 4. Gửi thông báo cho các bên
            string enderName = userId.HasValue ? "Người dùng" : "Hệ thống";
            await NotifyEndSessionAsync(session, enderName);

            return MapSession(session);
        }

        // ✅ HÀM BỔ TRỢ XỬ LÝ PAYOUT (Đảm bảo đồng bộ tài chính cho Flow 6)
        private async Task ProcessDoctorPayoutAsync(Appointments appointment, Guid sessionId)
        {
            var payout = await _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                .FirstOrDefaultAsync(p => p.AppointmentId == appointment.AppointmentId);

            if (payout != null && payout.Status == "Hold")
            {
                payout.ConsultationId = sessionId;
                payout.Status = "ReadyToPay";
                _unitOfWork.Repository<DoctorPayout>().Update(payout);

                // Lấy tên phòng khám để gửi thông báo Admin
                var doctor = await _doctorRepository.GetDoctorByIdAsync(appointment.DoctorId);
                var clinicDoc = await _unitOfWork.Repository<ClinicDoctors>().GetQueryable()
                    .Include(cd => cd.Clinic)
                    .FirstOrDefaultAsync(cd => cd.DoctorId == appointment.DoctorId && cd.Status == "Active");

                // Thông báo cho Admin đối soát (Flow 6 - Reconciliation)
                await _notificationService.SendNotificationToRoleAsync(
                    Roles.Admin,
                    "Yêu cầu tất toán mới",
                    $"Bác sĩ {doctor?.FullName} thuộc {clinicDoc?.Clinic?.Name ?? "Phòng khám"} vừa hoàn tất ca khám {appointment.AppointmentId.ToString()[..8].ToUpper()}.",
                    "Info"
                );
            }
        }

        // Hàm hỗ trợ gửi thông báo khi kết thúc
        private async Task NotifyEndSessionAsync(ConsultationSessions session, string enderName)
        {
            string title = "✅ Phiên tư vấn đã kết thúc";
            string message = $"{enderName} đã kết thúc buổi tư vấn. Bạn có thể xem lại hồ sơ trong lịch sử khám.";

            // Notify Bác sĩ
            var doctor = await _doctorRepository.GetDoctorByIdAsync(session.DoctorId);
            if (doctor != null)
                await _notificationService.SendNotificationAsync(doctor.UserId, title, message, ConsultationSessionActionTypes.SESSION_ENDED, session.ConsultanSessionId);

            // Notify Bệnh nhân/Chủ hộ
            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(session.MemberId);
            if (member != null)
            {
                Guid? headId = member.UserId;
                if (!headId.HasValue && member.FamilyId != null)
                {
                    headId = (await _unitOfWork.Repository<Members>().GetQueryable()
                        .FirstOrDefaultAsync(m => m.FamilyId == member.FamilyId && m.UserId != null))?.UserId;
                }
                if (headId.HasValue)
                    await _notificationService.SendNotificationAsync(headId.Value, title, message, ConsultationSessionActionTypes.SESSION_ENDED, session.ConsultanSessionId);
            }
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
