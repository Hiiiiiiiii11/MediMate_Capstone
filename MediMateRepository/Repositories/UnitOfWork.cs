namespace MediMateRepository.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        // Phương thức để lấy Repository cho một Entity cụ thể
        IGenericRepository<T> Repository<T>() where T : class;

        // Phương thức commit tất cả thay đổi xuống Database
        Task<int> CompleteAsync();
    }
}
