using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using System.Text.Json; // Dùng để parse JSON

namespace MediMateService.Services.Implementations
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ActivityLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // --- 1. HÀM GHI LOG (DÙNG NỘI BỘ TRONG CÁC SERVICE KHÁC) ---
        public async Task LogActivityAsync(Guid familyId, Guid memberId, string actionType, string entityName, Guid entityId, string description, object? oldData = null, object? newData = null)
        {
            try
            {
                // Cấu hình Serialize JSON (Bỏ qua những field bị null để tiết kiệm dung lượng DB)
                var jsonOptions = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

                var log = new ActivityLogs
                {
                    LogId = Guid.NewGuid(),
                    FamilyId = familyId,
                    MemberId = memberId,
                    ActionType = actionType,
                    EntityName = entityName,
                    EntityId = entityId,
                    Description = description,
                    OldDataJson = oldData != null ? JsonSerializer.Serialize(oldData, jsonOptions) : string.Empty,
                    NewDataJson = newData != null ? JsonSerializer.Serialize(newData, jsonOptions) : string.Empty,
                    CreateAt = DateTime.Now
                };

                await _unitOfWork.Repository<ActivityLogs>().AddAsync(log);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception)
            {
                // Lưu ý quan trọng: Lỗi ghi log KHÔNG ĐƯỢC LÀM CRASH luồng chính.
                // Nếu không ghi log được thì chỉ nên ghi ra File/Console bằng ILogger
                // _logger.LogError(ex, "Failed to write activity log");
            }
        }

        // --- 2. HÀM LẤY DANH SÁCH LOG ĐỂ HIỂN THỊ LÊN APP ---
        public async Task<ApiResponse<IEnumerable<ActivityLogResponse>>> GetFamilyActivitiesAsync(Guid familyId, Guid currentUserId, int page = 1, int pageSize = 20)
        {
            // 1. Kiểm tra xem user có thuộc family này không
            var requester = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId && (m.UserId == currentUserId || m.MemberId == currentUserId))).FirstOrDefault();

            if (requester == null)
            {
                return ApiResponse<IEnumerable<ActivityLogResponse>>.Fail("Bạn không có quyền xem hoạt động của gia đình này.", 403);
            }

            // 2. Truy vấn Logs kết hợp với Tên Member (Lấy phân trang để app chạy mượt)
            // Lấy từ mới nhất đến cũ nhất
            var logs = await _unitOfWork.Repository<ActivityLogs>()
                .FindAsync(al => al.FamilyId == familyId);

            var pagedLogs = logs
                .OrderByDescending(al => al.CreateAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 3. Map Tên Member
            // Lấy danh sách ID của những người có trong log đợt này
            var memberIdsInLog = pagedLogs.Select(l => l.MemberId).Distinct().ToList();
            var members = await _unitOfWork.Repository<Members>()
                .FindAsync(m => memberIdsInLog.Contains(m.MemberId));

            var memberDict = members.ToDictionary(m => m.MemberId, m => m.FullName);

            var responseList = pagedLogs.Select(log => new ActivityLogResponse
            {
                LogId = log.LogId,
                MemberId = log.MemberId,
                MemberName = memberDict.ContainsKey(log.MemberId) ? memberDict[log.MemberId] : "Thành viên đã rời đi",
                ActionType = log.ActionType,
                EntityName = log.EntityName,
                Description = log.Description,
                OldDataJson = log.OldDataJson,
                NewDataJson = log.NewDataJson,
                CreateAt = log.CreateAt
            });

            return ApiResponse<IEnumerable<ActivityLogResponse>>.Ok(responseList, "Lấy lịch sử hoạt động thành công.");
        }
        
    }
}