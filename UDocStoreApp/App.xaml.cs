using System;
using System.IO;
using System.Windows;
using UDocStoreApp.Data;
using UDocStoreApp.Services;
using UDocStoreApp.ViewModels;
using UDocStoreApp.Views;

namespace UDocStoreApp
{
    public partial class App : Application
    {
        public App()
        {
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // 2. ФИКС ДИРЕКТОРИИ (Критично для инсталлятора)
            // При запуске из ярлыка рабочая директория может сбиться. 
            // Это заставляет программу всегда смотреть в папку, где лежит EXE.
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            // 3. ГЛОБАЛЬНЫЙ ПЕРЕХВАТЧИК ОШИБОК
            // Если в любом месте программы случится сбой, приложение не "схлопнется" молча,
            // а выведет понятное окно с ошибкой.
            this.DispatcherUnhandledException += (s, args) =>
            {
                string errorMsg = $"В работе приложения возникла проблема: {args.Exception.Message}";
                if (args.Exception.InnerException != null)
                    errorMsg += $"\n\nПодробности: {args.Exception.InnerException.Message}";

                MessageBox.Show(errorMsg, "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                // Handled = true позволяет приложению попытаться продолжить работу после ошибки
                args.Handled = true;
            };

            base.OnStartup(e);

            // 4. ПРЕДВАРИТЕЛЬНАЯ ПРОВЕРКА СОЕДИНЕНИЯ С БАЗОЙ
            // Мы пытаемся "постучаться" в БД до того, как покажем окно входа.
            try
            {
                using (var db = new ArchiveDbContext())
                {
                    if (!db.Database.CanConnect())
                    {
                        throw new Exception("Не удалось установить соединение с SQL сервером.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Внимание! База данных недоступна:\n{ex.Message}\n\n" +
                                "Проверьте настройки строки подключения в файле ArchiveDbContext или состояние сервера.",
                                "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // 5. ИНИЦИАЛИЗАЦИЯ MVVM СТЕКА
            // Создаем сервис авторизации и передаем его во ViewModel (Dependency Injection)
            var authService = new AuthService();
            var loginVM = new LoginViewModel(authService);

            // 6. ЗАПУСК ОКНА ВХОДА
            var loginWindow = new LoginWindow();
            loginWindow.DataContext = loginVM;

            // Назначаем окно логина главным (MainWindow). 
            // Это гарантирует, что при закрытии этого окна процесс завершится корректно.
            this.MainWindow = loginWindow;

            loginWindow.Show();
        }
    }
}