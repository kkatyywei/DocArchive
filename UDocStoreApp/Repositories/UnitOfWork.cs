using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using UDocStoreApp.Data;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ArchiveDbContext _context;
        private IDbContextTransaction _transaction;

        public ISectionRepository Sections { get; private set; }
        public IOrderRepository Orders { get; private set; }
        public IUserRepository Users { get; }
        public IRepository<Catalog> Catalogs { get; }
        public IRepository<FileEntity> Files { get; }
        public IRepository<Executor> Executors { get; }
        public IOrderFileRepository OrderFiles { get; }
        public IRepository<PassParam> PassParams { get; }
        public IRepository<Right> Rights { get; }
        public IUsedPasswordRepository UsedPasswords { get; private set; }
        public IOrderExecutorRepository OrderExecutors { get; private set; }



        public UnitOfWork()
        {
            _context = new ArchiveDbContext();
            Orders = new OrderRepository(_context);
            Users = new UserRepository(_context);
            Sections = new SectionRepository(_context);
            Catalogs = new GenericRepository<Catalog>(_context);
            Files = new GenericRepository<FileEntity>(_context);
            Executors = new GenericRepository<Executor>(_context);
            OrderFiles = new OrderFileRepository(_context);
            PassParams = new GenericRepository<PassParam>(_context);
            Rights = new GenericRepository<Right>(_context);
            UsedPasswords = new UsedPasswordRepository(_context);
            OrderExecutors = new OrderExecutorRepository(_context);
        }

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

        public async Task BeginTransactionAsync() => _transaction = await _context.Database.BeginTransactionAsync();
        public async Task CommitTransactionAsync() { await _transaction?.CommitAsync(); _transaction?.Dispose(); }
        public async Task RollbackTransactionAsync() { await _transaction?.RollbackAsync(); _transaction?.Dispose(); }

        public void Dispose() => _context.Dispose();
    }
}