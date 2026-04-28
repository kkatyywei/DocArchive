using System.Threading.Tasks;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public interface IUsedPasswordRepository : IRepository<UsedPassword>
    {
        // Проверка: использовался ли этот хеш в последних 'count' записях пользователя
        Task<bool> IsPasswordRepeatedAsync(int userId, string passwordHash, int count);
    }
}