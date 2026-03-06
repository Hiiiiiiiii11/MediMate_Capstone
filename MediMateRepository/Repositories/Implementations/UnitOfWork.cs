using MediMateRepository.Data;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections;

namespace MediMateRepository.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MediMateDbContext _context;
        private Hashtable _repositories;
        private bool _disposed = false;

        // Inject DbContext vào Constructor
        public UnitOfWork(MediMateDbContext context)
        {
            _context = context;
        }

        public IGenericRepository<T> Repository<T>() where T : class
        {
            _repositories ??= new Hashtable();

            var type = typeof(T).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(GenericRepository<>);

                // Truyền _context (kiểu MediMateDbContext) vào GenericRepository
                var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), _context);

                _repositories.Add(type, repositoryInstance);
            }

            return (IGenericRepository<T>)_repositories[type]!;
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // Dispose Pattern để giải phóng DbContext
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            // Gọi hàm Transaction có sẵn của Entity Framework Core
            return await _context.Database.BeginTransactionAsync();
        }

    }
}
