using MediMateRepository.Model;

namespace MediMateRepository.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByPhoneNumberAsync(string phoneNumber);
        Task<bool> IsPhoneNumberExistsAsync(string phoneNumber);
    }
}
