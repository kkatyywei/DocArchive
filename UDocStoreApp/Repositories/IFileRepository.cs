using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public interface IFileRepository : IRepository<FileEntity>
    {
        Task<FileEntity> GetByHashAsync(string hash);
    }
}
