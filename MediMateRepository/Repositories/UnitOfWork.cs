using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        // Phương thức để lấy Repository cho một Entity cụ thể
        IGenericRepository<T> Repository<T>() where T : class;

        // Phương thức commit tất cả thay đổi xuống Database
        Task<int> CompleteAsync();
    }
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;
        private Hashtable _repositories;
        private bool _disposed = false;

        // Inject DbContext vào Constructor
        public UnitOfWork(DbContext context)
        {
            _context = context;
        }

        public IGenericRepository<T> Repository<T>() where T : class
        {
            if (_repositories == null)
                _repositories = new Hashtable();

            var type = typeof(T).Name;

            // Kiểm tra xem Repository cho Entity này đã được tạo chưa
            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(GenericRepository<>);

                // Tạo instance mới của GenericRepository<T> và truyền _context vào
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
    }
}
