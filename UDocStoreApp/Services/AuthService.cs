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
        public static User CurrentUser { get; private set; }

        public AuthService() { }

        public async Task<(bool Success, string Message)> LoginAsync(string login, string password)
        {
            try
            {
                string hash = PasswordHasher.GetMD5Hash(password);

                using (var context = new ArchiveDbContext())
                {
                    // 1. Проверяем наличие пользователя
                    var user = await context.Users
                        .Include(u => u.Right)
                        .Include(u => u.Executor)
                        .FirstOrDefaultAsync(u => u.Login == login);

                    if (user == null) return (false, "Пользователь не найден");

                    // 2. Проверяем пароль (сравнение в C# более надежно для отладки)
                    if (user.Password.Trim().ToUpper() != hash.ToUpper())
                        return (false, "Неверный пароль");

                    if (user.Active == 0) return (false, "Пользователь заблокирован");

                    // Копируем данные в статический объект, чтобы они не стерлись после закрытия context
                    CurrentUser = user;

                    return (true, "Успешно");
                }
            }
            catch (Exception ex)
            {
                // Если база не отвечает, ты увидишь это в окне ошибки
                return (false, "Ошибка базы данных: " + ex.Message);
            }
        }
        public void Logout() => CurrentUser = null;
    }
}