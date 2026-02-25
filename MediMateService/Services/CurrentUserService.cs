namespace MediMateService.Services
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        Task<bool> CheckAccess(Guid memberId, Guid userId);
        // Sau này có thể thêm: string Email { get; } hoặc string Role { get; }
    }
}
