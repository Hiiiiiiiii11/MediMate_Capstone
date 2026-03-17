using FirebaseAdmin.Messaging;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediMateService.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFirebaseNotificationService _firebaseService; // Inject service Firebase của bạn vào đây

        public NotificationService(IUnitOfWork unitOfWork, IFirebaseNotificationService firebaseService)
        {
            _unitOfWork = unitOfWork;
            _firebaseService = firebaseService;
        }

        public async Task<ApiResponse<bool>> SendNotificationAsync(Guid userId, string title, string message, string type, Guid? referenceId = null)
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
                // Lấy User từ DB để lấy FcmToken
                var targetUser = await _unitOfWork.Repository<User>().GetByIdAsync(userId);

                if (targetUser != null && !string.IsNullOrEmpty(targetUser.FcmToken))
                {
                    // Data ngầm định gửi kèm để Frontend (Flutter/React Native) 
                    // biết chuyển đến màn hình nào khi người dùng bấm vào thông báo.
                    var payloadData = new Dictionary<string, string>
                    {
                        { "type", type }, // VD: "NEW_APPOINTMENT", "APPOINTMENT_APPROVED"
                        { "referenceId", referenceId?.ToString() ?? "" }
                    };

                    // Gọi sang service Firebase hiện có của bạn
                    await _firebaseService.SendNotificationAsync(targetUser.FcmToken, title, message, payloadData);
                }

                return ApiResponse<bool>.Ok(true, "Gửi thông báo thành công.");
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu muốn
                return ApiResponse<bool>.Fail($"Lỗi khi gửi thông báo: {ex.Message}", 500);
            }
        }
    }
}