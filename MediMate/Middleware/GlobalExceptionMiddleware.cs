using MediMateService.Shared;
using Share.Common;
using System.Net;
using System.Text.Json;

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
            int statusCode;
            string? businessCode = null;
            string? field = null;
            string message = exception.Message;

            statusCode = exception switch
            {
                NotFoundException => (int)HttpStatusCode.NotFound,
                ForbiddenException => (int)HttpStatusCode.Forbidden,
                BadRequestException => (int)HttpStatusCode.BadRequest,
                ConflictException => (int)HttpStatusCode.Conflict,
                _ => (int)HttpStatusCode.InternalServerError
            };

            if (exception is ServiceException serviceException)
            {
                businessCode = serviceException.BusinessCode;
                field = serviceException.Field;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = ApiResponse<object>.Fail(message, statusCode, businessCode, field);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(response, jsonOptions);

            return context.Response.WriteAsync(json);
        }
    }
}