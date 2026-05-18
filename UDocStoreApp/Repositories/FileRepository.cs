using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using UDocStoreApp.Data;
using UDocStoreApp.Models;

namespace UDocStoreApp.Repositories
{
    public class FileRepository : GenericRepository<FileEntity>, IFileRepository
    {
        public FileRepository(ArchiveDbContext context) : base(context) { }

        public async Task<FileEntity> GetByHashAsync(string hash)
        {
            return await _dbSet.FirstOrDefaultAsync(f => f.FileHash == hash);
        }
    }
}