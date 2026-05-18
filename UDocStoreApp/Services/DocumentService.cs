using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using UDocStoreApp.Data;
using UDocStoreApp.Models;
using UDocStoreApp.Repositories;

namespace UDocStoreApp.Services
{
    public class DocumentService
    {
        private readonly ArchiveDbContext _context;

        public DocumentService(ArchiveDbContext context)
        {
            _context = context;
        }

        // get docs in view role
        public async Task<List<Order>> GetVisibleDocumentsAsync(int? catalogId = null)
        {
            var user = AuthService.CurrentUser;
            IQueryable<Order> query = _context.Orders
                .Include(o => o.Catalog)
                .Include(o => o.Author)
                .Include(o => o.OrderExecutors)
                .ThenInclude(oe => oe.Executor);

            if (user.Right.Name != "Администратор")
                query = query.Where(o => o.isDel == 0);

            if (user.Right.Name == "Исполнитель")
            {
                query = query.Where(o => o.OrderExecutors.Any(oe => oe.idExecutor == user.idExecutor));
            }

            if (catalogId.HasValue)
                query = query.Where(o => o.idCatalog == catalogId.Value);

            return await query.ToListAsync();
        }

        // Блокировка документа при открытии
        //public async Task<bool> TryLockDocumentAsync(int orderId)
        //{
        //    var order = await _context.Orders.FindAsync(orderId);
        //    if (order == null) return false;

        //    if (order.idUserOpen != null && order.idUserOpen != AuthService.CurrentUser.id)
        //        return false; // Занят другим

        //    order.idUserOpen = AuthService.CurrentUser.id;
        //    await _context.SaveChangesAsync();
        //    return true;
        //}

        //// Снятие блокировки
        //public async Task ReleaseLockAsync(int orderId)
        //{
        //    var order = await _context.Orders.FindAsync(orderId);
        //    if (order != null && (order.idUserOpen == AuthService.CurrentUser.id || AuthService.CurrentUser.idRights == 1))
        //    {
        //        order.idUserOpen = null;
        //        await _context.SaveChangesAsync();
        //    }
        //}

        public async Task AddFileAsync(int orderId, string fileName, byte[] data, string hash)
        {
            using (var uow = new UnitOfWork())
            {
                var file = new FileEntity
                {
                    Name = fileName,
                    Data = data,
                    FileHash = hash
                };
                await uow.Files.AddAsync(file);
                await uow.CompleteAsync(); 

                var link = new OrderFile
                {
                    idOrder = orderId,
                    idFile = file.id
                };
                await uow.OrderFiles.AddAsync(link);
                await uow.CompleteAsync();
            }
        }
    }
}