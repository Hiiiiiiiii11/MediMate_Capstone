using MediMateRepository.Model;

namespace MediMateRepository.Repositories
{
    // 1. Cập nhật Interface
    public interface IAuthenticationRepository : IGenericRepository<User>
    {
        Task<User?> GetUserByEmailOrPhoneAsync(string identifier);
        Task<bool> IsUserExistsAsync(string phone, string email);
    }
}