using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    namespace MediMateService.Services
    {
        public interface ICurrentUserService
        {
            Guid UserId { get; }
            // Sau này có thể thêm: string Email { get; } hoặc string Role { get; }
        }
        public class CurrentUserService : ICurrentUserService
        {
            private readonly IHttpContextAccessor _httpContextAccessor;

            public CurrentUserService(IHttpContextAccessor httpContextAccessor)
            {
                _httpContextAccessor = httpContextAccessor;
            }

            public Guid UserId
            {
                get
                {
                    var user = _httpContextAccessor.HttpContext?.User;

                    if (user == null)
                    {
                        throw new UnauthorizedAccessException("HttpContext User is null");
                    }

                    // Tìm Claim NameIdentifier (Map với 'sub' hoặc 'nameid')
                    var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                       ?? user.FindFirst("sub")?.Value
                                       ?? user.FindFirst("UserId")?.Value;

                    if (string.IsNullOrEmpty(userIdString))
                    {
                        throw new UnauthorizedAccessException("Token does not contain UserId claim.");
                    }

                    if (Guid.TryParse(userIdString, out var userId))
                    {
                        return userId;
                    }

                    throw new UnauthorizedAccessException("UserId in Token is not a valid Guid.");
                }
            }
        }
    }
}
