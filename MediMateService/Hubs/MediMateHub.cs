using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MediMateService.Hubs
{
    // [Authorize] // Đảm bảo chỉ User có Token mới kết nối được
    public class MediMateHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserIdFromContext();

            if (!string.IsNullOrEmpty(userId))
            {
                // Join vào group cá nhân để nhận thông báo/appointment riêng
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

                // Gợi ý: Nếu muốn nhận thông báo chung của cả gia đình, 
                // bạn có thể lấy thêm FamilyId từ Claim và join vào group "Family_{familyId}"
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserIdFromContext();
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Copy logic "quét" ID từ CurrentUserService sang đây
        private string? GetUserIdFromContext()
        {
            var user = Context.User;
            if (user == null) return null;

            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user.FindFirst("sub")?.Value
                   ?? user.FindFirst("Id")?.Value
                   ?? user.FindFirst("MemberId")?.Value
                   ?? user.FindFirst("UserId")?.Value;
        }
    }
}