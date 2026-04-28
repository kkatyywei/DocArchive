using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using UDocStoreApp.Data;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public class OrderExecutorRepository : GenericRepository<OrderExecutor>, IOrderExecutorRepository
    {
        public OrderExecutorRepository(ArchiveDbContext context) : base(context) { }

        public async Task RemoveLinksByOrderIdAsync(int orderId)
        {
            var links = await _dbSet.Where(oe => oe.idOrder == orderId).ToListAsync();
            _dbSet.RemoveRange(links);
        }
    }
}