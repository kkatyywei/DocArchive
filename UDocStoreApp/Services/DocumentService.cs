using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using UDocStoreApp.Data;
using UDocStoreApp.Models;

namespace UDocStoreApp.Services
{
    public class DocumentService
    {
        private readonly ArchiveDbContext _context;

        public DocumentService(ArchiveDbContext context)
        {
            _context = context;
        }

        // Получение документов с учетом прав доступа
        public async Task<List<Order>> GetVisibleDocumentsAsync(int? catalogId = null)
        {
            var user = AuthService.CurrentUser;
            IQueryable<Order> query = _context.Orders
                .Include(o => o.Catalog)
                .Include(o => o.Author)
                .Include(o => o.OrderExecutors)
                .ThenInclude(oe => oe.Executor);

            // Фильтр по удалению (Админ видит всё, остальные - только не удалённые)
            if (user.Right.Name != "Администратор")
                query = query.Where(o => o.isDel == 0);

            // Фильтр по ролям
            if (user.Right.Name == "Исполнитель")
            {
                // Видит только те, где он назначен
                query = query.Where(o => o.OrderExecutors.Any(oe => oe.idExecutor == user.idExecutor));
            }

            if (catalogId.HasValue)
                query = query.Where(o => o.idCatalog == catalogId.Value);

            return await query.ToListAsync();
        }

        // Блокировка документа при открытии
        public async Task<bool> TryLockDocumentAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            if (order.idUserOpen != null && order.idUserOpen != AuthService.CurrentUser.id)
                return false; // Занят другим

            order.idUserOpen = AuthService.CurrentUser.id;
            await _context.SaveChangesAsync();
            return true;
        }

        // Снятие блокировки
        public async Task ReleaseLockAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null && (order.idUserOpen == AuthService.CurrentUser.id || AuthService.CurrentUser.idRights == 1))
            {
                order.idUserOpen = null;
                await _context.SaveChangesAsync();
            }
        }

        // Работа с файлами
        public async Task AddFileAsync(int orderId, string fileName, byte[] data)
        {
            var file = new FileEntity { idOrder = orderId, Name = fileName, Data = data };
            await _context.Files.AddAsync(file);
            await _context.SaveChangesAsync();
        }
    }
}