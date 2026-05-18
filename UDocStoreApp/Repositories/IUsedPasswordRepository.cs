using System.Threading.Tasks;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public interface IUsedPasswordRepository : IRepository<UsedPassword>
    {
        Task<bool> IsPasswordRepeatedAsync(int userId, string passwordHash, int count);
    }
}