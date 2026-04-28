using System.Threading.Tasks;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public interface IOrderExecutorRepository : IRepository<OrderExecutor>
    {
        // Метод для быстрой очистки старых связей перед сохранением новых
        Task RemoveLinksByOrderIdAsync(int orderId);
    }
}