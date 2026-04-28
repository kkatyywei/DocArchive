using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using UDocStoreApp.Data;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public class UsedPasswordRepository : GenericRepository<UsedPassword>, IUsedPasswordRepository
    {
        public UsedPasswordRepository(ArchiveDbContext context) : base(context) { }

        public async Task<bool> IsPasswordRepeatedAsync(int userId, string passwordHash, int count)
        {
            return await _dbSet
                .Where(p => p.id_User == userId)
                .OrderByDescending(p => p.Date)
                .Take(count)
                .AnyAsync(p => p.Password == passwordHash);
        }
    }
}