using System.Windows;
using UDocStoreApp.Data;
using UDocStoreApp.Services;
using UDocStoreApp.ViewModels;
using UDocStoreApp.Views;

namespace UDocStoreApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Инициализация
            var context = new ArchiveDbContext();
            var authService = new AuthService(context);
            var loginVM = new LoginViewModel(authService);

            // 2. Создаем окно
            var loginWindow = new LoginWindow();
            loginWindow.DataContext = loginVM;

            // 3. ВАЖНО: Назначаем его главным, чтобы приложение не закрылось
            this.MainWindow = loginWindow;

            // 4. Показываем
            loginWindow.Show();
        }
    }
}