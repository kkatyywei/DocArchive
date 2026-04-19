using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UDocStoreApp.Data;
using UDocStoreApp.Infrastructure;
using User = UDocStoreApp.Models.User;

namespace UDocStoreApp.Services
{
    public class AuthService
    {
        private readonly ArchiveDbContext _context;
        public static User CurrentUser { get; private set; }

        public AuthService(ArchiveDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> LoginAsync(string login, string password)
        {
            string hash = PasswordHasher.GetMD5Hash(password);

            var user = await _context.Users
                .Include(u => u.Right)
                .Include(u => u.Executor)
                .FirstOrDefaultAsync(u => u.Login == login && u.Password == hash);

            if (user == null) return (false, "Неверный логин или пароль");
            if (user.Active == 0) return (false, "Пользователь заблокирован");

            // Проверка политики паролей
            var policy = await _context.PassParams.FirstOrDefaultAsync();
            if (policy != null && policy.MaxPeriodCheck)
            {
                var lastPassDate = await _context.UsedPasswords
                    .Where(p => p.id_User == user.id)
                    .OrderByDescending(p => p.Date)
                    .Select(p => p.Date)
                    .FirstOrDefaultAsync();

                if (lastPassDate != default && (DateTime.Now - lastPassDate).TotalDays > policy.MaxPeriod)
                {
                    user.ChangePassword = 1;
                    await _context.SaveChangesAsync();
                }
            }

            CurrentUser = user;
            return (true, "Успешный вход");
        }

        public void Logout() => CurrentUser = null;
    }
}