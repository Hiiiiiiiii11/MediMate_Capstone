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

            // Trừ đi 1 lượt khám
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

            // 1. Kiểm tra ngày đó bác sĩ có xin nghỉ phép (Exception) không?
            var isDayOff = (await _unitOfWork.Repository<DoctorAvailabilityExceptions>()
                .FindAsync(e => e.DoctorId == doctorId
                                && e.Date.Date == date.Date
                                && !e.IsAvailableOverride)).Any();

            if (isDayOff)
            {
                return ApiResponse<List<AvailableSlotDto>>.Ok(result, "Bác sĩ xin nghỉ phép ngày này.");
            }

            // 2. Tìm ca làm việc của Bác sĩ dựa vào Thứ (DayOfWeek)
            string dayOfWeekString = date.DayOfWeek.ToString(); // VD: "Monday"
            var availabilities = await _unitOfWork.Repository<DoctorAvailability>()
                .FindAsync(a => a.DoctorId == doctorId
                                && a.DayOfWeek == dayOfWeekString
                                && a.IsActive);

            if (!availabilities.Any())
            {
                return ApiResponse<List<AvailableSlotDto>>.Ok(result, "Bác sĩ không có ca làm việc vào thứ này.");
            }

            // 3. Lấy danh sách CÁC KHUNG GIỜ ĐÃ BỊ ĐẶT trong ngày đó
            var bookedAppointments = await _unitOfWork.Repository<Appointments>()
                .FindAsync(a => a.DoctorId == doctorId
                                && a.AppointmentDate.Date == date.Date
                                && a.Status != "Cancelled"
                                && a.Status != "Rejected");

            // Tạo 1 list chỉ chứa con số (ví dụ: [8, 9, 14])
            var bookedTimes = bookedAppointments.Select(a => a.AppointmentTime).ToList();

            // 4. Sinh ra các Slot 1 tiếng dựa trên ca làm việc
            // (Bác sĩ có thể có 2 ca: Sáng 8h-12h, Chiều 13h-17h, nên phải dùng foreach)
            foreach (var shift in availabilities)
            {
                // Vì model của bạn đang dùng TimeSpan, ta lấy property Hours
                int startHour = shift.StartTime.Hours;
                int endHour = shift.EndTime.Hours;

                // Cắt mỗi slot 1 tiếng
                for (int hour = startHour; hour < endHour; hour++)
                {
                    result.Add(new AvailableSlotDto
                    {
                        Time = hour,
                        DisplayTime = $"{hour:D2}:00 - {hour + 1:D2}:00", // Format đẹp: "08:00 - 09:00"
                        IsBooked = bookedTimes.Contains(hour) // Nếu mảng bookedTimes có số này -> Đã bị đặt
                    });
                }
            }

            // 5. Sắp xếp giờ từ sáng đến chiều cho đẹp
            result = result.OrderBy(x => x.Time).ToList();

            return ApiResponse<List<AvailableSlotDto>>.Ok(result, "Lấy danh sách giờ khám thành công.");
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
