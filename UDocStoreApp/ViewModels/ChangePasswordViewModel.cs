using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using UDocStoreApp.Infrastructure;
using UDocStoreApp.Models;
using UDocStoreApp.Repositories;

namespace UDocStoreApp.ViewModels
{
    public class ChangePasswordViewModel : ViewModelBase
    {
        private readonly int _userId;
        private string _newPassword;
        private string _confirmPassword;
        private string _errorMessage;

        public ChangePasswordViewModel(User user)
        {
            _userId = user.id;
            ChangePasswordCommand = new RelayCommand(async _ => await ExecuteChange());
        }

        public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }
        public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public RelayCommand ChangePasswordCommand { get; }

        private async Task ExecuteChange()
        {
            if (NewPassword != ConfirmPassword) { ErrorMessage = "Пароли не совпадают"; return; }

            using (var uow = new UnitOfWork())
            {
                // 1. Получаем политику безопасности
                var policyList = await uow.PassParams.GetAllAsync();
                var policy = policyList.FirstOrDefault() ?? new PassParam();

                // 2. Валидация сложности (Client-side logic)
                if (!ValidateComplexity(policy)) return;

                // 3. Проверка истории паролей через специализированный репозиторий
                string newHash = PasswordHasher.GetMD5Hash(NewPassword);
                if (policy.CountLastCheck)
                {
                    bool isRepeated = await uow.UsedPasswords.IsPasswordRepeatedAsync(_userId, newHash, policy.CountLast);
                    if (isRepeated)
                    {
                        ErrorMessage = $"Нельзя использовать последние {policy.CountLast} паролей";
                        return;
                    }
                }

                // 4. Сохранение изменений через транзакцию (User + UsedPassword)
                await uow.BeginTransactionAsync();
                try
                {
                    var user = await uow.Users.GetByIdAsync(_userId);
                    user.Password = newHash;
                    user.ChangePassword = 0;

                    uow.Users.Update(user);

                    // Добавляем запись в историю
                    await uow.UsedPasswords.AddAsync(new UsedPassword
                    {
                        id_User = _userId,
                        Password = newHash,
                        Date = DateTime.Now
                    });

                    await uow.CompleteAsync();
                    await uow.CommitTransactionAsync();

                    MessageBox.Show("Пароль успешно изменен!");
                    CloseWindow();
                }
                catch (Exception ex)
                {
                    await uow.RollbackTransactionAsync();
                    ErrorMessage = "Ошибка сохранения: " + ex.Message;
                }
            }
        }

        private bool ValidateComplexity(PassParam policy)
        {
            if (policy.MinWidthCheck && (NewPassword?.Length < policy.MinWidth))
            {
                ErrorMessage = $"Минимальная длина: {policy.MinWidth} симв.";
                return false;
            }

            if (policy.Strength)
            {
                var hasUpper = new Regex(@"[A-Z]").IsMatch(NewPassword);
                var hasLower = new Regex(@"[a-z]").IsMatch(NewPassword);
                var hasNumber = new Regex(@"[0-9]").IsMatch(NewPassword);
                if (!hasUpper || !hasLower || !hasNumber)
                {
                    ErrorMessage = "Пароль должен содержать A-Z, a-z и 0-9";
                    return false;
                }
            }
            return true;
        }

        private void CloseWindow()
        {
            foreach (Window win in Application.Current.Windows)
            {
                if (win is Views.ChangePasswordWindow)
                {
                    win.DialogResult = true;
                    win.Close();
                }
            }
        }
    }
}