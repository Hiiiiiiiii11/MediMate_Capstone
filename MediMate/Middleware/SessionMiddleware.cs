using MediMateRepository.Model;
using MediMateRepository.Repositories;
using Microsoft.EntityFrameworkCore;

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
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token) && context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                var userIdClaim = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                                  ?? context.User.FindFirst("Sub")?.Value
                                  ?? context.User.FindFirst("MemberId")?.Value; // Lấy cả MemberId nếu là Dependent

                var role = context.User.FindFirst("Role")?.Value;

                if (Guid.TryParse(userIdClaim, out Guid accountId))
                {
                    bool isKickedOut = false;

                    // NẾU LÀ NGƯỜI PHỤ THUỘC (MEMBER)
                    if (role == "Dependent")
                    {
                        // QUAN TRỌNG: Dùng AsNoTracking trong Middleware để không ảnh hưởng Tracker hệ thống
                        var member = await unitOfWork.Repository<Members>().GetQueryable()
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync(m => m.MemberId == accountId);

                        if (member != null && member.CurrentSessionToken != token) isKickedOut = true;
                    }
                    // NẾU LÀ USER CHÍNH HOẶC DOCTOR
                    else
                    {
                        var user = await unitOfWork.Repository<User>().GetQueryable()
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync(u => u.UserId == accountId);

                        if (user != null && user.CurrentSessionToken != token) isKickedOut = true;
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

