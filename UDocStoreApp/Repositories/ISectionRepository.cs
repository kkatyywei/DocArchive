using System.Collections.Generic;
using System.Threading.Tasks;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public interface ISectionRepository : IRepository<Section>
    {
        Task<IEnumerable<Section>> GetAllWithCatalogsAsync();
    }
}