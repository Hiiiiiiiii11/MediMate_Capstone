using FirebaseAdmin.Messaging;

namespace MediMateService.Services.Implementations
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        public async Task<bool> SendNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string> data = null)
        {
            if (string.IsNullOrEmpty(fcmToken)) return false;

            try
            {
                var message = new Message()
                {
                    Token = fcmToken, // Địa chỉ máy nhận
                    Notification = new Notification()
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data // Dữ liệu ngầm định gửi kèm để Frontend chuyển màn hình (VD: { "type": "reminder", "id": "123" })
                };

                // Gọi lên server của Google Firebase
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                Console.WriteLine($"[FIREBASE] Đã gửi thông báo thành công: {response}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FIREBASE ERROR] Lỗi gửi thông báo: {ex.Message}");
                return false;
            }
        }
    }
}