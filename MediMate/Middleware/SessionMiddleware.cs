using MediMateRepository.Model;
using MediMateRepository.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MediMate.Middleware
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork)
        {
            // 1. BỎ QUA CÁC API AUTH ĐỂ TRÁNH LỖI VÒNG LẶP KHI ĐĂNG NHẬP/ĐĂNG XUẤT
            if (context.Request.Path.StartsWithSegments("/api/v1/auth"))
            {
                await _next(context);
                return;
            }

            // 2. BỎ QUA HUB SIGNALR (SignalR đã tự quản lý connection qua Token)
            if (context.Request.Path.StartsWithSegments("/hub") || context.Request.Path.StartsWithSegments("/medimateHub"))
            {
                await _next(context);
                return;
            }

            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token) && context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                // Lấy Role bao quát nhất
                var role = context.User.FindFirst(ClaimTypes.Role)?.Value
                           ?? context.User.FindFirst("Role")?.Value
                           ?? context.User.FindFirst("typeLogin")?.Value;

                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? context.User.FindFirst("sub")?.Value
                                  ?? context.User.FindFirst("Sub")?.Value
                                  ?? context.User.FindFirst("Id")?.Value
                                  ?? context.User.FindFirst("MemberId")?.Value;

                if (Guid.TryParse(userIdClaim, out Guid accountId))
                {
                    bool isKickedOut = false;

                    if (role == "Dependent")
                    {
                        var member = await unitOfWork.Repository<Members>().GetQueryable()
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync(m => m.MemberId == accountId);

                        // [QUAN TRỌNG]: THÊM CHECK NULL ĐỂ KHÔNG ĐÁ VĂNG USER CŨ
                        if (member != null && !string.IsNullOrEmpty(member.CurrentSessionToken) && member.CurrentSessionToken != token)
                            isKickedOut = true;
                    }
                    else
                    {
                        var user = await unitOfWork.Repository<User>().GetQueryable()
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync(u => u.UserId == accountId);

                        // [QUAN TRỌNG]: THÊM CHECK NULL ĐỂ KHÔNG ĐÁ VĂNG BÁC SĨ / USER CŨ
                        if (user != null && !string.IsNullOrEmpty(user.CurrentSessionToken) && user.CurrentSessionToken != token)
                            isKickedOut = true;
                    }

                    if (isKickedOut)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"success\":false,\"message\":\"Tài khoản của bạn đã được đăng nhập trên một thiết bị khác.\"}");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}