using System;
using System.Threading.Tasks;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IOrderRepository Orders { get; }
        IUserRepository Users { get; }
        ISectionRepository Sections { get; }
        IRepository<Catalog> Catalogs { get; }
        IRepository<FileEntity> Files { get; }
        IRepository<Executor> Executors { get; }
        IRepository<PassParam> PassParams { get; }
        IRepository<Right> Rights { get; }
        IUsedPasswordRepository UsedPasswords { get; }
        IOrderExecutorRepository OrderExecutors { get; }
        IOrderFileRepository OrderFiles { get; }

        Task<int> CompleteAsync(); // Сохранение (Save)
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}