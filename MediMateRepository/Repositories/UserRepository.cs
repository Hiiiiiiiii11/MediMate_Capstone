using MediMateRepository.Data;
using MediMateRepository.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByPhoneNumberAsync(string phoneNumber);
        Task<bool> IsPhoneNumberExistsAsync(string phoneNumber);
    }

    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(MediMateDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByPhoneNumberAsync(string phoneNumber)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        }

        public async Task<bool> IsPhoneNumberExistsAsync(string phoneNumber)
        {
            return await _dbSet.AnyAsync(u => u.PhoneNumber == phoneNumber);
        }
    }
}
