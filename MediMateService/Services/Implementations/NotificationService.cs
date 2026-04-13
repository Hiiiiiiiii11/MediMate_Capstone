using FirebaseAdmin.Messaging;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using MediMateService.Hubs;

namespace MediMateService.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFirebaseNotificationService _firebaseService; // Inject service Firebase của bạn vào đây
        private readonly IHubContext<MediMateHub> _hubContext;

        public NotificationService(IUnitOfWork unitOfWork, IFirebaseNotificationService firebaseService, IHubContext<MediMateHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _firebaseService = firebaseService;
            _hubContext = hubContext;
        }

        public async Task<ApiResponse<bool>> SendNotificationAsync(Guid? userId, string title, string message, string type, Guid? referenceId = null, Guid? memberId = null)
        {
            try
            {
                // ==========================================
                // 1. LƯU THÔNG BÁO VÀO DATABASE
                // ==========================================
                var notification = new Notifications
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = userId,
                    MemberId = memberId,
                    Title = title,
                    Message = message,
                    Type = type,
                    ReferenceId = referenceId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.Repository<Notifications>().AddAsync(notification);
                await _unitOfWork.CompleteAsync();

                // ==========================================
                // 2. BẮN PUSH NOTIFICATION XUỐNG THIẾT BỊ QUA FIREBASE
                // ==========================================
                string? targetFcmToken = null;

                if (userId.HasValue)
                {
                    var targetUser = await _unitOfWork.Repository<User>().GetByIdAsync(userId.Value);
                    targetFcmToken = targetUser?.FcmToken;
                }
                else if (memberId.HasValue)
                {
                    var targetMember = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId.Value);
                    targetFcmToken = targetMember?.FcmToken;
                }

                if (!string.IsNullOrEmpty(targetFcmToken))
                {
                    var payloadData = new Dictionary<string, string>
                    {
                        { "type", type },
                        { "referenceId", referenceId?.ToString() ?? "" }
                    };

                    await _firebaseService.SendNotificationAsync(targetFcmToken, title, message, payloadData);
                }

                // SignalR Push Update
                if (userId.HasValue)
                {
                    await _hubContext.Clients.Group($"User_{userId.Value}").SendAsync("ReceiveNotification", new { title, message, type, referenceId, createdAt = DateTime.Now });
                }
                if (memberId.HasValue)
                {
                    await _hubContext.Clients.Group($"User_{memberId.Value}").SendAsync("ReceiveNotification", new { title, message, type, referenceId, createdAt = DateTime.Now });
                }

                return ApiResponse<bool>.Ok(true, "Gửi thông báo thành công.");
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu muốn
                return ApiResponse<bool>.Fail($"Lỗi khi gửi thông báo: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<IEnumerable<NotificationDto>>> GetUserNotificationsAsync(Guid? userId = null, Guid? memberId = null)
        {
            var notifications = await _unitOfWork.Repository<Notifications>()
                .FindAsync(n => (userId.HasValue && n.UserId == userId.Value) || (memberId.HasValue && n.MemberId == memberId.Value));

            // Sắp xếp mới nhất lên đầu và map sang DTO
            var result = notifications
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    ReferenceId = n.ReferenceId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToList();

            return ApiResponse<IEnumerable<NotificationDto>>.Ok(result);
        }

        // ==========================================
        // 2. ĐÁNH DẤU 1 THÔNG BÁO LÀ ĐÃ ĐỌC
        // ==========================================
        public async Task<ApiResponse<bool>> MarkAsReadAsync(Guid notificationId, Guid? userId = null, Guid? memberId = null)
        {
            var notification = await _unitOfWork.Repository<Notifications>().GetByIdAsync(notificationId);

            if (notification == null)
                return ApiResponse<bool>.Fail("Không tìm thấy thông báo.", 404);

            // Bảo mật: Chỉ chủ nhân của thông báo mới được phép đánh dấu đọc
            bool isOwner = (userId.HasValue && notification.UserId == userId.Value) || (memberId.HasValue && notification.MemberId == memberId.Value);
            if (!isOwner)
                return ApiResponse<bool>.Fail("Bạn không có quyền cập nhật thông báo này.", 403);

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _unitOfWork.Repository<Notifications>().Update(notification);
                await _unitOfWork.CompleteAsync();

                // SignalR Push Update
                if (userId.HasValue) await _hubContext.Clients.Group($"User_{userId.Value}").SendAsync("ReceiveNotificationUpdate");
                if (memberId.HasValue) await _hubContext.Clients.Group($"User_{memberId.Value}").SendAsync("ReceiveNotificationUpdate");
            }

            return ApiResponse<bool>.Ok(true, "Đã đánh dấu đọc.");
        }

        // ==========================================
        // 3. ĐÁNH DẤU TẤT CẢ LÀ ĐÃ ĐỌC (Nút "Đọc tất cả" trên App)
        // ==========================================
        public async Task<ApiResponse<bool>> MarkAllAsReadAsync(Guid? userId = null, Guid? memberId = null)
        {
            // Chỉ lấy những cái chưa đọc ra để update cho nhẹ Database
            var unreadNotifications = (await _unitOfWork.Repository<Notifications>()
                .FindAsync(n => ((userId.HasValue && n.UserId == userId.Value) || (memberId.HasValue && n.MemberId == memberId.Value)) && !n.IsRead)).ToList();

            if (unreadNotifications.Any())
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    _unitOfWork.Repository<Notifications>().Update(notification);
                }
                await _unitOfWork.CompleteAsync();

                // SignalR Push Update
                if (userId.HasValue) await _hubContext.Clients.Group($"User_{userId.Value}").SendAsync("ReceiveNotificationUpdate");
                if (memberId.HasValue) await _hubContext.Clients.Group($"User_{memberId.Value}").SendAsync("ReceiveNotificationUpdate");
            }

            return ApiResponse<bool>.Ok(true, "Đã đánh dấu đọc tất cả.");
        }
    }
}