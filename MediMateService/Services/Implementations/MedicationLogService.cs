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
    public class MedicationLogService : IMedicationLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MedicationLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // --- 1. GHI NHẬN HÀNG ĐỘNG UỐNG THUỐC ---
        public async Task<ApiResponse<MedicationLogResponse>> LogMedicationActionAsync(LogMedicationRequest request, Guid currentUserId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var reminder = await _unitOfWork.Repository<MedicationReminders>()
                    .GetByIdAsync(request.ReminderId);

                if (reminder == null)
                    return ApiResponse<MedicationLogResponse>.Fail("Không tìm thấy lời nhắc này.", 404);

                var schedule = await _unitOfWork.Repository<MedicationSchedules>()
                    .GetByIdAsync(reminder.ScheduleId);

                if (schedule == null)
                    return ApiResponse<MedicationLogResponse>.Fail("Không tìm thấy lịch uống thuốc.", 404);

                var member = await _unitOfWork.Repository<Members>().GetByIdAsync(schedule.MemberId);
                if (member == null) return ApiResponse<MedicationLogResponse>.Fail("Thành viên không tồn tại.", 404);

                var existingLog = (await _unitOfWork.Repository<MedicationLogs>()
                    .FindAsync(l => l.ReminderId == request.ReminderId)).FirstOrDefault();

                if (existingLog != null)
                {
                    return ApiResponse<MedicationLogResponse>.Fail("Lời nhắc này đã được xác nhận trước đó.", 409);
                }

                var now = DateTime.Now;

                // Cập nhật trạng thái của Reminder
                reminder.Status = request.Status;
                reminder.AcknowledgedAt = now;
                _unitOfWork.Repository<MedicationReminders>().Update(reminder);

                // Tạo bản ghi Log
                var log = new MedicationLogs
                {
                    LogId = Guid.NewGuid(),
                    MemberId = schedule.MemberId,
                    ScheduleId = schedule.ScheduleId,
                    ReminderId = reminder.ReminderId,
                    LogDate = reminder.ReminderDate,
                    ScheduledTime = reminder.ReminderDate.Date.Add(reminder.ReminderTime.TimeOfDay),
                    ActualTime = request.ActualTime ?? now,
                    Status = request.Status,
                    Notes = request.Notes ?? string.Empty,
                    CreatedAt = now
                };

                await _unitOfWork.Repository<MedicationLogs>().AddAsync(log);
                await _unitOfWork.CompleteAsync();

                await transaction.CommitAsync();

                // Đã sửa cách gọi hàm Map (gọi như hàm bình thường)
                var responseDto = MapToResponse(log, schedule.MedicineName);

                return ApiResponse<MedicationLogResponse>.Ok(responseDto, "Đã ghi nhận lịch sử uống thuốc.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResponse<MedicationLogResponse>.Fail($"Lỗi hệ thống: {ex.Message}", 500);
            }
        }

        // --- 2. LẤY LỊCH SỬ UỐNG THUỐC THEO THÀNH VIÊN ---
        public async Task<ApiResponse<IEnumerable<MedicationLogResponse>>> GetMemberLogsAsync(Guid memberId, Guid currentUserId, DateTime? startDate, DateTime? endDate)
        {
            var query = await _unitOfWork.Repository<MedicationLogs>()
                .FindAsync(l => l.MemberId == memberId);

            if (startDate.HasValue)
                query = query.Where(l => l.LogDate >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(l => l.LogDate <= endDate.Value.Date);

            var logs = query.OrderByDescending(l => l.ActualTime).ToList();
            var responseList = new List<MedicationLogResponse>();

            foreach (var log in logs)
            {
                var schedule = await _unitOfWork.Repository<MedicationSchedules>().GetByIdAsync(log.ScheduleId);
                string medicineName = schedule?.MedicineName ?? "Không xác định";

                // Đã sửa cách gọi hàm Map
                responseList.Add(MapToResponse(log, medicineName));
            }

            return ApiResponse<IEnumerable<MedicationLogResponse>>.Ok(responseList);
        } // Đã xóa 1 dấu ngoặc nhọn bị thừa ở đây

        // --- 3. THỐNG KÊ TUÂN THỦ LIỆU TRÌNH ---
        public async Task<ApiResponse<object>> GetAdherenceStatsAsync(Guid scheduleId, Guid currentUserId)
        {
            var logs = await _unitOfWork.Repository<MedicationLogs>()
                .FindAsync(l => l.ScheduleId == scheduleId);

            int totalLogs = logs.Count();
            if (totalLogs == 0)
                return ApiResponse<object>.Ok(new { Taken = 0, Skipped = 0, Missed = 0, AdherenceRate = 0 });

            int taken = logs.Count(l => l.Status == "Taken");
            int skipped = logs.Count(l => l.Status == "Skipped");
            int missed = logs.Count(l => l.Status == "Missed");

            double adherenceRate = Math.Round((double)taken / totalLogs * 100, 2);

            return ApiResponse<object>.Ok(new
            {
                ScheduleId = scheduleId,
                TotalLogged = totalLogs,
                Taken = taken,
                Skipped = skipped,
                Missed = missed,
                AdherenceRate = adherenceRate
            }, "Lấy thống kê thành công.");
        }
        public async Task<ApiResponse<IEnumerable<MedicationLogResponse>>> GetFamilyLogsAsync(Guid familyId, Guid currentUserId, DateTime? startDate, DateTime? endDate)
        {
            // 1. Kiểm tra quyền: currentUserId phải là thành viên của family này
            var requester = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId && m.UserId == currentUserId)).FirstOrDefault();

            if (requester == null)
            {
                return ApiResponse<IEnumerable<MedicationLogResponse>>.Fail("Bạn không có quyền xem dữ liệu của gia đình này.", 403);
            }

            // 2. Lấy danh sách TẤT CẢ MemberId trong gia đình này
            var familyMembers = await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId);

            // Tạo Dictionary để lát nữa map tên cho nhanh (Key: MemberId, Value: FullName)
            var memberDict = familyMembers.ToDictionary(m => m.MemberId, m => m.FullName);
            var memberIds = memberDict.Keys.ToList();

            if (!memberIds.Any())
            {
                return ApiResponse<IEnumerable<MedicationLogResponse>>.Ok(new List<MedicationLogResponse>());
            }

            // 3. Query bảng Logs với điều kiện MemberId nằm trong danh sách của gia đình
            var query = await _unitOfWork.Repository<MedicationLogs>()
                .FindAsync(l => memberIds.Contains(l.MemberId));

            // Filter theo ngày
            if (startDate.HasValue)
                query = query.Where(l => l.LogDate >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(l => l.LogDate <= endDate.Value.Date);

            var logs = query.OrderByDescending(l => l.ActualTime).ToList();
            var responseList = new List<MedicationLogResponse>();

            foreach (var log in logs)
            {
                var schedule = await _unitOfWork.Repository<MedicationSchedules>().GetByIdAsync(log.ScheduleId);
                string medicineName = schedule?.MedicineName ?? "Không xác định";

                // Lấy tên thành viên từ Dictionary đã tạo ở trên
                string memberName = memberDict.ContainsKey(log.MemberId) ? memberDict[log.MemberId] : "Không xác định";

                // Sử dụng hàm Map đã cập nhật
                responseList.Add(MapToResponse(log, medicineName, memberName));
            }

            return ApiResponse<IEnumerable<MedicationLogResponse>>.Ok(responseList, "Lấy danh sách thành công.");
        }

        // --- 4. HÀM MAP NỘI BỘ (Đã bỏ chữ "this") ---
        private MedicationLogResponse MapToResponse(MedicationLogs log, string medicineName,string memberName = "")
        {
            if (log == null) return null;

            return new MedicationLogResponse
            {
                LogId = log.LogId,
                MemberId = log.MemberId,
                ScheduleId = log.ScheduleId,
                ReminderId = log.ReminderId,
                MedicineName = medicineName,
                MemberName = memberName,
                LogDate = log.LogDate,
                ScheduledTime = log.ScheduledTime,
                ActualTime = log.ActualTime,
                Status = log.Status,
                Notes = log.Notes
            };
        }
    }
}