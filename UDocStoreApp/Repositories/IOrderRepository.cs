using UDocStoreApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UDocStoreApp.Repositories
{
    public interface IOrderRepository : IRepository<Order>
    {
        // Специальный запрос для главной страницы с фильтрами и сортировкой (isDel в конце)
        Task<IEnumerable<Order>> GetArchiveOrdersAsync(int? catalogId, string search, bool isAdmin);
    }
}