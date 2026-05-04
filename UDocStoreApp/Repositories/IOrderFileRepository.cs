using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public interface IOrderFileRepository : IRepository<OrderFile>
    {
        // Метод, который гарантированно загрузит связанные объекты File
        Task<IEnumerable<OrderFile>> GetFilesByOrderIdAsync(int orderId);
    }
}
