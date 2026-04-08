using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

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
            }
        }

        // --- 2. HÀM LẤY DANH SÁCH LOG ĐỂ HIỂN THỊ LÊN APP (ĐÃ SỬA ĐỔI SANG PHÂN TRANG CHUẨN) ---
        public async Task<ApiResponse<PagedResult<ActivityLogResponse>>> GetFamilyActivitiesAsync(Guid familyId, Guid currentUserId, int page = 1, int pageSize = 20)
        {
            // 1. Kiểm tra xem user có thuộc family này không
            var requester = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId && (m.UserId == currentUserId || m.MemberId == currentUserId))).FirstOrDefault();

            if (requester == null)
            {
                return ApiResponse<PagedResult<ActivityLogResponse>>.Fail("Bạn không có quyền xem hoạt động của gia đình này.", 403);
            }

            // 2. Xây dựng truy vấn an toàn bằng EF Core IQueryable để tối ưu RAM
            var query = _unitOfWork.Repository<ActivityLogs>().GetQueryable()
                .Where(al => al.FamilyId == familyId);

            // Đếm tổng số bản ghi TRƯỚC KHI phân trang
            var totalCount = await query.CountAsync();

            // 3. Phân trang và lấy dữ liệu
            var pagedLogs = await query
                .OrderByDescending(al => al.CreateAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 4. Map Tên Member
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
            }).ToList();

            // 5. Đóng gói dữ liệu vào PagedResult
            var result = new PagedResult<ActivityLogResponse>
            {
                Items = responseList,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
                // Nếu PagedResult của bạn có trường TotalPages, bạn có thể thêm:
                // TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return ApiResponse<PagedResult<ActivityLogResponse>>.Ok(result, "Lấy lịch sử hoạt động thành công.");
        }
    }
}