using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UDocStoreApp.Data;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public class SectionRepository : GenericRepository<Section>, ISectionRepository
    {
        public SectionRepository(ArchiveDbContext context) : base(context) { }

        public async Task<IEnumerable<Section>> GetAllWithCatalogsAsync()
        {
            return await _dbSet.Include(s => s.Catalogs).ToListAsync();
        }
    }
}