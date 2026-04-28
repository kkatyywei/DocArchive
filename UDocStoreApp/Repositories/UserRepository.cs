using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UDocStoreApp.Data;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ArchiveDbContext context) : base(context) { }

        public async Task<IEnumerable<User>> GetAllWithRightsAsync()
        {
            return await _dbSet.Include(u => u.Right).ToListAsync();
        }
        public async Task<User> GetByLoginAsync(string login)
        {
            return await _dbSet
                .Include(u => u.Right)
                .Include(u => u.Executor)
                .FirstOrDefaultAsync(u => u.Login == login);
        }
    }
}
