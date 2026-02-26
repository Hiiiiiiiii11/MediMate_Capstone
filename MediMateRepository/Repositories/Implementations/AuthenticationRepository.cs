using MediMateRepository.Data;
using MediMateRepository.Model;
using Microsoft.EntityFrameworkCore;

namespace MediMateRepository.Repositories.Implementations
{
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