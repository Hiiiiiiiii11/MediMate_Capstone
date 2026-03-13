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
    public class DoctorAvailabilityExceptionService : IDoctorAvailabilityExceptionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DoctorAvailabilityExceptionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<DoctorAvailabilityExceptionDto>> CreateAsync(Guid doctorId, Guid currentUserId, CreateDoctorAvailabilityExceptionRequest request)
        {
            var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(doctorId);
            if (doctor == null)
                return ApiResponse<DoctorAvailabilityExceptionDto>.Fail("Không tìm thấy thông tin bác sĩ.", 404);

            if (doctor.UserId != currentUserId)
                return ApiResponse<DoctorAvailabilityExceptionDto>.Fail("Bạn không có quyền thiết lập ngoại lệ lịch cho bác sĩ này.", 403);

            // Validate logic giờ
            if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime >= request.EndTime)
            {
                return ApiResponse<DoctorAvailabilityExceptionDto>.Fail("Giờ bắt đầu phải sớm hơn giờ kết thúc.", 400);
            }

            var exception = new DoctorAvailabilityExceptions
            {
                ExceptionId = Guid.NewGuid(),
                DoctorId = doctorId,
                Date = request.Date.Date, // Chỉ lấy ngày, bỏ giờ phút giây
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Reason = request.Reason,
                IsAvailableOverride = request.IsAvailableOverride
            };

            await _unitOfWork.Repository<DoctorAvailabilityExceptions>().AddAsync(exception);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorAvailabilityExceptionDto>.Ok(MapToDto(exception), "Thêm ngoại lệ lịch làm việc thành công.");
        }

        public async Task<ApiResponse<IEnumerable<DoctorAvailabilityExceptionDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            var exceptions = await _unitOfWork.Repository<DoctorAvailabilityExceptions>()
                .FindAsync(e => e.DoctorId == doctorId);

            // Sắp xếp ngày gần nhất lên trước
            var response = exceptions.OrderByDescending(e => e.Date).Select(MapToDto);
            return ApiResponse<IEnumerable<DoctorAvailabilityExceptionDto>>.Ok(response);
        }

        public async Task<ApiResponse<DoctorAvailabilityExceptionDto>> GetByIdAsync(Guid exceptionId)
        {
            var exception = await _unitOfWork.Repository<DoctorAvailabilityExceptions>().GetByIdAsync(exceptionId);
            if (exception == null)
                return ApiResponse<DoctorAvailabilityExceptionDto>.Fail("Không tìm thấy ngoại lệ lịch.", 404);

            return ApiResponse<DoctorAvailabilityExceptionDto>.Ok(MapToDto(exception));
        }

        public async Task<ApiResponse<DoctorAvailabilityExceptionDto>> UpdateAsync(Guid exceptionId, Guid currentUserId, UpdateDoctorAvailabilityExceptionRequest request)
        {
            var exception = (await _unitOfWork.Repository<DoctorAvailabilityExceptions>()
                .FindAsync(e => e.ExceptionId == exceptionId, "Doctor")).FirstOrDefault();

            if (exception == null)
                return ApiResponse<DoctorAvailabilityExceptionDto>.Fail("Không tìm thấy ngoại lệ lịch.", 404);

            if (exception.Doctor.UserId != currentUserId)
                return ApiResponse<DoctorAvailabilityExceptionDto>.Fail("Bạn không có quyền sửa ngoại lệ này.", 403);

            if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime >= request.EndTime)
            {
                return ApiResponse<DoctorAvailabilityExceptionDto>.Fail("Giờ bắt đầu phải sớm hơn giờ kết thúc.", 400);
            }

            exception.Date = request.Date.Date;
            exception.StartTime = request.StartTime;
            exception.EndTime = request.EndTime;
            exception.Reason = request.Reason;
            exception.IsAvailableOverride = request.IsAvailableOverride;

            _unitOfWork.Repository<DoctorAvailabilityExceptions>().Update(exception);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorAvailabilityExceptionDto>.Ok(MapToDto(exception), "Cập nhật ngoại lệ lịch thành công.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid exceptionId, Guid currentUserId)
        {
            var exception = (await _unitOfWork.Repository<DoctorAvailabilityExceptions>()
                .FindAsync(e => e.ExceptionId == exceptionId, "Doctor")).FirstOrDefault();

            if (exception == null)
                return ApiResponse<bool>.Fail("Không tìm thấy ngoại lệ lịch.", 404);

            if (exception.Doctor.UserId != currentUserId)
                return ApiResponse<bool>.Fail("Bạn không có quyền xóa ngoại lệ này.", 403);

            _unitOfWork.Repository<DoctorAvailabilityExceptions>().Remove(exception);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Xóa ngoại lệ lịch thành công.");
        }

        private DoctorAvailabilityExceptionDto MapToDto(DoctorAvailabilityExceptions e)
        {
            return new DoctorAvailabilityExceptionDto
            {
                ExceptionId = e.ExceptionId,
                DoctorId = e.DoctorId,
                Date = e.Date,
                // Kiểm tra HasValue, nếu có thì format, không thì gán null
                StartTime = e.StartTime.HasValue ? e.StartTime.Value.ToString(@"hh\:mm") : null,
                EndTime = e.EndTime.HasValue ? e.EndTime.Value.ToString(@"hh\:mm") : null,
                Reason = e.Reason,
                IsAvailableOverride = e.IsAvailableOverride
            };
        }
    }
}