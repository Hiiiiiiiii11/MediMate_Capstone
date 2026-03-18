using MediMateRepository.Data;
using MediMateRepository.Model;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MediMateRepository.Repositories.Implementations
{
    public class TransactionRepository : GenericRepository<Transactions>, ITransactionRepository
    {
        public TransactionRepository(MediMateDbContext context) : base(context)
        {
        }

        public IQueryable<Transactions> GetTransactionsWithDetailsQuery()
        {
            return _dbSet
                .Include(t => t.Payment)
                    .ThenInclude(p => p.User)
                .Include(t => t.Payment)
                    .ThenInclude(p => p.Subscription)
                .AsQueryable();
        }
    }
}
