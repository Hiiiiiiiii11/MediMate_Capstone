using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Share.Common;
using Share.Constants;

namespace MediMateService.Services.Implementations
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public AppointmentService(
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

        public async Task<AppointmentDto> CreateAppointmentAsync(Guid userId, CreateAppointmentDto request)
        {
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
            if (doctor == null || !string.Equals(doctor.Status, DoctorStatuses.Active, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotFoundException("Không tìm thấy bác sĩ phù hợp để đặt lịch.");
            }

            var availability = await _doctorRepository.GetAvailabilityByIdAsync(request.DoctorId, request.AvailabilityId);
            if (availability == null || !availability.IsActive)
            {
                throw new NotFoundException("Không tìm thấy khung giờ làm việc khả dụng.");
            }

            var appointmentDayOfWeek = request.AppointmentDate.DayOfWeek.ToString(); // VD: "Monday"
            if (!string.Equals(availability.DayOfWeek, appointmentDayOfWeek, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("Ngày đặt khám không khớp với khung giờ rảnh của bác sĩ.");
            }

            // 2. Kiểm tra bác sĩ có xin nghỉ phép ngày hôm đó không?
            var isDayOff = (await _unitOfWork.Repository<DoctorAvailabilityExceptions>()
                .FindAsync(e => e.DoctorId == request.DoctorId
                                && e.Date.Date == request.AppointmentDate.Date
                                && !e.IsAvailableOverride)).Any();
            if (isDayOff)
            {
                throw new BadRequestException("Bác sĩ đã xin nghỉ phép vào ngày này.");
            }

            var isSlotBooked = (await _unitOfWork.Repository<Appointments>()
                .FindAsync(a => a.DoctorId == request.DoctorId
                                && a.AvailabilityId == request.AvailabilityId
                                && a.AppointmentDate.Date == request.AppointmentDate.Date
                                && a.Status != "Cancelled"
                                && a.Status != "Rejected")).Any(); // Bỏ qua các lịch đã bị hủy/từ chối
            if (isSlotBooked)
            {
                throw new ConflictException("Khung giờ này trong ngày đã có bệnh nhân khác đặt. Vui lòng chọn giờ khác.");
            }
            if (member.FamilyId == null)
            {
                throw new BadRequestException("Hồ sơ không thuộc gia đình nào nên không thể kiểm tra gói dịch vụ.");
            }

            var currentDate = DateOnly.FromDateTime(DateTime.Now);
            var activeSubscription = (await _unitOfWork.Repository<FamilySubscriptions>()
                .FindAsync(s => s.FamilyId == member.FamilyId
                                && s.Status == "Active"
                                && s.StartDate <= currentDate
                                && s.EndDate >= currentDate)).FirstOrDefault();

            if (activeSubscription == null)
            {
                throw new ForbiddenException("Gia đình của bạn hiện không có gói hội viên nào đang hoạt động.");
            }

            if (activeSubscription.RemainingConsultantCount <= 0)
            {
                throw new ForbiddenException("Gia đình bạn đã sử dụng hết lượt khám bệnh online. Vui lòng gia hạn thêm gói.");
            }

            //Trừ đi 1 lượt khám
            activeSubscription.RemainingConsultantCount -= 1;
            _unitOfWork.Repository<FamilySubscriptions>().Update(activeSubscription);

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

            var doctorUser = await _unitOfWork.Repository<Doctors>().GetByIdAsync(request.DoctorId);
            if (doctorUser != null)
            {
                string timeString = request.AppointmentTime.ToString(@"hh\:mm");
                string dateString = request.AppointmentDate.ToString("dd/MM/yyyy");

                await _notificationService.SendNotificationAsync(
                    userId: doctorUser.UserId, // Bắn cho tài khoản User của Bác sĩ
                    title: "📅 Có lịch đặt khám mới!",
                    message: $"Bệnh nhân {member.FullName} vừa đặt lịch vào lúc {timeString} ngày {dateString}.",
                    type: AppointmentActionTypes.NEW_APPOINTMENT,
                    referenceId: appointment.AppointmentId
                );
            }

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

            return MapAppointment(appointment);
        }

        public async Task<List<AppointmentDto>> GetAppointmentsAsync(Guid userId)
        {
            var doctor = (await _unitOfWork.Repository<Doctors>()
             .FindAsync(d => d.UserId == userId)).FirstOrDefault();
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

        public async Task<ApiResponse<List<AvailableSlotDto>>> GetAvailableSlotsAsync(Guid doctorId, DateTime date)
        {
            var result = new List<AvailableSlotDto>();

            var isDayOff = (await _unitOfWork.Repository<DoctorAvailabilityExceptions>()
                .FindAsync(e => e.DoctorId == doctorId
                                 && e.Date.Date == date.Date
                                 && !e.IsAvailableOverride)).Any();

            if (isDayOff)
            {
                return ApiResponse<List<AvailableSlotDto>>.Ok(result, "Bác sĩ xin nghỉ phép ngày này.");
            }

            string dayOfWeekString = date.DayOfWeek.ToString();
            var availabilities = await _unitOfWork.Repository<DoctorAvailability>()
                .FindAsync(a => a.DoctorId == doctorId
                                 && a.DayOfWeek == dayOfWeekString
                                 && a.IsActive);

            if (!availabilities.Any())
            {
                return ApiResponse<List<AvailableSlotDto>>.Ok(result, "Bác sĩ không có ca làm việc vào thứ này.");
            }

            var bookedAppointments = await _unitOfWork.Repository<Appointments>()
                .FindAsync(a => a.DoctorId == doctorId
                                 && a.AppointmentDate.Date == date.Date
                                 && a.Status != "Cancelled"
                                 && a.Status != "Rejected");

            // Đổi sang list TimeSpan
            var bookedTimes = bookedAppointments.Select(a => a.AppointmentTime).ToList();

            // CHIA SLOT THỜI GIAN (Ở đây setup là 60 phút/slot)
            TimeSpan slotDuration = TimeSpan.FromMinutes(60);

            foreach (var shift in availabilities)
            {
                TimeSpan currentSlotTime = shift.StartTime;

                // Chạy vòng lặp từ StartTime đến khi chạm mốc EndTime
                while (currentSlotTime + slotDuration <= shift.EndTime)
                {
                    result.Add(new AvailableSlotDto
                    {
                        Time = currentSlotTime,
                        // Format hh\:mm để hiển thị chuẩn (VD: 08:30 - 09:00)
                        DisplayTime = $"{currentSlotTime:hh\\:mm} - {currentSlotTime + slotDuration:hh\\:mm}",
                        IsBooked = bookedTimes.Contains(currentSlotTime)
                    });

                    // Cộng thêm 30 phút cho vòng lặp tiếp theo
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

            if (appointment.Status == "Cancelled" || appointment.Status == "Completed")
            {
                throw new BadRequestException($"Không thể cập nhật lịch hẹn đã ở trạng thái {appointment.Status}.");
            }

            // NẾU TRẠNG THÁI CÓ SỰ THAY ĐỔI
            if (!string.Equals(appointment.Status, request.Status, StringComparison.OrdinalIgnoreCase))
            {
                // --- NGHIỆP VỤ: TỪ CHỐI (REJECTED) -> HOÀN LƯỢT KHÁM ---
                if (request.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase) && member?.FamilyId != null)
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
                    await _appointmentRepository.UpdateSessionAsync(session);
                }
            }

            await _unitOfWork.CompleteAsync();

            if (member != null)
            {
                string title = "";
                string message = "";

                if (request.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                {
                    title = "✅ Lịch khám đã được xác nhận";
                    message = $"Bác sĩ đã chấp nhận lịch khám của {member.FullName}. Vui lòng chuẩn bị sẵn sàng vào khung giờ đã đặt.";
                }
                else if (request.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
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
            }

            return MapAppointment(appointment);
        }
        public async Task<List<AppointmentDto>> GetAppointmentsByDoctorIdAsync(Guid doctorId)
        {
            var appointments = await _unitOfWork.Repository<Appointments>()
                .FindAsync(a => a.DoctorId == doctorId);

            return appointments
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .Select(MapAppointment)
                .ToList();
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
