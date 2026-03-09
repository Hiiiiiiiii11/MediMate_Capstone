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

        public async Task<ApiResponse<NotificationSettingResponse>> GetSettingByMemberIdAsync(Guid memberId, Guid currentUserId)
        {
            // 1. Kiểm tra quyền truy cập
            if (!await _currentUserService.CheckAccess(memberId, currentUserId))
            {
                return ApiResponse<NotificationSettingResponse>.Fail("Không có quyền truy cập cài đặt của thành viên này.", 403);
            }

            // 2. Lấy Setting
            var setting = (await _unitOfWork.Repository<NotificationSetting>()
                .FindAsync(ns => ns.MemberId == memberId)).FirstOrDefault();

            // 3. Lazy Initialization: Nếu chưa có thì tự động tạo mặc định
            if (setting == null)
            {
                setting = new NotificationSetting
                {
                    SettingId = Guid.NewGuid(),
                    MemberId = memberId,
                    EnablePushNotification = true,
                    EnableEmailNotification = false,
                    EnableSmsNotification = false,
                    ReminderAdvanceMinutes = 15,
                    EnableFamilyAlert = true,
                    UpdateAt = DateTime.Now
                };

                await _unitOfWork.Repository<NotificationSetting>().AddAsync(setting);
                await _unitOfWork.CompleteAsync();
            }

            return ApiResponse<NotificationSettingResponse>.Ok(MapToResponse(setting));
        }

        public async Task<ApiResponse<NotificationSettingResponse>> UpdateSettingAsync(Guid memberId, Guid currentUserId, UpdateNotificationSettingRequest request)
        {
            // 1. Kiểm tra quyền truy cập
            if (!await _currentUserService.CheckAccess(memberId, currentUserId))
            {
                return ApiResponse<NotificationSettingResponse>.Fail("Không có quyền thay đổi cài đặt của thành viên này.", 403);
            }

            // 2. Lấy Setting (Nếu chưa có thì gọi hàm Get ở trên để nó tự tạo)
            var getResult = await GetSettingByMemberIdAsync(memberId, currentUserId);
            if (!getResult.Success) return getResult;

            var setting = await _unitOfWork.Repository<NotificationSetting>().GetByIdAsync(getResult.Data.SettingId);
            if (setting == null) return ApiResponse<NotificationSettingResponse>.Fail("Lỗi dữ liệu hệ thống.", 500);

            // 3. Clone dữ liệu cũ để ghi Log
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

            // 4. Cập nhật dữ liệu mới
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

            // 5. Lưu xuống DB nếu có thay đổi
            if (hasChanges)
            {
                setting.UpdateAt = DateTime.Now;
                _unitOfWork.Repository<NotificationSetting>().Update(setting);
                await _unitOfWork.CompleteAsync();

                // 6. Ghi Log
                var targetMember = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
                if (targetMember != null && targetMember.FamilyId.HasValue)
                {
                    var doer = (await _unitOfWork.Repository<Members>()
                        .FindAsync(m => m.FamilyId == targetMember.FamilyId && m.UserId == currentUserId)).FirstOrDefault();

                    if (doer != null)
                    {
                        var newData = new
                        {
                            setting.EnablePushNotification,
                            setting.EnableEmailNotification,
                            setting.EnableSmsNotification,
                            setting.ReminderAdvanceMinutes,
                            setting.EnableFamilyAlert,
                            setting.CustomSetting
                        };

                        await _activityLogService.LogActivityAsync(
                            familyId: targetMember.FamilyId.Value,
                            memberId: doer.MemberId,
                            actionType: ActivityActionTypes.UPDATE,
                            entityName: ActivityEntityNames.NOTIFICATION_SETTING,
                            entityId: setting.SettingId,
                            description: $"Đã thay đổi cấu hình thông báo của '{targetMember.FullName}'.",
                            oldData: oldData,
                            newData: newData
                        );
                    }
                }
            }

            return ApiResponse<NotificationSettingResponse>.Ok(MapToResponse(setting), "Cập nhật cài đặt thành công.");
        }

        private NotificationSettingResponse MapToResponse(NotificationSetting s)
        {
            return new NotificationSettingResponse
            {
                SettingId = s.SettingId,
                MemberId = s.MemberId,
                EnablePushNotification = s.EnablePushNotification,
                EnableEmailNotification = s.EnableEmailNotification,
                EnableSmsNotification = s.EnableSmsNotification,
                ReminderAdvanceMinutes = s.ReminderAdvanceMinutes,
                EnableFamilyAlert = s.EnableFamilyAlert,
                CustomSetting = s.CustomSetting,
                UpdateAt = s.UpdateAt
            };
        }
    }
}