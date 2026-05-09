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
        private bool _isComplexityVisible;
        public bool IsComplexityVisible
        {
            get => _isComplexityVisible;
            set => SetProperty(ref _isComplexityVisible, value);
        }

        public PassParam Policy { get; set; } = new PassParam();
        public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }
        public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public RelayCommand ChangePasswordCommand { get; }


        public ChangePasswordViewModel(User user)
        {
            _userId = user.id;
            ChangePasswordCommand = new RelayCommand(async _ => await ExecuteChange());
            _ = LoadPolicyAsync();

        }

        private async Task LoadPolicyAsync()
        {
            using (var uow = new UnitOfWork())
            {
                var list = await uow.PassParams.GetAllAsync();
                var dbPolicy = list.FirstOrDefault();

                if (dbPolicy != null)
                {
                    // Используем Dispatcher, чтобы изменение UI произошло в основном потоке
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        this.IsComplexityVisible = dbPolicy.Strength;
                        // Если хочешь подстраховаться, вызови уведомление еще раз явно:
                        OnPropertyChanged(nameof(IsComplexityVisible));
                    });
                }
            }
        }
        private async Task ExecuteChange()
        {
            if (NewPassword != ConfirmPassword) { ErrorMessage = "Пароли не совпадают"; return; }

            using (var uow = new UnitOfWork())
            {
                // 1. Получаем политику безопасности
                var policyList = await uow.PassParams.GetAllAsync();
                var policy = policyList.FirstOrDefault() ?? new PassParam();

                // Обновляем свойство для отображения правил в UI (XAML привязан к этому полю)
                this.Policy = policy;
                OnPropertyChanged(nameof(Policy));

                // 2. НОВОЕ: Проверка минимального срока действия (MinPeriod)
                // Не даем менять пароль слишком часто, если включена проверка
                if (policy.MinPeriodCheck)
                {
                    var history = await uow.UsedPasswords.FindAsync(p => p.id_User == _userId);
                    var lastPass = history.OrderByDescending(p => p.Date).FirstOrDefault();

                    if (lastPass != null)
                    {
                        var daysPassed = (DateTime.Now - lastPass.Date).TotalDays;
                        if (daysPassed < policy.MinPeriod)
                        {
                            int daysLeft = (int)Math.Ceiling(policy.MinPeriod - daysPassed);
                            ErrorMessage = $"Пароль менялся недавно. Смена будет доступна через {daysLeft} дн.";
                            return;
                        }
                    }
                }

                // 3. Валидация сложности (обновленный метод ниже)
                if (!ValidateComplexity(policy)) return;

                // 4. Проверка истории паролей
                string newHash = PasswordHasher.GetMD5Hash(NewPassword);
                if (policy.CountLastCheck)
                {
                    bool isRepeated = await uow.UsedPasswords.IsPasswordRepeatedAsync(_userId, newHash, policy.CountLast);
                    if (isRepeated)
                    {
                        ErrorMessage = $"Нельзя использовать последние {policy.CountLast} ваших паролей";
                        return;
                    }
                }

                // 5. Сохранение изменений через транзакцию
                await uow.BeginTransactionAsync();
                try
                {
                    var user = await uow.Users.GetByIdAsync(_userId);
                    user.Password = newHash;
                    user.ChangePassword = 0;
                    uow.Users.Update(user);

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
            // Проверка минимальной длины
            if (policy.MinWidthCheck && (NewPassword?.Length < policy.MinWidth))
            {
                ErrorMessage = $"Пароль слишком короткий (минимум {policy.MinWidth} симв.)";
                return false;
            }

            // Проверка категорий символов (Strength)
            if (policy.Strength)
            {
                bool hasUpper = NewPassword.Any(char.IsUpper);
                bool hasLower = NewPassword.Any(char.IsLower);
                bool hasDigit = NewPassword.Any(char.IsDigit);
                bool hasSpecial = NewPassword.Any(c => !char.IsLetterOrDigit(c));

                if (!hasUpper || !hasLower || !hasDigit || !hasSpecial)
                {
                    ErrorMessage = "Пароль не отвечает требованиям сложности (нужны: A, a, 0-9, @#$)";
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
        private async Task LoadPolicy()
        {
            using (var uow = new UnitOfWork())
            {
                var list = await uow.PassParams.GetAllAsync();
                Policy = list.FirstOrDefault() ?? new PassParam();
                OnPropertyChanged(nameof(Policy));
            }
        }



    }
}