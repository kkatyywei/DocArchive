using System.Windows;
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

        private void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedOrder != null)
            {
                var orderWin = new OrderWindow();
                var orderVm = new OrderViewModel(vm.SelectedOrder);
                orderWin.DataContext = orderVm;

                orderWin.Closing += (s2, e2) => orderVm.ReleaseLock();
                orderWin.ShowDialog();

                _ = vm.LoadOrders();
            }
            else
            {
                MessageBox.Show("Выберите документ в таблице!");
            }
        }
    }
}