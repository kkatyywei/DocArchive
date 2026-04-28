using System;
using System.Linq;
using System.Threading.Tasks;
using UDocStoreApp.Infrastructure;
using UDocStoreApp.Models;
using UDocStoreApp.Repositories;

namespace UDocStoreApp.Services
{
    public class AuthService
    {
        // Сессия текущего пользователя
        public static User CurrentUser { get; private set; }

        public async Task<(bool Success, string Message)> LoginAsync(string login, string password)
        {
            try
            {
                string hash = PasswordHasher.GetMD5Hash(password);

                using (var uow = new UnitOfWork())
                {
                    // 1. Поиск пользователя через репозиторий
                    var user = await uow.Users.GetByLoginAsync(login);

                    if (user == null)
                        return (false, "Пользователь не найден.");

                    // 2. Проверка хеша пароля
                    if (user.Password.Trim().ToUpper() != hash.ToUpper())
                        return (false, "Неверный пароль.");

                    // 3. Проверка активности
                    if (user.Active == 0)
                        return (false, "Аккаунт заблокирован.");

                    // 4. Проверка срока действия пароля (бизнес-логика)
                    var policy = (await uow.PassParams.GetAllAsync()).FirstOrDefault();
                    if (policy != null && policy.MaxPeriodCheck)
                    {
                        // Получаем дату последней смены пароля из истории
                        var lastPass = (await uow.UsedPasswords.FindAsync(p => p.id_User == user.id))
                                       .OrderByDescending(p => p.Date)
                                       .FirstOrDefault();

                        if (lastPass != null && (DateTime.Now - lastPass.Date).TotalDays > policy.MaxPeriod)
                        {
                            user.ChangePassword = 1; // Устанавливаем флаг принудительной смены
                            uow.Users.Update(user);
                            await uow.CompleteAsync();
                        }
                    }

                    // Сохраняем пользователя в статическую сессию
                    CurrentUser = user;

                    return (true, "Успешно");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка авторизации: {ex.Message}");
            }
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}