using System;
using System.Windows;
using System.Windows.Input;
using UDocStoreApp.Infrastructure;
using UDocStoreApp.Services;
using UDocStoreApp.Views;

namespace UDocStoreApp.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private string _login;
        private string _password;
        private string _errorMessage;

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
            LoginCommand = new RelayCommand(async _ => await ExecuteLogin());
        }

        public string Login
        {
            get => _login;
            set => SetProperty(ref _login, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand LoginCommand { get; }

        private async System.Threading.Tasks.Task ExecuteLogin()
        {
            if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Введите логин и пароль";
                return;
            }

            var result = await _authService.LoginAsync(Login, Password);
            if (result.Success)
            {
                // ПРОВЕРКА ПРИНУДИТЕЛЬНОЙ СМЕНЫ ПАРОЛЯ
                if (AuthService.CurrentUser.ChangePassword == 1)
                {
                    var changeWin = new Views.ChangePasswordWindow();
                    var changeVM = new ChangePasswordViewModel(AuthService.CurrentUser);
                    changeWin.DataContext = changeVM;

                    // Пока не сменит пароль, в систему не пустим
                    if (changeWin.ShowDialog() != true)
                    {
                        return; // Пользователь закрыл окно, вход отменяется
                    }
                }

                // Если всё ок - открываем главное окно
                var mainWin = new Views.MainWindow();
                mainWin.Show();

                // 3. Переназначаем главное окно приложения на MainWindow
                Application.Current.MainWindow = mainWin;

                // 4. Закрываем окно логина
                // Ищем окно логина среди открытых и закрываем его
                foreach (Window win in Application.Current.Windows)
                {
                    if (win is LoginWindow)
                    {
                        win.Close();
                        break;
                    }
                }
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
    }
}