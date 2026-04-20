using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Microsoft.EntityFrameworkCore;
using Share.Common;
using Share.Constants;
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
            if (doctor == null) return ApiResponse<DoctorAvailabilityExceptionDto>.Fail("Không tìm thấy bác sĩ.", 404);

            if (doctor.UserId != currentUserId)
                return ApiResponse<DoctorAvailabilityExceptionDto>.Fail("Không có quyền.", 403);

            // Chống trùng lặp
            var isOverlap = await _unitOfWork.Repository<DoctorAvailabilityExceptions>()
                .GetQueryable()
                .AnyAsync(e => e.DoctorId == doctorId
                            && e.Date.Date == request.Date.Date
                            && request.StartTime < e.EndTime
                            && e.StartTime < request.EndTime);

            if (isOverlap) return ApiResponse<DoctorAvailabilityExceptionDto>.Fail("Khung giờ này đã tồn tại.", 409);

            var exception = new DoctorAvailabilityExceptions
            {
                ExceptionId = Guid.NewGuid(),
                DoctorId = doctorId,
                Date = request.Date.Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Reason = request.Reason,
                IsAvailableOverride = request.IsAvailableOverride, // false = Nghỉ, true = Tăng ca
                Status = DoctorExceptionStatuses.PENDING // Luôn là Pending khi mới tạo
            };

            await _unitOfWork.Repository<DoctorAvailabilityExceptions>().AddAsync(exception);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorAvailabilityExceptionDto>.Ok(MapToDto(exception), "Gửi yêu cầu thành công.");
        }

        public async Task<ApiResponse<PagedResult<DoctorAvailabilityExceptionDto>>> GetAllAsync(DoctorAvailabilityExceptionFilter filter)
        {
            filter ??= new DoctorAvailabilityExceptionFilter();
            if (filter.PageNumber < 1) filter.PageNumber = 1;
            if (filter.PageSize < 1) filter.PageSize = 10;

            IQueryable<DoctorAvailabilityExceptions> query = _unitOfWork.Repository<DoctorAvailabilityExceptions>()
                .GetQueryable()
                .Include(d => d.Doctor);

            if (filter.DoctorId.HasValue)
            {
                query = query.Where(e => e.DoctorId == filter.DoctorId.Value);
            }
            if (!string.IsNullOrEmpty(filter.Status))
            {
                // So sánh chuỗi (có thể dùng ToLower để an toàn hơn)
                query = query.Where(e => e.Status == filter.Status);
            }
            if (filter.DateFrom.HasValue)
            {
                query = query.Where(e => e.Date.Date >= filter.DateFrom.Value.Date);
            }
            if (filter.DateTo.HasValue)
            {
                query = query.Where(e => e.Date.Date <= filter.DateTo.Value.Date);
            }

            var ordered = filter.IsDescending
                ? query.OrderByDescending(e => e.Date)
                : query.OrderBy(e => e.Date);

            var totalCount = ordered.Count();
            var items = ordered
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(MapToDto)
                .ToList();

            var result = new PagedResult<DoctorAvailabilityExceptionDto>
            {
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                Items = items
            };

            return ApiResponse<PagedResult<DoctorAvailabilityExceptionDto>>.Ok(result);
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

            if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime >= request.EndTime)
            {
                return ApiResponse<DoctorAvailabilityExceptionDto>.Fail("Giờ bắt đầu phải sớm hơn giờ kết thúc.", 400);
            }

            // --- CẬP NHẬT CÁC TRƯỜNG DỮ LIỆU ---
            exception.Date = request.Date.Date;
            exception.StartTime = request.StartTime;
            exception.EndTime = request.EndTime;
            exception.Reason = request.Reason;
            exception.IsAvailableOverride = request.IsAvailableOverride;

            // ✅ BỔ SUNG DÒNG NÀY ĐỂ CẬP NHẬT STATUS
            if (!string.IsNullOrEmpty(request.Status))
            {
                exception.Status = request.Status;
            }

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
                DoctorName = e.Doctor?.FullName ?? "Unknown",
                Date = e.Date,
                Status = e.Status,
                // Kiểm tra HasValue, nếu có thì format, không thì gán null
                StartTime = e.StartTime.HasValue ? e.StartTime.Value.ToString(@"hh\:mm") : null,
                EndTime = e.EndTime.HasValue ? e.EndTime.Value.ToString(@"hh\:mm") : null,
                Reason = e.Reason,
                IsAvailableOverride = e.IsAvailableOverride
            };
        }
    }
}