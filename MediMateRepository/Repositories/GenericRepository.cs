using MediMateRepository.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);
        Task<T?> GetByIdAsync(object id, params Expression<Func<T, object>>[] includes);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, string includeProperties = "");

        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);

    }
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly MediMateDbContext _context;
        protected readonly DbSet<T> _dbSet;
        public GenericRepository(MediMateDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }
        public async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.ToListAsync();
        }
        public async Task<T?> GetByIdAsync(object id, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            // 1. Apply các bảng liên kết (Include)
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            // 2. Tìm tên khóa chính (Primary Key) của bảng hiện tại một cách động
            // Ví dụ: Với User -> keyName = "UserId", Với Family -> keyName = "FamilyId"
            var entityType = _context.Model.FindEntityType(typeof(T));
            var keyName = entityType?.FindPrimaryKey()?.Properties
                .Select(x => x.Name)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(keyName))
            {
                throw new Exception($"Entity {typeof(T).Name} does not have a primary key defined.");
            }

            // 3. Query với tên khóa chính vừa tìm được
            // EF.Property<object>(e, keyName) giúp EF hiểu cột nào cần so sánh
            return await query.FirstOrDefaultAsync(e => EF.Property<object>(e, keyName) == id);
        }
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression, string includeProperties = "")
        {
            IQueryable<T> query = _dbSet;

            // 1. Áp dụng điều kiện lọc (Where)
            if (expression != null)
            {
                query = query.Where(expression);
            }

            // 2. Áp dụng Include (Join bảng)
            // Chuỗi nhập vào dạng: "Conditions,Member"
            foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            return await query.ToListAsync();
        }
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }
        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }
        public void UpdateRange(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
        }
        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }
        public void RemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }
    }
}
