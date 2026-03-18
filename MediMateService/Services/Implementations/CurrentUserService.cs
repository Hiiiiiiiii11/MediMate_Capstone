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

                // Tự động quét để lấy ID từ BẤT KỲ loại Token nào (User hoặc Dependent)
                var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? user.FindFirst("sub")?.Value
                                   ?? user.FindFirst("Id")?.Value
                                   ?? user.FindFirst("MemberId")?.Value
                                   ?? user.FindFirst("UserId")?.Value;

                return string.IsNullOrEmpty(userIdString)
                    ? throw new UnauthorizedAccessException("Token does not contain any valid ID claim.")
                    : Guid.TryParse(userIdString, out var userId)
                    ? userId
                    : throw new UnauthorizedAccessException("ID in Token is not a valid Guid.");
            }
        }
        public async Task<bool> CheckAccess(Guid memberId, Guid callerId)
        {
            // 1. Tự xem hồ sơ của chính mình (Hỗ trợ cả Dependent tự xem hồ sơ của nó)
            if (memberId == callerId) return true;

            var member = await _unitOfWork.Repository<Members>().GetByIdAsync(memberId);
            if (member == null) return false;

            // 2. User (Chủ hộ) xem hồ sơ của Member do chính mình tạo ra
            if (member.UserId == callerId) return true;

            // 3. Xem chéo hồ sơ của người khác nhưng CÙNG TRONG MỘT GIA ĐÌNH
            if (member.FamilyId != null)
            {
                // [QUAN TRỌNG NHẤT Ở ĐÂY]: Hỗ trợ cả User (m.UserId) và Dependent (m.MemberId)
                var requester = (await _unitOfWork.Repository<Members>()
                    .FindAsync(m => m.FamilyId == member.FamilyId && (m.UserId == callerId || m.MemberId == callerId))).FirstOrDefault();

                if (requester != null) return true;
            }

            return false;
        }


    }
}
