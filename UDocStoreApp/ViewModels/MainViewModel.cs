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
using UDocStoreApp.Repositories;

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
            DataBus.RefreshStructureRequested += async () => await LoadData();

            // Инициализация коллекций
            Sections = new ObservableCollection<Section>();
            Orders = new ObservableCollection<Order>();

            DeleteOrderCommand = new RelayCommand(_ => ExecuteDeleteOrder(), _ => SelectedOrder != null);


            // Загружаем данные при старте
            Task.Run(LoadData);
        }

        public ICommand DeleteOrderCommand { get; }

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
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadOrders(); 
                }
            }
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
        public async Task LoadData()
        {
            using (var unitOfWork = new UnitOfWork())
            {
                // Вызываем специализированный метод репозитория
                var data = await unitOfWork.Sections.GetAllWithCatalogsAsync();

                App.Current.Dispatcher.Invoke(() =>
                {
                    Sections.Clear();
                    foreach (var item in data)
                        Sections.Add(item);
                });
            }
        }

        // Загрузка документов с учетом прав и выбранного каталога
        //public async Task LoadOrders()
        //{
        //    using (var db = new ArchiveDbContext())
        //    {
        //        IQueryable<Order> query = db.Orders
        //            .Include(o => o.Author)
        //            .Include(o => o.Catalog);

        //        // 1. Фильтр по удалению:
        //        // Если НЕ Админ - показываем только НЕ удаленные
        //        if (AuthService.CurrentUser.Right.Name != "Администратор")
        //        {
        //            query = query.Where(o => o.isDel == 0);
        //        }

        //        // 2. Фильтр по журналу (если выбран в дереве)
        //        if (SelectedTreeItem is Catalog cat)
        //        {
        //            query = query.Where(o => o.idCatalog == cat.id);
        //        }

        //        var list = await query
        //                    .OrderBy(o => o.isDel)
        //                    .ThenByDescending(o => o.RegDate)
        //                    .ToListAsync();

        //        // Обновляем коллекцию в UI потоке
        //        App.Current.Dispatcher.Invoke(() => {
        //            Orders.Clear();
        //            foreach (var o in list) Orders.Add(o);
        //        });
        //    }
        //}

        public async Task LoadOrders()
        {
            using (var unitOfWork = new UnitOfWork())
            {
                var user = AuthService.CurrentUser;
                if (user == null) return;

                // 1. Получаем параметры из ViewModel
                int? catalogId = (SelectedTreeItem as Catalog)?.id;

                string roleName = user.Right.Name.Trim();
                bool isAdmin = CurrentUser.Right.Name == "Администратор";
                int? executorId = (CurrentUser.Right.Name == "Исполнитель") ? CurrentUser.idExecutor : null;


                // 2. Вся логика (фильтр по удалению, по каталогу, по поиску и сортировка) 
                // теперь живет внутри репозитория Orders.GetArchiveOrdersAsync
                var list = await unitOfWork.Orders.GetArchiveOrdersAsync(
                    catalogId,
                    SearchText,
                    isAdmin,
                    executorId
                );

                // 3. Обновляем UI
                App.Current.Dispatcher.Invoke(() => {
                    Orders.Clear();
                    foreach (var o in list)
                        Orders.Add(o);
                });
            }
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

        public ICommand AddCatalogCommand => new RelayCommand(_ =>
        {
            if (SelectedTreeItem is Section selectedSection)
            {
                string name = Microsoft.VisualBasic.Interaction.InputBox($"Новый журнал для '{selectedSection.SectionName}':", "Название", "");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var newCat = new Catalog { CatalogName = name, idSection = selectedSection.id, NumberNext = 1 };
                    _db.Catalogs.Add(newCat);
                    _db.SaveChanges();
                    _ = LoadData(); // Перегружаем дерево
                }
            }
            else
            {
                MessageBox.Show("Сначала выберите РАЗДЕЛ (папку) в дереве!");
            }
        });

        private async void ExecuteDeleteOrder()
        {
            if (SelectedOrder == null) return;

            // Проверка прав: обычно удалять могут только Админы или Регистраторы (свои документы)
            var user = AuthService.CurrentUser;
            bool canDelete = user.Right.Name == "Администратор" ||
                             (user.Right.Name == "Регистратор" && SelectedOrder.idUser == user.id);

            if (!canDelete)
            {
                MessageBox.Show("У вас недостаточно прав для удаления этого документа.");
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите перенести документ №{SelectedOrder.NumberReg} в корзину?",
                                         "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new ArchiveDbContext())
                    {
                        // Проверяем, не заблокирован ли документ (не открыт ли кем-то другим)
                        var order = db.Orders.Find(SelectedOrder.id);
                        if (order.idUserOpen != null && order.idUserOpen != user.id)
                        {
                            MessageBox.Show("Невозможно удалить документ, так как он редактируется другим пользователем.");
                            return;
                        }

                        // МЯГКОЕ УДАЛЕНИЕ
                        order.isDel = 1;
                        await db.SaveChangesAsync();
                    }

                    MessageBox.Show("Документ перенесен в корзину.");
                    await LoadOrders(); // Обновляем список
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении: " + ex.Message);
                }
            }
        }


    }
}