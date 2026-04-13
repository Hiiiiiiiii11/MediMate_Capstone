using Hangfire;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Microsoft.EntityFrameworkCore;
using Share.Common;
using Share.Constants;

using Microsoft.AspNetCore.SignalR;
using MediMateService.Hubs;

namespace MediMateService.Services.Implementations
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IActivityLogService _activityLogService;
        private readonly IHubContext<MediMateHub> _hubContext;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IDoctorRepository doctorRepository,
            IUnitOfWork unitOfWork,
            INotificationService notificationService, IBackgroundJobClient backgroundJobClient, IActivityLogService activityLogService, IHubContext<MediMateHub> hubContext)

        {
            _appointmentRepository = appointmentRepository;
            _doctorRepository = doctorRepository;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _backgroundJobClient = backgroundJobClient;
            _activityLogService = activityLogService;
            _hubContext = hubContext;
        }
        //check lịch availability
        public async Task<AppointmentDto> CreateAppointmentAsync(Guid userId, CreateAppointmentDto request)
        {
            // 1. Kiểm tra thời gian đặt lịch
            var requestedDateTime = request.AppointmentDate.Date.Add(request.AppointmentTime);
            if (requestedDateTime < DateTime.Now.AddMinutes(10))
            {
                throw new BadRequestException("Không thể đặt lịch trong quá khứ hoặc quá sát giờ hiện tại.");
            }

            // 2. Lấy thông tin Bệnh nhân kèm Gia đình (Sử dụng "Family" dạng string để tránh lỗi Npgsql)
            var member = await _unitOfWork.Repository<Members>().GetQueryable()
                .Include("Family")
                .FirstOrDefaultAsync(m => m.MemberId == request.MemberId);

            if (member == null) throw new NotFoundException("Không tìm thấy hồ sơ thành viên.");

            // 3. Lấy thông tin Bác sĩ kèm User thông tin
            var doctor = await _unitOfWork.Repository<Doctors>().GetQueryable()
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DoctorId == request.DoctorId);

            if (doctor == null || !string.Equals(doctor.Status, DoctorStatuses.Active, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotFoundException("Không tìm thấy bác sĩ phù hợp.");
            }

            // 4. Lấy thông tin người trực tiếp thực hiện đặt lịch (User đang đăng nhập)
            var orderPlacer = await _unitOfWork.Repository<User>().GetByIdAsync(userId);

            // 5. Kiểm tra lịch làm việc (Availability)
            var availability = await _doctorRepository.GetAvailabilityByIdAsync(request.DoctorId, request.AvailabilityId);
            if (availability == null || !availability.IsActive)
            {
                throw new NotFoundException("Không tìm thấy khung giờ làm việc khả dụng.");
            }

            var appointmentDayOfWeek = request.AppointmentDate.DayOfWeek.ToString();
            if (!string.Equals(availability.DayOfWeek, appointmentDayOfWeek, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("Ngày đặt khám không khớp với khung giờ rảnh của bác sĩ.");
            }

            // 6. Kiểm tra ngày nghỉ (Day Off)
            var isDayOff = (await _unitOfWork.Repository<DoctorAvailabilityExceptions>()
                .FindAsync(e => e.DoctorId == request.DoctorId
                                && e.Date.Date == request.AppointmentDate.Date
                                && !e.IsAvailableOverride)).Any();
            if (isDayOff) throw new BadRequestException("Bác sĩ đã xin nghỉ phép vào ngày này.");

            // 7. Kiểm tra trùng lịch (Slot Booked)
            var isSlotBooked = (await _unitOfWork.Repository<Appointments>()
                .FindAsync(a => a.DoctorId == request.DoctorId
                                && a.AvailabilityId == request.AvailabilityId
                                && a.AppointmentDate.Date == request.AppointmentDate.Date
                                && a.AppointmentTime == request.AppointmentTime
                                && a.Status != AppointmentConstants.CANCELLED
                                && a.Status != AppointmentConstants.REJECTED)).Any();
            if (isSlotBooked) throw new ConflictException("Khung giờ này đã có bệnh nhân khác đặt.");

            // 8. Kiểm tra gói dịch vụ (Subscription)
            if (member.FamilyId == null) throw new BadRequestException("Hồ sơ không thuộc gia đình nào.");

            var currentDate = DateOnly.FromDateTime(DateTime.Now);
            var activeSubscription = (await _unitOfWork.Repository<FamilySubscriptions>()
                .FindAsync(s => s.FamilyId == member.FamilyId
                                && s.Status == "Active"
                                && s.StartDate <= currentDate
                                && s.EndDate >= currentDate)).FirstOrDefault();

            if (activeSubscription == null) throw new ForbiddenException("Gia đình không có gói hội viên đang hoạt động.");
            if (activeSubscription.RemainingConsultantCount <= 0) throw new ForbiddenException("Gia đình bạn đã hết lượt khám.");

            // --- THỰC HIỆN NGHIỆP VỤ ---

            // Trừ lượt khám
            activeSubscription.RemainingConsultantCount -= 1;
            _unitOfWork.Repository<FamilySubscriptions>().Update(activeSubscription);

            // Tạo Appointment
            var appointment = new Appointments
            {
                AppointmentId = Guid.NewGuid(),
                DoctorId = request.DoctorId,
                MemberId = request.MemberId,
                AvailabilityId = request.AvailabilityId,
                AppointmentDate = request.AppointmentDate,
                AppointmentTime = request.AppointmentTime,
                Status = AppointmentConstants.PENDING,
                CreatedAt = DateTime.Now
            };
            await _appointmentRepository.AddAppointmentAsync(appointment);

            // Định dạng thông tin hiển thị
            string timeStr = request.AppointmentTime.ToString(@"hh\:mm");
            string dateStr = request.AppointmentDate.ToString("dd/MM/yyyy");
            string familyName = member.Family?.FamilyName ?? "Gia đình";
            string doctorName = doctor.User?.FullName ?? "Bác sĩ";
            string patientName = member.FullName;
            string placerName = orderPlacer?.FullName ?? "Thành viên gia đình";

            // 9. GỬI THÔNG BÁO CHO BÁC SĨ
            if (doctor.User != null)
            {
                await _notificationService.SendNotificationAsync(
                    userId: doctor.UserId,
                    title: "📅 Lịch đặt khám mới!",
                    message: $"{placerName} đã đặt lịch cho bệnh nhân {patientName} ({familyName}) vào lúc {timeStr} ngày {dateStr}.",
                    type: AppointmentActionTypes.NEW_APPOINTMENT,
                    referenceId: appointment.AppointmentId
                );
            }

            // 9.1. GỬI THÔNG BÁO CHO MEMBER QUA MEMBER ID
            await _notificationService.SendNotificationAsync(
                userId: null,
                title: "📅 Lịch khám mới cho bạn!",
                message: $"{placerName} đã đặt lịch khám cho bạn với bác sĩ {doctorName} vào lúc {timeStr} ngày {dateStr}.",
                type: AppointmentActionTypes.NEW_APPOINTMENT,
                referenceId: appointment.AppointmentId,
                memberId: member.MemberId
            );

            // 10. GHI NHẬT KÝ HOẠT ĐỘNG (Activity Log)
            await _activityLogService.LogActivityAsync(
                familyId: member.FamilyId.Value,
                memberId: member.MemberId,
                actionType: ActivityActionTypes.CREATE,
                entityName: "Appointment",
                entityId: appointment.AppointmentId,
                description: $"{placerName} đã đặt lịch khám cho {patientName} (Gia đình {familyName}). Bác sĩ: {doctorName}. Thời gian: {timeStr} ngày {dateStr}."
            );

            // Lưu toàn bộ thay đổi
            await _unitOfWork.CompleteAsync();

            // 11. Hẹn giờ Hangfire tự động hủy nếu không duyệt
            var appointmentFullTime = request.AppointmentDate.Date.Add(request.AppointmentTime);
            var autoCancelTime = appointmentFullTime.AddMinutes(-20);
            if (autoCancelTime <= DateTime.Now) autoCancelTime = appointmentFullTime.AddMinutes(-5);

            _backgroundJobClient.Schedule<IReminderJobService>(
                job => job.AutoCancelUnapprovedAppointmentAsync(appointment.AppointmentId),
                new DateTimeOffset(autoCancelTime)
            );

            // SignalR Update
            if (doctor.User != null) {
                await _hubContext.Clients.Group($"User_{doctor.UserId}").SendAsync("AppointmentStatusUpdated", new {
                    appointmentId = appointment.AppointmentId,
                    status = appointment.Status
                });
            }
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("AppointmentStatusUpdated", new {
                appointmentId = appointment.AppointmentId,
                status = appointment.Status
            });

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



            if (appointment.Status == AppointmentConstants.CANCELLED)
            {
                throw new ConflictException("Lịch hẹn đã được hủy trước đó.");
            }

            appointment.Status = AppointmentConstants.CANCELLED;
            appointment.CancelReason = request.Reason?.Trim();
            await _appointmentRepository.UpdateAppointmentAsync(appointment);

            var session = await _appointmentRepository.GetSessionByAppointmentIdAsync(appointmentId);
            if (session != null && session.Status != "Ended")
            {
                session.Status = "Cancelled";
                await _appointmentRepository.UpdateSessionAsync(session);
            }

            // -------------------------------------------------------------------------
            // [NEW] NẾU HỦY LỊCH TRƯỚC KHI KHÁM -> HOÀN TRẢ LẠI 1 LƯỢT CHO GIA ĐÌNH
            // -------------------------------------------------------------------------
            if (member?.FamilyId != null)
            {
                var currentDate = DateOnly.FromDateTime(DateTime.Now);
                var activeSubscription = (await _unitOfWork.Repository<FamilySubscriptions>()
                    .FindAsync(s => s.FamilyId == member.FamilyId
                                    && s.Status == "Active"
                                    && s.EndDate >= currentDate)).FirstOrDefault();

                if (activeSubscription != null)
                {
                    activeSubscription.RemainingConsultantCount += 1;
                    _unitOfWork.Repository<FamilySubscriptions>().Update(activeSubscription);
                }
            }

            // Cập nhật Database
            await _unitOfWork.CompleteAsync();

            string timeString = appointment.AppointmentTime.ToString(@"hh\:mm");
            string dateString = appointment.AppointmentDate.ToString("dd/MM/yyyy");
            string reasonStr = string.IsNullOrWhiteSpace(request.Reason) ? "Không có lý do cụ thể" : request.Reason;

            if (isUserOwner && doctor != null)
            {
                // 1. Bệnh nhân hủy -> Gửi thông báo cho Bác sĩ
                await _notificationService.SendNotificationAsync(
                    userId: doctor.UserId,
                    title: "❌ Lịch khám đã bị hủy",
                    message: $"Bệnh nhân {member?.FullName} đã hủy lịch khám lúc {timeString} ngày {dateString}. Lý do: {reasonStr}.",
                    type: AppointmentActionTypes.APPOINTMENT_CANCELLED,
                    referenceId: appointment.AppointmentId
                );
            }

            // SignalR Update
            if (doctor != null) {
                await _hubContext.Clients.Group($"User_{doctor.UserId}").SendAsync("AppointmentStatusUpdated", new {
                    appointmentId = appointment.AppointmentId,
                    status = appointment.Status
                });
            }
            if (member?.UserId != null) {
                await _hubContext.Clients.Group($"User_{member.UserId}").SendAsync("AppointmentStatusUpdated", new {
                    appointmentId = appointment.AppointmentId,
                    status = appointment.Status
                });
            }

            return MapAppointment(appointment);
        }

        public async Task<List<AppointmentDto>> GetAppointmentsAsync(Guid userId)
        {
            // 1. Lấy lịch hẹn với tư cách là Bác sĩ (nếu có)
            var doctor = (await _unitOfWork.Repository<Doctors>()
                .FindAsync(d => d.UserId == userId)).FirstOrDefault();

            var doctorAppointments = doctor == null
                ? new List<Appointments>()
                : await _appointmentRepository.GetAppointmentsByDoctorIdAsync(doctor.DoctorId);

            // 2. Lấy danh sách các FamilyId mà User này là thành viên
            var userMemberProfiles = await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.UserId == userId && m.FamilyId != null);

            var familyIds = userMemberProfiles.Select(m => m.FamilyId!.Value).Distinct().ToList();

            // 3. Lấy tất cả MemberId thuộc về những gia đình trên
            // Bước này giúp lấy được cả MemberId của con cái, người thân trong cùng gia đình
            var allFamilyMemberIds = await _unitOfWork.Repository<Members>()
                .GetQueryable()
                .Where(m => m.FamilyId.HasValue && familyIds.Contains(m.FamilyId.Value))
                .Select(m => m.MemberId)
                .ToListAsync();

            // 4. Lấy lịch hẹn của tất cả các thành viên trong các gia đình đó
            var memberAppointments = await _unitOfWork.Repository<Appointments>()
                .GetQueryable()
                .Where(a => allFamilyMemberIds.Contains(a.MemberId))
                .ToListAsync();

            // 5. Gộp lịch hẹn (Bác sĩ + Thành viên gia đình), loại bỏ trùng lặp và sắp xếp
            var merged = memberAppointments
                .Concat(doctorAppointments)
                .GroupBy(a => a.AppointmentId)
                .Select(g => g.First())
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToList();

            return merged.Select(MapAppointment).ToList();
        }

        public async Task<List<AppointmentDto>> GetAppointmentsByDoctorUserIdAsync(Guid userId)
        {
            var doctor = (await _unitOfWork.Repository<Doctors>()
                .FindAsync(d => d.UserId == userId)).FirstOrDefault();

            if (doctor == null)
            {
                throw new NotFoundException("Không tìm thấy hồ sơ bác sĩ cho tài khoản hiện tại.");
            }

            return await GetAppointmentsByDoctorIdAsync(doctor.DoctorId);
        }

        public async Task<ApiResponse<List<AvailableSlotDto>>> GetAvailableSlotsAsync(Guid doctorId, DateTime date)
        {
            var result = new List<AvailableSlotDto>();

            // 1. ÉP CHUẨN NGÀY: Chuyển mọi thứ về múi giờ Local và chỉ lấy phần Ngày (Date)
            DateTime targetDate = date.ToLocalTime().Date;
            DateTime today = DateTime.Now.Date;

            // Nếu tra cứu ngày trong quá khứ -> Cứ im lặng trả về mảng rỗng []
            if (targetDate < today)
            {
                return ApiResponse<List<AvailableSlotDto>>.Ok(result, "Bác sĩ không có ca làm việc hiện tại");
            }

            // 1. Chặn ngay từ vòng gửi xe nếu user cố tình xem ngày hôm qua
            if (date.Date < DateTime.Now.Date)
            {
                return ApiResponse<List<AvailableSlotDto>>.Ok(result, "Không thể tra cứu lịch của ngày trong quá khứ.");
            }

            // 2. Kiểm tra bác sĩ có xin nghỉ nguyên ngày không
            var isDayOff = (await _unitOfWork.Repository<DoctorAvailabilityExceptions>()
                .FindAsync(e => e.DoctorId == doctorId
                                 && e.Date.Date == date.Date
                                 && !e.IsAvailableOverride)).Any();

            if (isDayOff)
            {
                return ApiResponse<List<AvailableSlotDto>>.Ok(result, "Bác sĩ xin nghỉ phép ngày này.");
            }

            // 3. Lấy danh sách ca làm việc (Đã bao gồm check a.IsActive == true)
            string dayOfWeekString = date.DayOfWeek.ToString();
            var availabilities = await _unitOfWork.Repository<DoctorAvailability>()
                .FindAsync(a => a.DoctorId == doctorId
                                 && a.DayOfWeek == dayOfWeekString
                                 && a.IsActive);

            if (!availabilities.Any())
            {
                return ApiResponse<List<AvailableSlotDto>>.Ok(result, "Bác sĩ không có ca làm việc vào ngày này.");
            }

            // 4. Lấy các lịch đã bị người khác đặt mất
            var bookedAppointments = await _unitOfWork.Repository<Appointments>()
                .FindAsync(a => a.DoctorId == doctorId
                                 && a.AppointmentDate.Date == date.Date
                                 && a.Status != AppointmentConstants.CANCELLED
                                 && a.Status != AppointmentConstants.REJECTED);

            var bookedTimes = bookedAppointments.Select(a => a.AppointmentTime).ToList();

            // 5. CHIA SLOT THỜI GIAN (60 phút/slot)
            TimeSpan slotDuration = TimeSpan.FromMinutes(60);

            // LOGIC MỚI: Xử lý triệt để thời gian quá khứ
            bool isToday = date.Date == DateTime.Now.Date;
            TimeSpan currentTime = DateTime.Now.TimeOfDay;
            // Buffer: Ví dụ bây giờ là 10h15, thì slot 10h-11h sẽ bị ẩn luôn, chỉ hiện từ slot 11h trở đi
            TimeSpan bufferTime = TimeSpan.FromMinutes(30);

            foreach (var shift in availabilities)
            {
                TimeSpan currentSlotTime = shift.StartTime;

                // FIX BUGS: Bù thêm 1 phút vào EndTime để xử lý ca 23:59 vs 24:00
                TimeSpan adjustedEndTime = shift.EndTime.Add(TimeSpan.FromMinutes(1));

                while (currentSlotTime + slotDuration <= adjustedEndTime)
                {
                    // NẾU LÀ HÔM NAY: Ẩn các slot có giờ bắt đầu <= Giờ hiện tại + 30 phút
                    if (isToday && currentSlotTime <= currentTime.Add(bufferTime))
                    {
                        currentSlotTime = currentSlotTime.Add(slotDuration);
                        continue;
                    }

                    result.Add(new AvailableSlotDto
                    {
                        AvailabilityId = shift.DoctorAvailabilityId,
                        Time = currentSlotTime,
                        DisplayTime = $"{currentSlotTime:hh\\:mm} - {currentSlotTime + slotDuration:hh\\:mm}",
                        IsBooked = bookedTimes.Contains(currentSlotTime)
                    });

                    currentSlotTime = currentSlotTime.Add(slotDuration);
                }
            }

            result = result.OrderBy(x => x.Time).ToList();
            return ApiResponse<List<AvailableSlotDto>>.Ok(result, "Lấy danh sách giờ khám thành công.");
        }

        public async Task<AppointmentDto> UpdateAppointmentAsync(Guid appointmentId, Guid userId, UpdateAppointmentDto request)
        {
            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
            if (appointment == null)
            {
                throw new NotFoundException("Không tìm thấy lịch hẹn.");
            }

            // Kiểm tra quyền (Chỉ Bác sĩ hoặc Bệnh nhân của lịch này mới được sửa)
            var doctor = await _doctorRepository.GetDoctorByIdAsync(appointment.DoctorId);
            var isDoctorOwner = doctor != null && doctor.UserId == userId;

            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(appointment.MemberId);
            var isUserOwner = member != null && member.UserId == userId;

            if (!isDoctorOwner)
            {
                throw new ForbiddenException("Bạn không có quyền cập nhật lịch hẹn này.");
            }

            if (appointment.Status == AppointmentConstants.CANCELLED || appointment.Status == AppointmentConstants.COMPLETED)
            {
                throw new BadRequestException($"Không thể cập nhật lịch hẹn đã ở trạng thái {appointment.Status}.");
            }

            // NẾU TRẠNG THÁI CÓ SỰ THAY ĐỔI
            if (!string.Equals(appointment.Status, request.Status, StringComparison.OrdinalIgnoreCase))
            {
                // --- NGHIỆP VỤ: TỪ CHỐI (REJECTED) -> HOÀN LƯỢT KHÁM ---
                if (request.Status.Equals(AppointmentConstants.REJECTED, StringComparison.OrdinalIgnoreCase) && member?.FamilyId != null)
                {
                    var currentDate = DateOnly.FromDateTime(DateTime.Now);
                    var activeSubscription = (await _unitOfWork.Repository<FamilySubscriptions>()
                        .FindAsync(s => s.FamilyId == member.FamilyId
                                        && s.Status == "Active"
                                        && s.EndDate >= currentDate)).FirstOrDefault();

                    if (activeSubscription != null)
                    {
                        activeSubscription.RemainingConsultantCount += 1; // Trả lại 1 lượt cho gia đình
                        _unitOfWork.Repository<FamilySubscriptions>().Update(activeSubscription);
                    }
                }

                appointment.Status = request.Status;
                await _appointmentRepository.UpdateAppointmentAsync(appointment);

                // Đồng bộ trạng thái sang bảng ConsultationSessions
                var session = await _appointmentRepository.GetSessionByAppointmentIdAsync(appointmentId);
                if (session != null)
                {
                    session.Status = request.Status;

                    // [NEW] Ghi nhận thời gian kết thúc buổi khám
                    if (request.Status.Equals(AppointmentConstants.COMPLETED, StringComparison.OrdinalIgnoreCase))
                    {
                        session.EndedAt = DateTime.Now;
                    }

                    await _appointmentRepository.UpdateSessionAsync(session);
                }
            }

            await _unitOfWork.CompleteAsync();

            if (member != null)
            {
                string title = "";
                string message = "";

                if (request.Status.Equals(AppointmentConstants.APPROVED, StringComparison.OrdinalIgnoreCase))
                {
                    title = "✅ Lịch khám đã được xác nhận";
                    message = $"Bác sĩ đã chấp nhận lịch khám của {member.FullName}. Vui lòng chuẩn bị sẵn sàng vào khung giờ đã đặt.";

                    var appointmentFullTime = appointment.AppointmentDate.Date.Add(appointment.AppointmentTime);

                    // Job T-15 phút: Gửi thông báo nhắc giờ khám (giữ nguyên logic cũ)
                    var notifyTime = appointmentFullTime.AddMinutes(-15);
                    if (notifyTime > DateTime.Now)
                    {
                        _backgroundJobClient.Schedule<IReminderJobService>(
                            job => job.NotifyUpcomingAppointmentAsync(appointment.AppointmentId),
                            new DateTimeOffset(notifyTime)
                        );
                    }

                    // Job T-5 phút: Tạo ConsultationSession trước 5 phút để phòng khám sẵn sàng
                    var sessionCreateTime = appointmentFullTime.AddMinutes(-5);
                    if (sessionCreateTime > DateTime.Now)
                    {
                        _backgroundJobClient.Schedule<IReminderJobService>(
                            job => job.CreateConsultationSessionAsync(appointment.AppointmentId),
                            new DateTimeOffset(sessionCreateTime)
                        );
                    }
                    else
                    {
                        // Nếu đã qua T-5 (approve sát giờ), tạo session ngay lập tức
                        _backgroundJobClient.Enqueue<IReminderJobService>(
                            job => job.CreateConsultationSessionAsync(appointment.AppointmentId)
                        );
                    }
                }
                else if (request.Status.Equals(AppointmentConstants.COMPLETED, StringComparison.OrdinalIgnoreCase))
                {
                    title = "🏁 Buổi khám đã hoàn thành";
                    message = $"Buổi tư vấn của {member.FullName} đã kết thúc. Vui lòng kiểm tra lại tin nhắn để xem toa thuốc hoặc lời dặn dò của bác sĩ (nếu có).";
                }
                else if (request.Status.Equals(AppointmentConstants.REJECTED, StringComparison.OrdinalIgnoreCase))
                {
                    title = "❌ Lịch khám bị từ chối";
                    message = $"Rất tiếc, bác sĩ không thể tiếp nhận lịch khám của {member.FullName}. Lượt khám đã được hoàn trả lại cho bạn.";
                }


                if (!string.IsNullOrEmpty(title))
                {
                    await _notificationService.SendNotificationAsync(
                        userId: member.UserId ?? Guid.Empty, // Bắn cho tài khoản Chủ hộ của Member này
                        title: title,
                        message: message,
                        type: AppointmentActionTypes.APPOINTMENT_UPDATED,
                        referenceId: appointment.AppointmentId
                    );
                }

                // SignalR Update
                await _hubContext.Clients.Group($"User_{member.UserId}").SendAsync("AppointmentStatusUpdated", new { 
                    appointmentId = appointment.AppointmentId, 
                    status = request.Status 
                });
            }
            if (doctor != null) {
                await _hubContext.Clients.Group($"User_{doctor.UserId}").SendAsync("AppointmentStatusUpdated", new { 
                    appointmentId = appointment.AppointmentId, 
                    status = request.Status 
                });
            }

            return MapAppointment(appointment);
        }
        public async Task<List<AppointmentDto>> GetAppointmentsByDoctorIdAsync(Guid doctorId)
        {
            var appointments = await _unitOfWork.Repository<Appointments>()
                .GetQueryable()
                .Include(appointments => appointments.Member)
                .Where(a => a.DoctorId == doctorId)
                .ToListAsync();

            return appointments
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .Select(MapAppointment)
                .ToList();
        }
        public async Task<ApiResponse<AppointmentDetailDto>> GetAppointmentDetailAsync(Guid appointmentId)
        {
            // Sử dụng GetQueryable và Include để lấy dữ liệu từ các bảng liên quan
            var appointment = await _unitOfWork.Repository<Appointments>().GetQueryable()
                .Include(a => a.Doctor)
                    .ThenInclude(d => d!.User) // Lấy thông tin User của Bác sĩ (chứa Tên, Avatar...)
                .Include(a => a.Member)        // Lấy thông tin bệnh nhân
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
            {
                return ApiResponse<AppointmentDetailDto>.Fail("Không tìm thấy thông tin lịch hẹn.", 404);
            }

            var detail = new AppointmentDetailDto
            {
                AppointmentId = appointment.AppointmentId,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                Status = appointment.Status,
                CancelReason = appointment.CancelReason,
                CreatedAt = appointment.CreatedAt,

                // Map thông tin Bác sĩ (Giả sử Tên và Avatar nằm trong bảng User)
                DoctorId = appointment.DoctorId,
                DoctorName = appointment.Doctor?.User?.FullName ?? "Chưa cập nhật",
                DoctorAvatar = appointment.Doctor?.User?.AvatarUrl,
                Specialty = appointment.Doctor?.Specialty,

                // Map thông tin Bệnh nhân
                MemberId = appointment.MemberId,
                MemberName = appointment.Member?.FullName ?? "Chưa cập nhật",
                MemberAvatar = appointment.Member?.AvatarUrl,
                MemberGender = appointment.Member?.Gender,
                MemberDateOfBirth = appointment.Member?.DateOfBirth
            };

            return ApiResponse<AppointmentDetailDto>.Ok(detail, "Lấy chi tiết lịch hẹn thành công.");
        }

        public async Task<List<AppointmentDto>> GetAppointmentsByMemberIdAsync(Guid memberId)
        {
            // Lấy tất cả lịch hẹn mà MemberId trùng với tham số truyền vào
            var appointments = await _unitOfWork.Repository<Appointments>()
                .GetQueryable()
                .Where(a => a.MemberId == memberId)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            // Map sang DTO để trả về
            return appointments.Select(MapAppointment).ToList();
        }

        private static AppointmentDto MapAppointment(Appointments item)
        {
            return new AppointmentDto
            {
                AppointmentId = item.AppointmentId,
                DoctorId = item.DoctorId,
                MemberId = item.MemberId,
                MemberName = item.Member?.FullName,
                AvailabilityId = item.AvailabilityId,
                AppointmentDate = item.AppointmentDate,
                AppointmentTime = item.AppointmentTime,
                Status = item.Status,
                CancelReason = item.CancelReason,
                CreatedAt = item.CreatedAt
            };
        }
    }
}
