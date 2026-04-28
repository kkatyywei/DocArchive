using System.Collections.Generic;
using System.Threading.Tasks;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<IEnumerable<User>> GetAllWithRightsAsync();
        Task<User> GetByLoginAsync(string login);

    }
}
