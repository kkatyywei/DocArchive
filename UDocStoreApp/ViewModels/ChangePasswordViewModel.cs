using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using UDocStoreApp.Data;
using UDocStoreApp.Infrastructure;
using UDocStoreApp.Models;

namespace UDocStoreApp.ViewModels
{
    public class ChangePasswordViewModel : ViewModelBase
    {
        private readonly ArchiveDbContext _db;
        private readonly User _user;
        private string _newPassword;
        private string _confirmPassword;
        private string _errorMessage;

        public ChangePasswordViewModel(User user)
        {
            _db = new ArchiveDbContext();
            _user = _db.Users.Find(user.id);
            ChangePasswordCommand = new RelayCommand(_ => ExecuteChange());
        }

        public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }
        public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public RelayCommand ChangePasswordCommand { get; }

        private void ExecuteChange()
        {
            if (NewPassword != ConfirmPassword) { ErrorMessage = "Пароли не совпадают"; return; }

            var policy = _db.PassParams.FirstOrDefault() ?? new PassParam();

            // 1. Проверка длины
            if (policy.MinWidthCheck && (NewPassword?.Length < policy.MinWidth))
            { ErrorMessage = $"Минимальная длина: {policy.MinWidth} симв."; return; }

            // 2. Проверка сложности (цифры, буквы, спецсимволы)
            if (policy.Strength)
            {
                var hasUpper = new Regex(@"[A-Z]").IsMatch(NewPassword);
                var hasLower = new Regex(@"[a-z]").IsMatch(NewPassword);
                var hasNumber = new Regex(@"[0-9]").IsMatch(NewPassword);
                if (!hasUpper || !hasLower || !hasNumber)
                { ErrorMessage = "Пароль слишком простой (нужны A-z, 0-9)"; return; }
            }

            // 3. Проверка на повтор последних паролей
            string newHash = PasswordHasher.GetMD5Hash(NewPassword);
            if (policy.CountLastCheck)
            {
                var usedBefore = _db.UsedPasswords
                    .Where(p => p.id_User == _user.id)
                    .OrderByDescending(p => p.Date)
                    .Take(policy.CountLast)
                    .Any(p => p.Password == newHash);

                if (usedBefore) { ErrorMessage = $"Нельзя использовать {policy.CountLast} последних паролей"; return; }
            }

            // СОХРАНЕНИЕ
            _user.Password = newHash;
            _user.ChangePassword = 0; // Сбрасываем флаг

            // Добавляем в историю
            _db.UsedPasswords.Add(new UsedPassword { id_User = _user.id, Password = newHash, Date = DateTime.Now });
            _db.SaveChanges();

            MessageBox.Show("Пароль успешно изменен!");

            // Закрываем окно (через DialogResult в Code-behind)
            foreach (Window win in Application.Current.Windows)
                if (win is Views.ChangePasswordWindow) win.DialogResult = true;
        }
    }
}