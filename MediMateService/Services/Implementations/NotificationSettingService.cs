using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using Share.Constants;

namespace MediMateService.Services.Implementations
{
    public class NotificationSettingService : INotificationSettingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IActivityLogService _activityLogService;

        public NotificationSettingService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IActivityLogService activityLogService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _activityLogService = activityLogService;
        }

        public async Task<ApiResponse<NotificationSettingResponse>> GetSettingByFamilyIdAsync(Guid familyId, Guid currentUserId)
        {
            // Kiểm tra user có trong gia đình không
            var isMember = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.FamilyId == familyId && m.UserId == currentUserId)).Any();

            if (!isMember) return ApiResponse<NotificationSettingResponse>.Fail("Không có quyền truy cập.", 403);

            var setting = (await _unitOfWork.Repository<NotificationSetting>()
                .FindAsync(ns => ns.FamilyId == familyId)).FirstOrDefault();

            if (setting == null)
            {
                // Fallback nếu vì lý do nào đó gia đình cũ chưa có setting
                setting = new NotificationSetting
                {
                    SettingId = Guid.NewGuid(),
                    FamilyId = familyId,
                    EnablePushNotification = true,
                    EnableFamilyAlert = true,
                    ReminderAdvanceMinutes = 15,
                    CustomSetting = "{\"autoSnooze\":true}",
                    UpdateAt = DateTime.Now
                };
                await _unitOfWork.Repository<NotificationSetting>().AddAsync(setting);
                await _unitOfWork.CompleteAsync();
            }

            return ApiResponse<NotificationSettingResponse>.Ok(MapToResponse(setting));
        }

        public async Task<ApiResponse<NotificationSettingResponse>> UpdateSettingAsync(Guid familyId, Guid currentUserId, UpdateNotificationSettingRequest request)
        {
            var isMember = (await _unitOfWork.Repository<Members>()
                 .FindAsync(m => m.FamilyId == familyId && (m.UserId == currentUserId || m.MemberId == currentUserId))).Any();

            if (!isMember) return ApiResponse<NotificationSettingResponse>.Fail("Không có quyền chỉnh sửa.", 403);

            var getResult = await GetSettingByFamilyIdAsync(familyId, currentUserId);
            if (!getResult.Success) return getResult;

            var setting = await _unitOfWork.Repository<NotificationSetting>().GetByIdAsync(getResult.Data.SettingId);
            if (setting == null) return ApiResponse<NotificationSettingResponse>.Fail("Lỗi hệ thống", 500);

            // Clone dữ liệu cũ để ghi Log
            var oldData = new
            {
                setting.EnablePushNotification,
                setting.EnableEmailNotification,
                setting.EnableSmsNotification,
                setting.ReminderAdvanceMinutes,
                setting.EnableFamilyAlert,
                setting.CustomSetting
            };
            bool hasChanges = false;

            // Cập nhật dữ liệu mới
            if (request.EnablePushNotification.HasValue && setting.EnablePushNotification != request.EnablePushNotification.Value)
            {
                setting.EnablePushNotification = request.EnablePushNotification.Value;
                hasChanges = true;
            }
            if (request.EnableEmailNotification.HasValue && setting.EnableEmailNotification != request.EnableEmailNotification.Value)
            {
                setting.EnableEmailNotification = request.EnableEmailNotification.Value;
                hasChanges = true;
            }
            if (request.EnableSmsNotification.HasValue && setting.EnableSmsNotification != request.EnableSmsNotification.Value)
            {
                setting.EnableSmsNotification = request.EnableSmsNotification.Value;
                hasChanges = true;
            }
            if (request.ReminderAdvanceMinutes.HasValue && setting.ReminderAdvanceMinutes != request.ReminderAdvanceMinutes.Value)
            {
                setting.ReminderAdvanceMinutes = request.ReminderAdvanceMinutes.Value;
                hasChanges = true;
            }
            if (request.EnableFamilyAlert.HasValue && setting.EnableFamilyAlert != request.EnableFamilyAlert.Value)
            {
                setting.EnableFamilyAlert = request.EnableFamilyAlert.Value;
                hasChanges = true;
            }
            if (request.CustomSetting != null && setting.CustomSetting != request.CustomSetting)
            {
                setting.CustomSetting = request.CustomSetting;
                hasChanges = true;
            }
            if (request.MinimumHoursGap.HasValue && setting.MinimumHoursGap != request.MinimumHoursGap.Value)
            {
                setting.MinimumHoursGap = request.MinimumHoursGap.Value;
                hasChanges = true;
            }
            if (request.MaxDosesPerDay.HasValue && setting.MaxDosesPerDay != request.MaxDosesPerDay.Value)
            {
                setting.MaxDosesPerDay = request.MaxDosesPerDay.Value;
                hasChanges = true;
            }
            if (request.MissedDosesThreshold.HasValue && setting.MissedDosesThreshold != request.MissedDosesThreshold.Value)
            {
                setting.MissedDosesThreshold = request.MissedDosesThreshold.Value;
                hasChanges = true;
            }

            // Lưu xuống DB nếu có thay đổi
            if (hasChanges)
            {
                setting.UpdateAt = DateTime.Now;
                _unitOfWork.Repository<NotificationSetting>().Update(setting);
                await _unitOfWork.CompleteAsync();

                // Ghi Log cho Family
                var doer = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == familyId && m.UserId == currentUserId)).FirstOrDefault();

                if (doer != null)
                {
                    var newData = new
                    {
                        setting.EnablePushNotification,
                        setting.EnableEmailNotification,
                        setting.EnableSmsNotification,
                        setting.ReminderAdvanceMinutes,
                        setting.EnableFamilyAlert,
                        setting.CustomSetting,
                        setting.MinimumHoursGap,
                        setting.MaxDosesPerDay,
                        setting.MissedDosesThreshold
                    };

                    await _activityLogService.LogActivityAsync(
                        familyId: familyId,
                        memberId: doer.MemberId,
                        actionType: ActivityActionTypes.UPDATE,
                        entityName: ActivityEntityNames.NOTIFICATION_SETTING,
                        entityId: setting.SettingId,
                        description: "Đã thay đổi cấu hình thông báo của gia đình.",
                        oldData: oldData,
                        newData: newData
                    );
                }
            }

            return ApiResponse<NotificationSettingResponse>.Ok(MapToResponse(setting), "Cập nhật cài đặt thành công.");
        }

        private NotificationSettingResponse MapToResponse(NotificationSetting s)
        {
            return new NotificationSettingResponse
            {
                SettingId = s.SettingId,
                FamilyId = s.FamilyId,
                EnablePushNotification = s.EnablePushNotification,
                EnableEmailNotification = s.EnableEmailNotification,
                EnableSmsNotification = s.EnableSmsNotification,
                ReminderAdvanceMinutes = s.ReminderAdvanceMinutes,
                EnableFamilyAlert = s.EnableFamilyAlert,
                CustomSetting = s.CustomSetting,
                MinimumHoursGap = s.MinimumHoursGap,
                MaxDosesPerDay = s.MaxDosesPerDay,
                MissedDosesThreshold = s.MissedDosesThreshold,
                UpdateAt = s.UpdateAt
            };
        }
    }
}