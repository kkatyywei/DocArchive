using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UDocStoreApp.Data;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(ArchiveDbContext context) : base(context) { }

        public async Task<IEnumerable<Order>> GetArchiveOrdersAsync(int? catalogId, string search, bool isAdmin)
        {
            IQueryable<Order> query = _dbSet
                .Include(o => o.Author)
                .Include(o => o.Catalog);

            if (!isAdmin) query = query.Where(o => o.isDel == 0);
            if (catalogId.HasValue) query = query.Where(o => o.idCatalog == catalogId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o => o.Text.Contains(search) || o.NumberOrder.Contains(search));
            }


            return await query
                .OrderBy(o => o.isDel)
                .ThenByDescending(o => o.RegDate)
                .ToListAsync();
        }
    }
}