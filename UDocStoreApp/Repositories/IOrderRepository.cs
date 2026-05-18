using UDocStoreApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace UDocStoreApp.Repositories
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<IEnumerable<Order>> GetArchiveOrdersAsync(int? catalogId, string search, bool isAdmin, int? executorId = null, DateTime? startDate = null, DateTime? endDate = null);
    }
}