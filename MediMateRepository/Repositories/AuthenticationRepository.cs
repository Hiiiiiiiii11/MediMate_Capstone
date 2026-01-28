using MediMateRepository.Data;
using MediMateRepository.Model;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MediMateRepository.Repositories
{
    // 1. Cập nhật Interface
    public interface IAuthenticationRepository : IGenericRepository<User>
    {
        Task<User?> GetUserByEmailOrPhoneAsync(string identifier);
        Task<bool> IsUserExistsAsync(string phone, string email);
    }

    // 2. Cập nhật Implementation
    public class AuthenticationRepository : GenericRepository<User>, IAuthenticationRepository
    {
        public AuthenticationRepository(MediMateDbContext context) : base(context)
        {
        }

        public async Task<User?> GetUserByEmailOrPhoneAsync(string identifier)
        {
            return await _dbSet.FirstOrDefaultAsync(u =>
                u.Email == identifier || u.PhoneNumber == identifier);
        }

        public async Task<bool> IsUserExistsAsync(string phone, string email)
        {
            return await _dbSet.AnyAsync(u => u.PhoneNumber == phone || u.Email == email);
        }
    }
}