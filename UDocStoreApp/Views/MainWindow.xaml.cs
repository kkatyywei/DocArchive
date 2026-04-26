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
                var user = AuthService.CurrentUser;

                // ПРОВЕРКА ПРАВ: Только Админ или Регистратор могут открывать карточку
                if (user.Right.Name == "Администратор" || user.Right.Name == "Регистратор")
                {
                    if (DataContext is MainViewModel vm && vm.SelectedOrder != null)
                    {
                        // ПРОВЕРКА АВТОРСТВА (для Регистратора)
                        bool canEdit = user.Right.Name == "Администратор" || vm.SelectedOrder.idUser == user.id;

                        var orderWin = new OrderWindow();
                        var orderVm = new OrderViewModel(vm.SelectedOrder);

                        if (!canEdit) orderVm.IsReadOnly = true; // Если не свой, то только просмотр

                        orderWin.DataContext = orderVm;
                        orderWin.Owner = this;
                        orderWin.Closing += (s2, ev) => orderVm.ReleaseLock();
                        orderWin.ShowDialog();
                        _ = vm.LoadOrders();
                    }
                }
                else
                {
                    // Исполнители и Наблюдатели получат это сообщение
                    MessageBox.Show("У вас недостаточно прав для открытия карточки документа.");
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

        private void OrdersTable_Sorting(object sender, DataGridSortingEventArgs e)
        {
            // 1. Отменяем стандартную сортировку WPF
            e.Handled = true;

            var column = e.Column;
            var direction = (column.SortDirection != ListSortDirection.Ascending)
                            ? ListSortDirection.Ascending
                            : ListSortDirection.Descending;

            column.SortDirection = direction;

            // 2. Получаем View коллекции
            ICollectionView view = CollectionViewSource.GetDefaultView(OrdersTable.ItemsSource);

            // 3. Очищаем старые правила и задаем новые
            view.SortDescriptions.Clear();

            // ПРАВИЛО №1: Всегда сначала isDel (0 будут сверху, 1 - снизу)
            view.SortDescriptions.Add(new SortDescription("isDel", ListSortDirection.Ascending));

            // ПРАВИЛО №2: Сортировка по выбранной колонке (внутри групп isDel)
            view.SortDescriptions.Add(new SortDescription(column.SortMemberPath, direction));

            view.Refresh();
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
                var user = AuthService.CurrentUser;

                // ПРАВО РЕДАКТИРОВАНИЯ: Админ или автор документа
                bool canEdit = user.Right.Name == "Администратор" || vm.SelectedOrder.idUser == user.id;

                var orderWin = new OrderWindow();
                var orderVm = new OrderViewModel(vm.SelectedOrder);

                // Если нельзя редактировать, принудительно ставим ReadOnly
                if (!canEdit)
                {
                    orderVm.IsReadOnly = true;
                    MessageBox.Show("Вы не являетесь автором этого документа. Просмотр ограничен только чтением.");
                }

                orderWin.DataContext = orderVm;
                orderWin.Owner = this;
                orderWin.Closing += (s, ev) => orderVm.ReleaseLock();
                orderWin.ShowDialog();
                _ = vm.LoadOrders();
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
                var authService = new AuthService();
                loginWindow.DataContext = new LoginViewModel(authService);

                // 3. Показываем вход и закрываем текущее окно
                loginWindow.Show();
                this.Close();
            }
        }
    }
}