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

        public async Task<IEnumerable<Order>> GetArchiveOrdersAsync(int? catalogId, string search, bool isAdmin, int? executorId = null)
        {
            IQueryable<Order> query = _dbSet
                .Include(o => o.Author)
                .Include(o => o.Catalog)
                .Include(o => o.OrderExecutors)
                        .ThenInclude(oe => oe.Executor);

            if (executorId.HasValue)
            {
                query = query.Where(o => o.OrderExecutors.Any(oe => oe.idExecutor == executorId.Value));
            }

            if (!isAdmin) query = query.Where(o => o.isDel == 0);
            if (catalogId.HasValue) query = query.Where(o => o.idCatalog == catalogId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.ToLower().Trim();
                query = query.Where(o =>
                    (o.Text != null && o.Text.ToLower().Contains(s)) ||
                    (o.NumberOrder != null && o.NumberOrder.ToLower().Contains(s)) ||
                    (o.NumberReg.ToString().Contains(s)) ||
                    // Поиск по ФИО исполнителей (для всех ролей)
                    o.OrderExecutors.Any(oe => oe.Executor.FIO != null && oe.Executor.FIO.ToLower().Contains(s))
                );
            }

            return await query
                .OrderBy(o => o.isDel)
                .ThenByDescending(o => o.RegDate)
                .ToListAsync();
        }
    }
}