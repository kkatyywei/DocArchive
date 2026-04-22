using System.Windows;
using UDocStoreApp.Data;
using UDocStoreApp.Services;
using UDocStoreApp.ViewModels;

namespace UDocStoreApp.Views
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();


            OrdersTable.MouseDoubleClick += (s, e) =>
            {
                if (DataContext is MainViewModel vm && vm.SelectedOrder != null)
                {
                    var orderWin = new OrderWindow();
                    var orderVm = new OrderViewModel(vm.SelectedOrder);
                    orderWin.DataContext = orderVm;
                    orderWin.Owner = this;

                    orderWin.Closing += (s2, e2) => orderVm.ReleaseLock(); // Снимаем блокировку при закрытии
                    orderWin.ShowDialog();

                    _ = vm.LoadOrders(); // Обновляем список после закрытия карточки
                }
            };

            // Создаем ViewModel
            var vm = new MainViewModel();
            this.DataContext = vm;

            // Теперь ArchiveTree существует!
            ArchiveTree.SelectedItemChanged += (s, e) =>
            {
                vm.SelectedTreeItem = e.NewValue;
            };
        }

        // В MainWindow.xaml.cs кнопка клик
        private void AdminBtn_Click(object sender, RoutedEventArgs e)
        {
            var adminWin = new AdminWindow();
            adminWin.DataContext = new AdminViewModel();
            adminWin.ShowDialog();
        }

        //private void EditOrder_Click(object sender, RoutedEventArgs e)
        //{
        //    if (DataContext is MainViewModel vm && vm.SelectedOrder != null)
        //    {
        //        var orderWin = new OrderWindow();
        //        var orderVm = new OrderViewModel(vm.SelectedOrder);
        //        orderWin.DataContext = orderVm;

        //        orderWin.Closing += (s2, e2) => orderVm.ReleaseLock();
        //        orderWin.ShowDialog();

        //        _ = vm.LoadOrders();
        //    }
        //    else
        //    {
        //        MessageBox.Show("Выберите документ в таблице!");
        //    }
        //}

        private void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedOrder != null)
            {
                // 1. Создаем окно
                var orderWin = new OrderWindow();

                // 2. Создаем ViewModel для КОНКРЕТНОГО документа
                var orderVm = new OrderViewModel(vm.SelectedOrder);

                // 3. Соединяем их
                orderWin.DataContext = orderVm;
                orderWin.Owner = this;

                // 4. Снятие блокировки при закрытии
                orderWin.Closing += (s, ev) => orderVm.ReleaseLock();

                // 5. Показываем
                orderWin.ShowDialog();

                // 6. Обновляем список, чтобы увидеть изменения
                _ = vm.LoadOrders();
            }
            else
            {
                MessageBox.Show("Сначала выберите документ в списке (нажмите на строку)!");
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из системы?", "Выход", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                // 1. Создаем окно входа
                var loginWindow = new LoginWindow();

                // 2. Инициализируем его ViewModel
                var context = new ArchiveDbContext();
                var authService = new AuthService(context);
                loginWindow.DataContext = new LoginViewModel(authService);

                // 3. Показываем вход и закрываем текущее окно
                loginWindow.Show();
                this.Close();
            }
        }
    }
}