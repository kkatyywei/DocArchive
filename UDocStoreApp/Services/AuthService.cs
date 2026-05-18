using System;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;
using UDocStoreApp.Infrastructure;
using UDocStoreApp.Models;
using UDocStoreApp.Repositories;

namespace UDocStoreApp.Services
{
    public class AuthService
    {
        // session of current user
        public static User CurrentUser { get; private set; }

        public async Task<(bool Success, string Message)> LoginAsync(string login, string password)
        {
            try
            {
                string hash = PasswordHasher.GetMD5Hash(password);

                using (var uow = new UnitOfWork())
                {
                    var user = await uow.Users.GetByLoginAsync(login);

                    if (user == null)
                    {
                        MessageBox.Show("Пользователь не найден");
                        return (false, "Пользователь не найден");
                    }
                    if (user.Password.Trim().ToUpper() != hash.ToUpper())
                    {
                        MessageBox.Show("Неверный пароль");
                        return (false, "Неверный пароль");
                    }
                    if (user.Active == 0)
                    {
                        MessageBox.Show("Аккаунт заблокирован");
                        return (false, "Аккаунт заблокирован");
                    }
                    var policy = (await uow.PassParams.GetAllAsync()).FirstOrDefault();
                    if (policy != null && policy.MaxPeriodCheck)
                    {
                        // get date of last changies password from history
                        var lastPass = (await uow.UsedPasswords.FindAsync(p => p.id_User == user.id))
                                       .OrderByDescending(p => p.Date)
                                       .FirstOrDefault();

                        if (lastPass != null && (DateTime.Now - lastPass.Date).TotalDays > policy.MaxPeriod)
                        {
                            user.ChangePassword = 1; 
                            uow.Users.Update(user);
                            await uow.CompleteAsync();
                        }
                    }

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