using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UDocStoreApp.Data;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public class OrderFileRepository : GenericRepository<OrderFile>, IOrderFileRepository
    {
        public OrderFileRepository(ArchiveDbContext context) : base(context) { }

        public async Task<IEnumerable<OrderFile>> GetFilesByOrderIdAsync(int orderId)
        {
            return await _dbSet
                .Include(of => of.File) 
                .Where(of => of.idOrder == orderId)
                .ToListAsync();
        }
    }
}
