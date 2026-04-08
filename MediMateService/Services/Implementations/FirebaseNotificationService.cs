using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace MediMateService.Services.Implementations
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly ILogger<FirebaseNotificationService> _logger;

        public FirebaseNotificationService(ILogger<FirebaseNotificationService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> SendNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null)
        {
            if (string.IsNullOrEmpty(fcmToken))
            {
                _logger.LogWarning("[FIREBASE] Bỏ qua vì FCM Token bị rỗng/null. Title: {Title}", title);
                return false;
            }

            try
            {
                var message = new Message()
                {
                    Token = fcmToken,
                    Notification = new Notification()
                    {
                        Title = title,
                        Body = body
                    },
                    Android = new AndroidConfig()
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification()
                        {
                            ChannelId = "default",
                            Sound = "default"
                        }
                    },
                    Apns = new ApnsConfig()
                    {
                        Aps = new Aps()
                        {
                            Sound = "default",
                            ContentAvailable = true
                        }
                    },
                    Data = data
                };

                _logger.LogInformation("[FIREBASE] Đang bắt đầu bắn Noti tới Token: {Token}...", fcmToken);

                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);

                _logger.LogInformation("[FIREBASE] Đã gửi thành công! MessageId: {Response}", response);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FIREBASE ERROR] Lỗi bắn FCM: {Message}", ex.Message);
                return false;
            }
        }
    }
}