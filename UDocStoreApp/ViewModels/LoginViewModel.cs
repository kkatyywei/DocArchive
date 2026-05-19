using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UDocStoreApp.Infrastructure;
using UDocStoreApp.Services;
using UDocStoreApp.Views;

namespace UDocStoreApp.ViewModels
{
    public class LoginViewModel : ViewModelBase, IDataErrorInfo
    {
        private readonly AuthService _authService;
        private string _login;
        private string _password;
        private string _errorMessage;

        private bool _isValidationEnabled = false;
        public ICommand LoginCommand { get; }

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
      
        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                if (!_isValidationEnabled) return null;

                if (columnName == nameof(Login))
                {
                    if (string.IsNullOrWhiteSpace(Login))
                    {
                        return "Поле обязательно для заполнения";
                    }
                }
                return null;
            }
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


        private async Task ExecuteLogin()
        {

            _isValidationEnabled = true;

            OnPropertyChanged(nameof(Login));

            if (string.IsNullOrWhiteSpace(Login))
            {
                return; 
            }


            var result = await _authService.LoginAsync(Login, Password);

            if (result.Success)
            {
                if (AuthService.CurrentUser.ChangePassword == 1)
                {
                    var changeWin = new Views.ChangePasswordWindow();
                    var changeVM = new ChangePasswordViewModel(AuthService.CurrentUser);
                    changeWin.DataContext = changeVM;

                    if (changeWin.ShowDialog() != true)
                    {
                        return;
                    }
                }

                var mainWin = new Views.MainWindow();
                mainWin.Show();

                Application.Current.MainWindow = mainWin; 

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