using System;
using System.Threading.Tasks;
using MediMateRepository.Model;

namespace MediMateRepository.Repositories
{
    public interface ITransactionRepository : IGenericRepository<Transactions>
    {
        // Add specific methods here if needed, generic repo might suffice for basic IQueryable needs,
        // but since we need to include Payments, Users, etc., it might be easier to use DbContext directly in Service or Repo method.
        // We'll define a queryable method to let Service handle filtering.
        IQueryable<Transactions> GetTransactionsWithDetailsQuery();
    }
}
