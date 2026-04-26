using Hangfire;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Microsoft.AspNetCore.Http;
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
        private readonly IPayOSService _payOsService;
        private readonly IUploadPhotoService _uploadPhotoService;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IDoctorRepository doctorRepository,
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IBackgroundJobClient backgroundJobClient,
            IActivityLogService activityLogService,
            IHubContext<MediMateHub> hubContext,
            IPayOSService payOsService,
            IUploadPhotoService uploadPhotoService)
        {
            _appointmentRepository = appointmentRepository;
            _doctorRepository = doctorRepository;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _backgroundJobClient = backgroundJobClient;
            _activityLogService = activityLogService;
            _hubContext = hubContext;
            _payOsService = payOsService;
            _uploadPhotoService = uploadPhotoService;
        }
        //check lịch availability
        public async Task<AppointmentPaymentResponseDto> CreateAppointmentAsync(Guid userId, CreateAppointmentDto request)
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

            // 8. TÌM PHÒNG KHÁM DUY NHẤT MÀ BÁC SĨ ĐANG LÀM VIỆC ĐỂ LẤY GIÁ KHÁM VÀ CLINIC_ID
            var clinicDoctor = await _unitOfWork.Repository<ClinicDoctors>().GetQueryable()
                .FirstOrDefaultAsync(cd => cd.DoctorId == request.DoctorId && cd.Status == "Active");

            if (clinicDoctor == null)
            {
                throw new BadRequestException("Bác sĩ hiện không hoạt động tại bất kỳ phòng khám nào.");
            }

            // Tạo Appointment (Trạng thái thanh toán là Pending)
            var appointment = new Appointments
            {
                AppointmentId = Guid.NewGuid(),
                DoctorId = request.DoctorId,
                MemberId = request.MemberId,
                AvailabilityId = request.AvailabilityId,
                ClinicId = clinicDoctor.ClinicId,
                AppointmentDate = request.AppointmentDate,
                AppointmentTime = request.AppointmentTime,
                Status = AppointmentConstants.PENDING,
                PaymentStatus = "Pending", // [NEW] Chờ thanh toán PayOS
                CreatedAt = DateTime.Now
            };
            await _appointmentRepository.AddAppointmentAsync(appointment);

            // Ghi nhận Payment xuống Database
            var paymentRecord = new Payments
            {
                PaymentId = Guid.NewGuid(),
                AppointmentId = appointment.AppointmentId,
                UserId = userId,
                Amount = clinicDoctor.ConsultationFee,
                PaymentContent = $"Thanh toan tien kham benh - {member.FullName}",
                Status = "Pending",
                CreatedAt = DateTime.Now
            };
            await _unitOfWork.Repository<Payments>().AddAsync(paymentRecord);
            
            // Gọi PayOS để lấy link thanh toán
            var payOsRequest = new CreatePaymentRequest
            {
                // Truyền các thông tin cần thiết vào (ở đây ta tận dụng CreatePaymentRequest, cần mở rộng nếu thiếu field)
                BuyerName = orderPlacer?.FullName ?? member.FullName,
                BuyerEmail = orderPlacer?.Email ?? "",
                BuyerPhone = orderPlacer?.PhoneNumber ?? "",
                ReturnUrl = "http://localhost:5173/payment/success", // Cấu hình URL trả về của Frontend
                CancelUrl = "http://localhost:5173/payment/cancel"
            };
            // Do hàm CreatePaymentLinkAsync hiện tại đang được code cho Package, ta cần viết 1 hàm riêng cho Appointment
            // Ở bước này, tạm gọi CreateAppointmentPaymentLinkAsync (sẽ thêm vào IPayOSService sau)
            var paymentLinkResponse = await _payOsService.CreateAppointmentPaymentLinkAsync(userId, paymentRecord, payOsRequest);

            // Định dạng thông tin hiển thị cho thông báo (có thể dùng trong ActivityLog)
            string timeStr = request.AppointmentTime.ToString(@"hh\:mm");
            string dateStr = request.AppointmentDate.ToString("dd/MM/yyyy");
            string familyName = member.Family?.FamilyName ?? "Gia đình";
            string doctorName = doctor.User?.FullName ?? "Bác sĩ";
            string patientName = member.FullName;
            string placerName = orderPlacer?.FullName ?? "Thành viên gia đình";

            // [FIX] Các thông báo (Notification) đã được dời sang Webhook (PayOSService.cs) để đảm bảo chỉ thông báo khi đã thanh toán thành công.




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

            // [FIX] SignalR Update (Gửi thông báo realtime) đã được dời sang Webhook (PayOSService.cs)

            // 12. LÊN LỊCH HỦY TỰ ĐỘNG NẾU KHÔNG THANH TOÁN (10 phút)
            _backgroundJobClient.Schedule(() => CheckAndCancelUnpaidAppointmentAsync(appointment.AppointmentId), TimeSpan.FromMinutes(10));

            return new AppointmentPaymentResponseDto
            {
                Appointment = MapAppointment(appointment),
                CheckoutUrl = paymentLinkResponse.PaymentUrl,
                OrderCode = paymentLinkResponse.OrderCode,
                QrCode = paymentLinkResponse.QrCode
            };
        }

        // 1. Hàm tự động chạy qua Hangfire sau 10 phút nếu không thanh toán
        public async Task CheckAndCancelUnpaidAppointmentAsync(Guid appointmentId)
        {
            var appointment = await _unitOfWork.Repository<Appointments>().GetQueryable()
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            // Chỉ xử lý nếu lịch hẹn tồn tại và vẫn đang chờ thanh toán
            if (appointment != null && appointment.PaymentStatus == "Pending" && appointment.Status == AppointmentConstants.PENDING)
            {
                // Tìm Payment và load luôn các Transactions đính kèm (nếu có)
                var payment = await _unitOfWork.Repository<Payments>().GetQueryable()
                    .Include(p => p.Transactions)
                    .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);

                if (payment != null)
                {
                    // Xóa tất cả các giao dịch (Transactions) liên quan đến Payment này
                    if (payment.Transactions != null && payment.Transactions.Any())
                    {
                        foreach (var tx in payment.Transactions.ToList())
                        {
                            _unitOfWork.Repository<Transactions>().Remove(tx);
                        }
                    }
                    // Xóa bản ghi Payment
                    _unitOfWork.Repository<Payments>().Remove(payment);
                }

                // Xóa hẳn Appointment để giải phóng slot cho bác sĩ
                _unitOfWork.Repository<Appointments>().Remove(appointment);

                await _unitOfWork.CompleteAsync();
            }
        }

        // 2. Hàm xóa thủ công (khi người dùng chủ động nhấn hủy/xóa lúc chưa trả tiền)
        public async Task DeleteUnpaidAppointmentAsync(Guid appointmentId)
        {
            var appointment = await _unitOfWork.Repository<Appointments>().GetQueryable()
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null) throw new NotFoundException("Không tìm thấy lịch hẹn.");

            if (appointment.PaymentStatus != "Pending" || appointment.Status != AppointmentConstants.PENDING)
            {
                throw new BadRequestException("Chỉ có thể xóa lịch hẹn khi đang ở trạng thái chờ thanh toán.");
            }

            // Tìm Payment kèm theo các Transactions
            var payment = await _unitOfWork.Repository<Payments>().GetQueryable()
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);

            if (payment != null)
            {
                // Xóa các giao dịch nháp/chờ xử lý liên quan
                if (payment.Transactions != null && payment.Transactions.Any())
                {
                    foreach (var tx in payment.Transactions.ToList())
                    {
                        _unitOfWork.Repository<Transactions>().Remove(tx);
                    }
                }
                _unitOfWork.Repository<Payments>().Remove(payment);
            }

            // Xóa lịch hẹn
            _unitOfWork.Repository<Appointments>().Remove(appointment);

            await _unitOfWork.CompleteAsync();
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
            if (appointment.Status == AppointmentConstants.COMPLETED)
            {
                throw new BadRequestException("Không thể hủy lịch hẹn đã hoàn thành.");
            }

            var session = await _appointmentRepository.GetSessionByAppointmentIdAsync(appointmentId);
            if (session != null && (session.Status == "InProgress" || session.Status == "Ended"))
            {
                throw new BadRequestException("Không thể hủy lịch hẹn khi phiên khám đã bắt đầu.");
            }

            appointment.Status = AppointmentConstants.CANCELLED;
            appointment.CancelReason = request.Reason?.Trim();
            await _appointmentRepository.UpdateAppointmentAsync(appointment);

            if (session != null && session.Status != "Ended")
            {
                session.Status = "Cancelled";
                await _appointmentRepository.UpdateSessionAsync(session);
            }

            // -------------------------------------------------------------------------
            // [NEW] NẾU HỦY LỊCH TRƯỚC KHI KHÁM VÀ ĐÃ THANH TOÁN -> ĐÁNH DẤU REFUNDED & CANCEL PAYOUT
            // -------------------------------------------------------------------------
            if (appointment.PaymentStatus == "Paid")
            {
                appointment.PaymentStatus = "Refunded"; // Giả định là hoàn tiền
                // Cập nhật DoctorPayout
                var payout = await _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                    .FirstOrDefaultAsync(p => p.AppointmentId == appointment.AppointmentId && p.Status == "Hold" && p.Amount > 0);
                if (payout != null)
                {
                    payout.Status = "Cancelled";
                    _unitOfWork.Repository<DoctorPayout>().Update(payout);
                }
            }

            // Cập nhật Database
            await _unitOfWork.CompleteAsync();

            // ── Kiểm tra User có tài khoản ngân hàng để nhận hoàn tiền chưa ──
            bool userHasBankAccount = false;
            if (appointment.PaymentStatus == "Refunded" && member?.UserId != null)
            {
                userHasBankAccount = await _unitOfWork.Repository<UserBankAccount>().GetQueryable()
                    .AnyAsync(b => b.UserId == member.UserId);
            }

            string timeString = appointment.AppointmentTime.ToString(@"hh\:mm");
            string dateString = appointment.AppointmentDate.ToString("dd/MM/yyyy");
            string reasonStr = string.IsNullOrWhiteSpace(request.Reason) ? "Không có lý do cụ thể" : request.Reason;

            // [QUAN TRỌNG]: Tìm chủ hộ (Family Head) để gửi Notification và SignalR
            Guid? headUserIdCancel = member?.UserId;
            if (!headUserIdCancel.HasValue && member?.FamilyId != null)
            {
                var familyManager = await _unitOfWork.Repository<Members>().GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.FamilyId == member.FamilyId && m.UserId != null);
                headUserIdCancel = familyManager?.UserId;
            }

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

                // 2. Bệnh nhân hủy -> Thông báo lại cho chính họ (tài khoản Member - Patient) để đồng bộ app
                await _notificationService.SendNotificationAsync(
                    userId: null,
                    title: "❌ Lịch khám đã bị hủy",
                    message: $"Lịch khám của bạn với bác sĩ {doctor.User?.FullName ?? "vô danh"} lúc {timeString} ngày {dateString} đã bị hủy thành công.",
                    type: AppointmentActionTypes.APPOINTMENT_CANCELLED,
                    referenceId: appointment.AppointmentId,
                    memberId: appointment.MemberId
                );

                // 3. Nếu là thành viên phụ, gửi thêm bản sao Db cho Chủ Hộ để họ thấy trong tab Thông Báo
                if (headUserIdCancel.HasValue && !member!.UserId.HasValue)
                {
                    await _notificationService.SendNotificationAsync(
                        userId: headUserIdCancel.Value,
                        title: "❌ Lịch khám đã bị hủy",
                        message: $"Lịch khám của {member?.FullName} với bác sĩ {doctor.User?.FullName ?? "vô danh"} lúc {timeString} ngày {dateString} đã bị hủy.",
                        type: AppointmentActionTypes.APPOINTMENT_CANCELLED,
                        referenceId: appointment.AppointmentId
                    );
                }
            }
            else if (isDoctorOwner && member != null)
            {
                // 1. Bác sĩ tự hủy -> Gửi cho User đặt (Chủ hộ / Quản lý gia đình)
                if (headUserIdCancel.HasValue)
                {
                    await _notificationService.SendNotificationAsync(
                        userId: headUserIdCancel.Value,
                        title: "❌ Bác sĩ đã hủy lịch khám",
                        message: $"Bác sĩ {doctor?.User?.FullName ?? "vô danh"} đã hủy lịch khám của bệnh nhân {member.FullName} lúc {timeString} ngày {dateString}. Lý do: {reasonStr}.",
                        type: AppointmentActionTypes.APPOINTMENT_CANCELLED,
                        referenceId: appointment.AppointmentId
                    );
                }

                // 2. Bác sĩ tự hủy -> Gửi cho Bệnh nhân (để push Firebase tự auto route về chủ hộ + lưu DB cho member)
                await _notificationService.SendNotificationAsync(
                    userId: null,
                    title: "❌ Bác sĩ đã hủy lịch khám",
                    message: $"Lịch khám của bạn với bác sĩ {doctor?.User?.FullName ?? "vô danh"} lúc {timeString} ngày {dateString} đã bị hủy. Lý do: {reasonStr}.",
                    type: AppointmentActionTypes.APPOINTMENT_CANCELLED,
                    referenceId: appointment.AppointmentId,
                    memberId: appointment.MemberId
                );
            }

            // SignalR Update
            if (doctor != null) {
                await _hubContext.Clients.Group($"User_{doctor.UserId}").SendAsync("AppointmentStatusUpdated", new {
                    appointmentId = appointment.AppointmentId,
                    status = appointment.Status
                });
            }
            
            // (Logic tìm headUserIdCancel đã di chuyển lên phía trên)
            if (headUserIdCancel.HasValue) {
                await _hubContext.Clients.Group($"User_{headUserIdCancel.Value}").SendAsync("AppointmentStatusUpdated", new {
                    appointmentId = appointment.AppointmentId,
                    status = appointment.Status
                });
            }

            // ── Cảnh báo nếu User chưa có banking info (cần để nhận hoàn tiền) ──
            if (appointment.PaymentStatus == "Refunded" && !userHasBankAccount && headUserIdCancel.HasValue)
            {
                await _notificationService.SendNotificationAsync(
                    userId: headUserIdCancel.Value,
                    title: "⚠️ Bạn chưa có thông tin ngân hàng để nhận hoàn tiền!",
                    message: "Lịch hẹn đã hủy thành công và hệ thống sẽ hoàn tiền cho bạn. " +
                             "Tuy nhiên, bạn chưa cập nhật thông tin ngân hàng. " +
                             "Vui lòng vào Cài đặt → Tài khoản ngân hàng để thêm thông tin nhận hoàn tiền.",
                    type: "BANKING_INFO_MISSING",
                    referenceId: appointment.AppointmentId
                );
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
                .Include(a => a.Member)
                .Include(a => a.Doctor).ThenInclude(d => d!.User)
                .Include(a => a.Clinic)
                .Include(a => a.Payments)
                .Include(a => a.ConsultationSessions)
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
            DateTime targetDate = date.Date;

            // --- LẤY GIỜ HIỆN TẠI ---
            DateTime now = DateTime.Now; // Lấy giờ hệ thống
            bool isToday = targetDate == now.Date;
            TimeSpan currentTime = now.TimeOfDay;

            // 1. Lấy tất cả các ngoại lệ (Exceptions)
            var exceptions = await _unitOfWork.Repository<DoctorAvailabilityExceptions>()
                .FindAsync(e => e.DoctorId == doctorId
                                && e.Date.Date == targetDate
                                && e.Status == DoctorExceptionStatuses.APPROVED);

            // 2. Nếu nghỉ nguyên ngày
            if (exceptions.Any(e => e.IsAvailableOverride == false && !e.StartTime.HasValue))
            {
                return ApiResponse<List<AvailableSlotDto>>.Ok(result, "Bác sĩ nghỉ cả ngày.");
            }

            // 3. Lấy lịch định kỳ
            string dayOfWeekString = date.DayOfWeek.ToString();
            var availabilities = await _unitOfWork.Repository<DoctorAvailability>()
                .FindAsync(a => a.DoctorId == doctorId && a.DayOfWeek == dayOfWeekString && a.IsActive);

            // 4. Lấy lịch đã đặt
            var bookedAppointments = await _unitOfWork.Repository<Appointments>()
                .FindAsync(a => a.DoctorId == doctorId && a.AppointmentDate.Date == targetDate
                                && a.Status != AppointmentConstants.CANCELLED);
            var bookedTimes = bookedAppointments.Select(a => a.AppointmentTime).ToList();

            TimeSpan slotDuration = TimeSpan.FromMinutes(60);

            foreach (var shift in availabilities)
            {
                TimeSpan currentSlotTime = shift.StartTime;
                while (currentSlotTime + slotDuration <= shift.EndTime)
                {
                    // ✅ LOGIC 1: NẾU LÀ HÔM NAY, BỎ QUA CÁC GIỜ ĐÃ QUA
                    if (isToday && currentSlotTime < currentTime)
                    {
                        currentSlotTime = currentSlotTime.Add(slotDuration);
                        continue;
                    }

                    // ✅ LOGIC 2: KIỂM TRA LỊCH NGHỈ (EXCEPTIONS)
                    bool isLeaveSlot = exceptions.Any(e =>
                        e.IsAvailableOverride == false &&
                        e.StartTime.HasValue &&
                        currentSlotTime >= e.StartTime.Value && currentSlotTime < e.EndTime.Value);

                    if (isLeaveSlot)
                    {
                        currentSlotTime = currentSlotTime.Add(slotDuration);
                        continue;
                    }

                    result.Add(new AvailableSlotDto
                    {
                        AvailabilityId = shift.DoctorAvailabilityId,
                        Time = currentSlotTime,
                        DisplayTime = $"{currentSlotTime:hh\\:mm} - {(currentSlotTime + slotDuration):hh\\:mm}",
                        IsBooked = bookedTimes.Contains(currentSlotTime)
                    });

                    currentSlotTime = currentSlotTime.Add(slotDuration);
                }
            }

            return ApiResponse<List<AvailableSlotDto>>.Ok(result.OrderBy(x => x.Time).ToList());
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
                // --- NGHIỆP VỤ: TỪ CHỐI (REJECTED) -> ĐÁNH DẤU REFUND VÀ HỦY PAYOUT ---
                if (request.Status.Equals(AppointmentConstants.REJECTED, StringComparison.OrdinalIgnoreCase))
                {
                    if (appointment.PaymentStatus == "Paid")
                    {
                        appointment.PaymentStatus = "Refunded";
                        // Cập nhật DoctorPayout
                        var payout = await _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                            .FirstOrDefaultAsync(p => p.AppointmentId == appointment.AppointmentId && p.Status == "Hold" && p.Amount > 0); 
                        if (payout != null)
                        {
                            payout.Status = "Cancelled";
                            _unitOfWork.Repository<DoctorPayout>().Update(payout);
                        }
                    }
                }

                // --- NGHIỆP VỤ: ĐỒNG Ý (APPROVED) -> TẠO PAYOUT (HOLD) ---
                if (request.Status.Equals(AppointmentConstants.APPROVED, StringComparison.OrdinalIgnoreCase))
                {
                    // ✅ Guard: Chỉ được Approve khi bệnh nhân đã thanh toán
                    if (appointment.PaymentStatus != "Paid")
                    {
                        throw new BadRequestException("Không thể xác nhận lịch hẹn khi bệnh nhân chưa hoàn tất thanh toán.");
                    }

                    // Tìm phí khám từ ClinicDoctors
                    var clinicDoctor = await _unitOfWork.Repository<ClinicDoctors>().GetQueryable()
                        .FirstOrDefaultAsync(cd => cd.DoctorId == appointment.DoctorId && cd.Status == "Active");

                    var fee = clinicDoctor?.ConsultationFee ?? 0m;

                    // Sinh DoctorPayout ở trạng thái Hold (chờ khám xong mới ReadyToPay)
                    var existingPayout = await _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                        .AnyAsync(p => p.AppointmentId == appointment.AppointmentId);

                    if (!existingPayout && clinicDoctor != null)
                    {
                        var payout = new DoctorPayout
                        {
                            PayoutId = Guid.NewGuid(),
                            ClinicId = clinicDoctor.ClinicId,
                            AppointmentId = appointment.AppointmentId,
                            Amount = fee,
                            Status = "Hold"
                        };
                        await _unitOfWork.Repository<DoctorPayout>().AddAsync(payout);
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

                // [QUAN TRỌNG]: Tìm chủ hộ (Family Head) để SignalR
                Guid? headUserId = member.UserId;
                if (!headUserId.HasValue && member.FamilyId != null)
                {
                    var familyManager = await _unitOfWork.Repository<Members>().GetQueryable()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.FamilyId == member.FamilyId && m.UserId != null);
                    headUserId = familyManager?.UserId;
                }

                if (!string.IsNullOrEmpty(title))
                {
                    // 1. Gửi cho User quản lý (chủ hộ)
                    if (headUserId.HasValue)
                    {
                        await _notificationService.SendNotificationAsync(
                            userId: headUserId.Value,
                            title: title,
                            message: message,
                            type: AppointmentActionTypes.APPOINTMENT_UPDATED,
                            referenceId: appointment.AppointmentId
                        );
                    }

                    // 2. Gửi cho Bệnh nhân (người được khám)
                    await _notificationService.SendNotificationAsync(
                        userId: null,
                        title: title,
                        message: message,
                        type: AppointmentActionTypes.APPOINTMENT_UPDATED,
                        referenceId: appointment.AppointmentId,
                        memberId: member.MemberId
                    );
                }

                // SignalR Update (Gửi event refresh giao diện về cho app của Chủ hộ / Bệnh nhân trực tiếp)
                if (headUserId.HasValue)
                {
                    await _hubContext.Clients.Group($"User_{headUserId.Value}").SendAsync("AppointmentStatusUpdated", new { 
                        appointmentId = appointment.AppointmentId, 
                        status = request.Status 
                    });
                }
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
                .Include(a => a.Member)
                .Include(a => a.Doctor).ThenInclude(d => d!.User)
                .Include(a => a.Clinic)
                .Include(a => a.Payments)
                .Include(a => a.ConsultationSessions)
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
                .Include(a => a.Clinic)        // Thông tin phòng khám
                .Include(a => a.Payments)      // Thông tin thanh toán (phí)
                .Include(a => a.ConsultationSessions) // Phiên tư vấn
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
            {
                return ApiResponse<AppointmentDetailDto>.Fail("Không tìm thấy thông tin lịch hẹn.", 404);
            }

            var payment = appointment.Payments?.FirstOrDefault();
            var session = appointment.ConsultationSessions?.FirstOrDefault();

            var detail = new AppointmentDetailDto
            {
                AppointmentId = appointment.AppointmentId,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                Status = appointment.Status,
                PaymentStatus = appointment.PaymentStatus,
                CancelReason = appointment.CancelReason,
                Amount = payment?.Amount,
                CreatedAt = appointment.CreatedAt,

                // Thông tin Phòng khám
                ClinicId = appointment.ClinicId,
                ClinicName = appointment.Clinic?.Name,

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
                MemberDateOfBirth = appointment.Member?.DateOfBirth,

                // Thông tin phiên
                ConsultationSessionId = session?.ConsultanSessionId,
                ConsultationSessionStatus = session?.Status,
                RecordingUrl = session?.RecordUrl
            };

            return ApiResponse<AppointmentDetailDto>.Ok(detail, "Lấy chi tiết lịch hẹn thành công.");
        }

        public async Task<List<AppointmentDto>> GetAppointmentsByMemberIdAsync(Guid memberId)
        {
            // Lấy tất cả lịch hẹn mà MemberId trùng với tham số truyền vào
            var appointments = await _unitOfWork.Repository<Appointments>()
                .GetQueryable()
                .Include(a => a.Member)
                .Include(a => a.Doctor).ThenInclude(d => d!.User)
                .Include(a => a.Clinic)
                .Include(a => a.Payments)
                .Include(a => a.ConsultationSessions)
                .Where(a => a.MemberId == memberId)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            // Map sang DTO để trả về
            return appointments.Select(MapAppointment).ToList();
        }

        // ─────────────────────────────────────────────────────────────────
        // REFUND MANAGEMENT (ADMIN)
        // ─────────────────────────────────────────────────────────────────

        public async Task<List<AppointmentDto>> GetRefundableAppointmentsAsync()
        {
            var appointments = await _unitOfWork.Repository<Appointments>().GetQueryable()
                .Include(a => a.Member)
                .Where(a => a.PaymentStatus == "Refunded")
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return appointments.Select(MapAppointment).ToList();
        }

        public async Task<AppointmentDto> CompleteRefundAsync(Guid appointmentId, IFormFile? transferImage)
        {
            var appointment = await _unitOfWork.Repository<Appointments>().GetQueryable()
                .Include(a => a.Member)
                .Include(a => a.Payments)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
                throw new NotFoundException("Không tìm thấy lịch hẹn.");

            if (appointment.PaymentStatus != "Refunded")
                throw new BadRequestException("Chỉ có thể hoàn tất hoàn tiền cho các lịch hẹn có trạng thái Refunded.");

            // Upload ảnh chứng minh chuyển khoản hoàn tiền nếu có
            string? refundImageUrl = null;
            if (transferImage != null)
            {
                var uploadResult = await _uploadPhotoService.UploadPhotoAsync(transferImage);
                refundImageUrl = uploadResult.OriginalUrl;
            }

            // Lấy userId người đặt lịch (chủ Family) từ Payment gốc
            var originalPayment = appointment.Payments?.FirstOrDefault();
            var payerUserId = originalPayment?.UserId ?? Guid.Empty;

            // Tạo Payment mới loại Refund
            var refundPayment = new Payments
            {
                PaymentId = Guid.NewGuid(),
                AppointmentId = appointmentId,
                UserId = payerUserId,
                Amount = originalPayment?.Amount ?? 0,
                PaymentContent = $"Hoàn tiền lịch hẹn #{appointmentId}",
                Status = "RefundCompleted",
                CreatedAt = DateTime.Now
            };
            await _unitOfWork.Repository<Payments>().AddAsync(refundPayment);

            // Tạo Transaction loại OUT để ghi nhận dòng tiền ra
            var refundTransaction = new Transactions
            {
                TransactionId = Guid.NewGuid(),
                TransactionCode = $"REFUND-{appointmentId.ToString()[..8].ToUpper()}",
                PaymentId = refundPayment.PaymentId,
                GatewayName = "Manual",
                TransactionStatus = "Success", // Đồng nhất với PayOSService
                AmountPaid = refundPayment.Amount,
                TransactionType = TransactionTypes.MoneySent,
                GatewayResponse = refundImageUrl,
                PaidAt = DateTime.Now
            };
            await _unitOfWork.Repository<Transactions>().AddAsync(refundTransaction);

            appointment.PaymentStatus = "RefundCompleted";
            _unitOfWork.Repository<Appointments>().Update(appointment);

            await _unitOfWork.CompleteAsync();

            return MapAppointment(appointment);
        }

        private static AppointmentDto MapAppointment(Appointments item)
        {
            // Lấy thông tin thanh toán đầu tiên (nếu có)
            var payment = item.Payments?.FirstOrDefault();
            // Lấy session đầu tiên (nếu có)
            var session = item.ConsultationSessions?.FirstOrDefault();

            return new AppointmentDto
            {
                AppointmentId = item.AppointmentId,
                DoctorId = item.DoctorId,
                DoctorName = item.Doctor?.User?.FullName ?? item.Doctor?.FullName,
                DoctorAvatar = item.Doctor?.User?.AvatarUrl,
                ClinicId = item.ClinicId,
                ClinicName = item.Clinic?.Name,
                MemberId = item.MemberId,
                MemberName = item.Member?.FullName,
                AvailabilityId = item.AvailabilityId,
                AppointmentDate = item.AppointmentDate,
                AppointmentTime = item.AppointmentTime,
                Status = item.Status,
                PaymentStatus = item.PaymentStatus,
                CancelReason = item.CancelReason,
                Amount = payment?.Amount,
                ConsultationSessionId = session?.ConsultanSessionId,
                CreatedAt = item.CreatedAt
            };
        }

        // ─────────────────────────────────────────────────────────────────
        // CẬP NHẬT TRẠNG THÁI THANH TOÁN (WEBHOOK PayOS)
        // Chỉ đánh dấu PaymentStatus = "Paid". Status giữ nguyên "Pending",
        // chờ Bác sĩ chủ động Approve qua UpdateAppointmentAsync.
        // ─────────────────────────────────────────────────────────────────
        public async Task<AppointmentDto> UpdateAppointmentPaymentStatusAsync(Guid appointmentId, string paymentStatus)
        {
            var appointment = await _unitOfWork.Repository<Appointments>().GetQueryable()
                .Include(a => a.Member)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
                throw new NotFoundException("Không tìm thấy lịch hẹn.");

            // Idempotency: nếu đã ở trạng thái đích rồi thì bỏ qua
            if (appointment.PaymentStatus == paymentStatus)
                return MapAppointment(appointment);

            appointment.PaymentStatus = paymentStatus;

            // Khi thanh toán thành công, cập nhật trạng thái Payment và Transaction tương ứng
            if (paymentStatus == "Paid")
            {
                var payment = await _unitOfWork.Repository<Payments>().GetQueryable()
                    .Include(p => p.Transactions)
                    .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);

                if (payment != null)
                {
                    payment.Status = "Success"; // Đồng nhất với PayOSService
                    _unitOfWork.Repository<Payments>().Update(payment);

                    foreach (var tx in payment.Transactions)
                    {
                        tx.TransactionStatus = "Success"; // Đồng nhất với PayOSService
                        tx.PaidAt = DateTime.Now;
                        _unitOfWork.Repository<Transactions>().Update(tx);
                    }
                }
            }

            _unitOfWork.Repository<Appointments>().Update(appointment);
            await _unitOfWork.CompleteAsync();

            // Notify Bác sĩ: có lịch mới đã được thanh toán, đang chờ xác nhận
            if (paymentStatus == "Paid")
            {
                var doctor = await _doctorRepository.GetDoctorByIdAsync(appointment.DoctorId);
                if (doctor != null)
                {
                    var member = appointment.Member;
                    string timeStr = appointment.AppointmentTime.ToString(@"hh\:mm");
                    string dateStr = appointment.AppointmentDate.ToString("dd/MM/yyyy");

                    await _notificationService.SendNotificationAsync(
                        userId: doctor.UserId,
                        title: "💳 Lịch khám mới đã thanh toán — Chờ xác nhận của bạn",
                        message: $"Bệnh nhân {member?.FullName ?? "Không rõ"} đã thanh toán lịch khám lúc {timeStr} ngày {dateStr}. Vui lòng vào app để xác nhận hoặc từ chối.",
                        type: AppointmentActionTypes.NEW_APPOINTMENT,
                        referenceId: appointment.AppointmentId
                    );

                    await _hubContext.Clients.Group($"User_{doctor.UserId}").SendAsync("AppointmentStatusUpdated", new
                    {
                        appointmentId = appointment.AppointmentId,
                        status = appointment.Status,
                        paymentStatus = appointment.PaymentStatus
                    });
                }
            }

            return MapAppointment(appointment);
        }
    }
}
