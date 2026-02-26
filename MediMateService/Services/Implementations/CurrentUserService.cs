using MediMateRepository.Model;
using MediMateRepository.Repositories;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace MediMateService.Services.Implementations
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUnitOfWork _unitOfWork;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IUnitOfWork unitOfWork)
        {
            _httpContextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;
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

                return string.IsNullOrEmpty(userIdString)
                    ? throw new UnauthorizedAccessException("Token does not contain UserId claim.")
                    : Guid.TryParse(userIdString, out var userId)
                    ? userId
                    : throw new UnauthorizedAccessException("UserId in Token is not a valid Guid.");
            }
        }
        public async Task<bool> CheckAccess(Guid memberId, Guid userId)
        {
            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (member == null)
            {
                return false;
            }

            if (member.UserId == userId)
            {
                return true;
            }

            if (member.FamilyId != null)
            {
                var requester = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == member.FamilyId && m.UserId == userId)).FirstOrDefault();
                return requester != null;
            }
            return false;
        }
    }
}
