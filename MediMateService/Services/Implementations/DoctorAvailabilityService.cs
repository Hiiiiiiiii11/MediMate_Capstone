using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediMateService.Services.Implementations
{
    public class DoctorAvailabilityService : IDoctorAvailabilityService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DoctorAvailabilityService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<DoctorAvailabilityDto>> CreateAsync(Guid doctorId, Guid currentUserId, CreateDoctorAvailabilityRequest request)
        {
            var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(doctorId);
            if (doctor == null)
                return ApiResponse<DoctorAvailabilityDto>.Fail("Không tìm thấy thông tin bác sĩ.", 404);

            //if (doctor.UserId != currentUserId)  //doc manager có thể thiết lập lịch cho bác sĩ, nên bỏ check này
            //    return ApiResponse<DoctorAvailabilityDto>.Fail("Bạn không có quyền thiết lập lịch cho bác sĩ này.", 403);

            if (request.StartTime >= request.EndTime)
                return ApiResponse<DoctorAvailabilityDto>.Fail("Giờ bắt đầu phải sớm hơn giờ kết thúc.", 400);

            // Tùy chọn: Có thể thêm logic check trùng khung giờ (Overlap) trong cùng một ngày ở đây

            var availability = new DoctorAvailability
            {
                DoctorAvailabilityId = Guid.NewGuid(),
                DoctorId = doctorId,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                IsActive = true
            };

            await _unitOfWork.Repository<DoctorAvailability>().AddAsync(availability);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorAvailabilityDto>.Ok(MapToDto(availability), "Thêm khung giờ làm việc thành công.");
        }

        public async Task<ApiResponse<IEnumerable<DoctorAvailabilityDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            var availabilities = await _unitOfWork.Repository<DoctorAvailability>()
                .FindAsync(a => a.DoctorId == doctorId);

            // Sắp xếp thứ tự ưu tiên: Thứ trong tuần -> Giờ bắt đầu
            var response = availabilities
                .OrderBy(a => GetDayOfWeekNumber(a.DayOfWeek))
                .ThenBy(a => a.StartTime)
                .Select(MapToDto);

            return ApiResponse<IEnumerable<DoctorAvailabilityDto>>.Ok(response);
        }

        public async Task<ApiResponse<DoctorAvailabilityDto>> GetByIdAsync(Guid availabilityId)
        {
            var availability = await _unitOfWork.Repository<DoctorAvailability>().GetByIdAsync(availabilityId);
            if (availability == null)
                return ApiResponse<DoctorAvailabilityDto>.Fail("Không tìm thấy lịch làm việc.", 404);

            return ApiResponse<DoctorAvailabilityDto>.Ok(MapToDto(availability));
        }

        public async Task<ApiResponse<DoctorAvailabilityDto>> UpdateAsync(Guid availabilityId, Guid currentUserId, UpdateDoctorAvailabilityRequest request)
        {
            var availability = (await _unitOfWork.Repository<DoctorAvailability>()
                .FindAsync(a => a.DoctorAvailabilityId == availabilityId, "Doctor")).FirstOrDefault();

            if (availability == null)
                return ApiResponse<DoctorAvailabilityDto>.Fail("Không tìm thấy lịch làm việc.", 404);

            //if (availability.Doctor.UserId != currentUserId) //doc manager có thể thiết lập lịch cho bác sĩ, nên bỏ check này
            //    return ApiResponse<DoctorAvailabilityDto>.Fail("Bạn không có quyền sửa lịch này.", 403);

            if (request.StartTime >= request.EndTime)
                return ApiResponse<DoctorAvailabilityDto>.Fail("Giờ bắt đầu phải sớm hơn giờ kết thúc.", 400);

            availability.DayOfWeek = request.DayOfWeek;
            availability.StartTime = request.StartTime;
            availability.EndTime = request.EndTime;
            availability.IsActive = request.IsActive;

            _unitOfWork.Repository<DoctorAvailability>().Update(availability);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorAvailabilityDto>.Ok(MapToDto(availability), "Cập nhật lịch làm việc thành công.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid availabilityId, Guid currentUserId)
        {
            var availability = (await _unitOfWork.Repository<DoctorAvailability>()
                .FindAsync(a => a.DoctorAvailabilityId == availabilityId, "Doctor")).FirstOrDefault();
            if (availability == null)
                return ApiResponse<bool>.Fail("Không tìm thấy lịch làm việc.", 404);

            var appointments = await _unitOfWork.Repository<Appointments>()
                .FindAsync(ap => ap.DoctorId == availability.DoctorId && ap.AppointmentDate.Date >= DateTime.UtcNow.Date);
            if (appointments.Any())
                return ApiResponse<bool>.Fail("Không thể xóa khung giờ này vì đã có lịch hẹn trong tương lai.", 400);

            //if (availability.Doctor.UserId != currentUserId) //doc manager có thể thiết lập lịch cho bác sĩ, nên bỏ check này
            //    return ApiResponse<bool>.Fail("Bạn không có quyền xóa lịch này.", 403);

            _unitOfWork.Repository<DoctorAvailability>().Remove(availability);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Xóa khung giờ làm việc thành công.");
        }

        private DoctorAvailabilityDto MapToDto(DoctorAvailability a)
        {
            return new DoctorAvailabilityDto
            {
                DoctorAvailabilityId = a.DoctorAvailabilityId,
                DoctorId = a.DoctorId,
                DayOfWeek = a.DayOfWeek,
                // Ép kiểu TimeSpan thành chuỗi định dạng "HH:mm"
                StartTime = $"{a.StartTime.Hours:D2}:{a.StartTime.Minutes:D2}",
                EndTime = $"{a.EndTime.Hours:D2}:{a.EndTime.Minutes:D2}",
                IsActive = a.IsActive
            };
        }

        // Helper để sắp xếp thứ tự hiển thị từ Thứ 2 -> Chủ nhật
        private int GetDayOfWeekNumber(string day)
        {
            return day.ToLower() switch
            {
                "monday" => 1,
                "tuesday" => 2,
                "wednesday" => 3,
                "thursday" => 4,
                "friday" => 5,
                "saturday" => 6,
                "sunday" => 7,
                _ => 8
            };
        }
    }
}