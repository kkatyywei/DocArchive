using System.Threading.Tasks;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public interface IOrderExecutorRepository : IRepository<OrderExecutor>
    {
        Task RemoveLinksByOrderIdAsync(int orderId);
    }
}