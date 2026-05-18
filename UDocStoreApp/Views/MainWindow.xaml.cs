using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
            OrdersTable.Sorting += OrdersTable_Sorting;

            OrdersTable.MouseDoubleClick += (s, e) =>
            {
                if (DataContext is MainViewModel vm && vm.SelectedOrder != null)
                {
                    var user = AuthService.CurrentUser;

                    // rights to edit
                    bool canEdit = user.Right.Name == "Администратор" ||
                                   (user.Right.Name == "Регистратор" && vm.SelectedOrder.idUser == user.id);

                    var orderWin = new OrderWindow();
                    var orderVm = new OrderViewModel(vm.SelectedOrder);

                    // no rights to edit
                    if (!canEdit)
                    {
                        orderVm.IsReadOnly = true;
                    }

                    orderWin.DataContext = orderVm;
                    orderWin.Owner = this;

                    orderWin.ShowDialog();
                    _ = vm.LoadOrders();
                }
            };

            var vm = new MainViewModel();
            this.DataContext = vm;

            ArchiveTree.SelectedItemChanged += (s, e) =>
            {
                vm.SelectedTreeItem = e.NewValue;
            };
        }

        private void OrdersTable_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            var column = e.Column;
            var direction = (column.SortDirection != ListSortDirection.Ascending)? ListSortDirection.Ascending : ListSortDirection.Descending;

            column.SortDirection = direction;

            ICollectionView view = CollectionViewSource.GetDefaultView(OrdersTable.ItemsSource);
            view.SortDescriptions.Clear();

            // deleted docs at the bottom
            view.SortDescriptions.Add(new SortDescription("isDel", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription(column.SortMemberPath, direction));

            view.Refresh();
        }

        private void AdminBtn_Click(object sender, RoutedEventArgs e)
        {
            var adminWin = new AdminWindow();
            adminWin.DataContext = new AdminViewModel();
            adminWin.ShowDialog();
        }
        private void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedOrder != null)
            {
                var user = AuthService.CurrentUser;

                bool canEdit = user.Right.Name == "Администратор" || vm.SelectedOrder.idUser == user.id;

                var orderWin = new OrderWindow();
                var orderVm = new OrderViewModel(vm.SelectedOrder);

                if (!canEdit)
                {
                    orderVm.IsReadOnly = true;
                    MessageBox.Show("Вы не являетесь автором этого документа. Просмотр ограничен только чтением.");
                }

                orderWin.DataContext = orderVm;
                orderWin.Owner = this;
                orderWin.ShowDialog();
                _ = vm.LoadOrders();
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из системы?", "Выход", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var loginWindow = new LoginWindow();

                var context = new ArchiveDbContext();
                var authService = new AuthService();
                loginWindow.DataContext = new LoginViewModel(authService);

                loginWindow.Show();
                this.Close();
            }
        }
    }
}