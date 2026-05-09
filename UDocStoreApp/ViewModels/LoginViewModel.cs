using System;
using System.Threading.Tasks;
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

        private async Task ExecuteLogin()
        {
            var result = await _authService.LoginAsync(Login, Password);

            if (result.Success)
            {
                // 1. Проверяем принудительную смену пароля
                if (AuthService.CurrentUser.ChangePassword == 1)
                {
                    var changeWin = new Views.ChangePasswordWindow();
                    var changeVM = new ChangePasswordViewModel(AuthService.CurrentUser);
                    changeWin.DataContext = changeVM;

                    // БЛОКИРУЕМ выполнение кода, пока окно смены пароля не закроется
                    // Если пользователь нажал "Отмена" или просто закрыл крестиком - не пускаем дальше
                    if (changeWin.ShowDialog() != true)
                    {
                        return;
                    }
                }

                // 2. Только после успешной смены (или если она не требовалась)
                // Сначала создаем и показываем ГЛАВНОЕ ОКНО
                var mainWin = new Views.MainWindow();
                mainWin.Show();

                // 3. И только в самом конце закрываем окно логина
                Application.Current.MainWindow = mainWin; // Назначаем новое окно главным

                foreach (Window win in Application.Current.Windows)
                {
                    if (win is Views.LoginWindow)
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