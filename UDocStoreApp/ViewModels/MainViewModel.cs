using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using UDocStoreApp.Data;
using UDocStoreApp.Infrastructure;
using UDocStoreApp.Models;
using UDocStoreApp.Services;
using UDocStoreApp.Views;
using User = UDocStoreApp.Models.User;
using System.Windows;

namespace UDocStoreApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ArchiveDbContext _db;
        private ObservableCollection<Section> _sections;
        private ObservableCollection<Order> _orders;
        private string _searchText;
        private object _selectedTreeItem;
        private Order _selectedOrder;

        public MainViewModel()
        {
            _db = new ArchiveDbContext();
            LoadDataCommand = new RelayCommand(async _ => await LoadData());
            SearchCommand = new RelayCommand(_ => ApplyFilter());

            // Инициализация коллекций
            Sections = new ObservableCollection<Section>();
            Orders = new ObservableCollection<Order>();

            // Загружаем данные при старте
            Task.Run(LoadData);
        }

        // Свойства для привязки к UI
        public ObservableCollection<Section> Sections
        {
            get => _sections;
            set => SetProperty(ref _sections, value);
        }

        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); ApplyFilter(); }
        }

        public object SelectedTreeItem
        {
            get => _selectedTreeItem;
            set { SetProperty(ref _selectedTreeItem, value); _ = LoadOrders(); }
        }

        public User CurrentUser => AuthService.CurrentUser;
        public Order SelectedOrder
        {
            get => _selectedOrder;
            set => SetProperty(ref _selectedOrder, value);
        }

        // Команды
        public ICommand LoadDataCommand { get; }
        public ICommand SearchCommand { get; }

        // Загрузка структуры дерева (Разделы -> Каталоги)
        private async Task LoadData()
        {
            var data = await _db.Sections
                .Include(s => s.Catalogs)
                .ToListAsync();

            App.Current.Dispatcher.Invoke(() =>
            {
                Sections.Clear();
                foreach (var item in data) Sections.Add(item);
            });
        }

        // Загрузка документов с учетом прав и выбранного каталога
        public async Task LoadOrders()
        {
            IQueryable<Order> query = _db.Orders
                .Include(o => o.Author)
                .Include(o => o.OrderExecutors)
                .ThenInclude(oe => oe.Executor);

            // 1. Фильтр по правам
            if (CurrentUser.Right.Name == "Исполнитель")
            {
                query = query.Where(o => o.OrderExecutors.Any(oe => oe.idExecutor == CurrentUser.idExecutor));
            }

            // 2. Скрываем удаленные для всех, кроме админа
            if (CurrentUser.Right.Name != "Администратор")
            {
                query = query.Where(o => o.isDel == 0);
            }

            // 3. Фильтр по выбранному каталогу в TreeView
            if (SelectedTreeItem is Catalog selectedCatalog)
            {
                query = query.Where(o => o.idCatalog == selectedCatalog.id);
            }

            var result = await query.OrderByDescending(o => o.RegDate).ToListAsync();

            App.Current.Dispatcher.Invoke(() =>
            {
                Orders.Clear();
                foreach (var order in result) Orders.Add(order);
            });
        }


        //public RelayCommand AddOrderCommand => new RelayCommand(_ =>
        //{
        //    if (SelectedTreeItem is Catalog cat)
        //    {
        //        var newOrder = new Order { idCatalog = cat.id, DateOrder = DateTime.Now };
        //        var orderWin = new OrderWindow();
        //        var orderVm = new OrderViewModel(newOrder);
        //        orderWin.DataContext = orderVm;
        //        orderWin.ShowDialog();
        //        _ = LoadOrders();
        //    }
        //    else
        //    {
        //        MessageBox.Show("Выберите журнал в дереве!");
        //    }
        //});

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                _ = LoadOrders();
                return;
            }

            // Ищем везде: в тексте, в номере, в названии каталога и в ФИО исполнителей
            var filtered = Orders.Where(o =>
                (o.Text != null && o.Text.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (o.NumberOrder != null && o.NumberOrder.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (o.Catalog != null && o.Catalog.CatalogName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (o.OrderExecutors.Any(oe => oe.Executor.FIO.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
            ).ToList();

            Orders.Clear();
            foreach (var item in filtered) Orders.Add(item);
        }


        public RelayCommand AddOrderCommand => new RelayCommand(_ =>
        {
            if (SelectedTreeItem is Catalog selectedCat)
            {
                var newOrder = new Order
                {
                    idCatalog = selectedCat.id,
                    DateOrder = DateTime.Now,
                    RegDate = DateTime.Now,
                    idUser = AuthService.CurrentUser.id,
                    NumberReg = 0
                };

                var orderWin = new OrderWindow();
                var orderVm = new OrderViewModel(newOrder);
                orderWin.DataContext = orderVm;
                orderWin.ShowDialog();

                _ = LoadOrders(); // Обновить список после добавления
            }
            else
            {
                MessageBox.Show("Сначала выберите журнал в дереве слева!");
            }
        });
    }
}