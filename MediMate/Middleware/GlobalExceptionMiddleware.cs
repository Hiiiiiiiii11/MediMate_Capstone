using Microsoft.AspNetCore.Http;
using Share.Common;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace MediMate.Middleware // Hoặc namespace phù hợp của bạn
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                // Chuyển tiếp request đi tiếp
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                // Nếu có lỗi ở bất cứ đâu, nó sẽ nhảy vào đây
                _logger.LogError(ex, "Đã xảy ra lỗi hệ thống: {Message}", ex.Message);
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // Tạo response chuẩn theo format ApiResponse
            // Lưu ý: Có thể ẩn exception.Message khi chạy Prod để bảo mật
            var response = ApiResponse<object>.ServerError(exception.Message);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Để trả về json dạng camelCase (success, code...)
            };

            var json = JsonSerializer.Serialize(response, jsonOptions);

            return context.Response.WriteAsync(json);
        }
    }
}