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
        private DateTime? _startDate;
        private DateTime? _endDate;

        public MainViewModel()
        {
            _db = new ArchiveDbContext();
            LoadDataCommand = new RelayCommand(async _ => await LoadData());
            SearchCommand = new RelayCommand(_ => ApplyFilter());

            DeleteOrderCommand = new RelayCommand(_ => ExecuteDeleteOrder(), _ => SelectedOrder != null && SelectedOrder.isDel == 0);
            DataBus.RefreshStructureRequested += async () => {
                await LoadData();
                await LoadExecutors();
            };

            DataBus.RefreshStructureRequested += async () => await LoadData();
            Sections = new ObservableCollection<Section>();
            Orders = new ObservableCollection<Order>();


            Task.Run(LoadData);
            _ = LoadExecutors();

        }
        public ObservableCollection<Executor> AllExecutors { get; set; } = new ObservableCollection<Executor>();

        public User CurrentUser => AuthService.CurrentUser;

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


        public Order SelectedOrder
        {
            get => _selectedOrder;
            set => SetProperty(ref _selectedOrder, value);
        }
        public DateTime? StartDate
        {
            get => _startDate;
            set { if (SetProperty(ref _startDate, value)) _ = LoadOrders(); }
        }
        public DateTime? EndDate
        {
            get => _endDate;
            set { if (SetProperty(ref _endDate, value)) _ = LoadOrders(); }
        }
        public ICommand DeleteOrderCommand { get; }
        public ICommand LoadDataCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand => new RelayCommand(_ => {
            SearchText = string.Empty;
            StartDate = null;
            EndDate = null;
        });

        public string ShortName
        {
            get
            {

                if (string.IsNullOrWhiteSpace(CurrentUser.Name)) return "";
                if (CurrentUser.Name == "admin") return CurrentUser.Name;
                var parts = CurrentUser.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return "";
                string lastName = parts[0];
                string initials = string.Concat(parts.Skip(1).Select(p => p[0] + "."));
                return $"{lastName} {initials}";
            }
        }

        // sections+catalogs
        public async Task LoadData()
        {
            using (var unitOfWork = new UnitOfWork())
            {
                var data = await unitOfWork.Sections.GetAllWithCatalogsAsync();

                App.Current.Dispatcher.Invoke(() =>
                {
                    Sections.Clear();
                    foreach (var item in data)
                        Sections.Add(item);
                });
            }
        }

        public async Task LoadOrders()
        {
            using (var unitOfWork = new UnitOfWork())
            {
                var user = AuthService.CurrentUser;
                if (user == null) return;

                int? catalogId = (SelectedTreeItem as Catalog)?.id;

                string roleName = user.Right.Name.Trim();
                bool isAdmin = CurrentUser.Right.Name == "Администратор";
                int? executorId = (CurrentUser.Right.Name == "Исполнитель") ? CurrentUser.idExecutor : null;


                var list = await unitOfWork.Orders.GetArchiveOrdersAsync(
                    catalogId,
                    SearchText,
                    isAdmin,
                    executorId,
                    StartDate,
                    EndDate
                );

                App.Current.Dispatcher.Invoke(() => {
                    Orders.Clear();
                    foreach (var o in list)
                        Orders.Add(o);
                });
            }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                _ = LoadOrders();
                return;
            }

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

                _ = LoadOrders(); 
            }
            else
            {
                MessageBox.Show("Сначала выберите журнал в дереве слева!");
            }
        });

        public ICommand AddCatalogCommand => new RelayCommand(async _ =>
        {
            if (!(SelectedTreeItem is Section selectedSection))
            {
                MessageBox.Show("Сначала выберите РАЗДЕЛ (папку) в дереве слева!",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string name = Microsoft.VisualBasic.Interaction.InputBox(
                $"Создание нового журнала в разделе '{selectedSection.SectionName}':",
                "Новый журнал", "")?.Trim();


            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                using (var uow = new UnitOfWork())
                {
                    var duplicate = await uow.Catalogs.FindAsync(c =>
                        c.CatalogName.ToLower() == name.ToLower() &&
                        c.idSection == selectedSection.id);

                    if (duplicate.Any())
                    {
                        MessageBox.Show($"В разделе '{selectedSection.SectionName}' уже существует журнал с названием '{name}'!",
                                        "Дубликат", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var newCat = new Catalog
                    {
                        CatalogName = name,
                        idSection = selectedSection.id,
                        NumberNext = 1
                    };

                    await uow.Catalogs.AddAsync(newCat);
                    await uow.CompleteAsync(); 

                    await LoadData();

                    MessageBox.Show($"Журнал '{name}' успешно создан и добавлен в раздел '{selectedSection.SectionName}'.",
                                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось добавить журнал: {ex.Message}",
                                "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
        private async void ExecuteDeleteOrder()
        {
            if (SelectedOrder == null) return;

            if (SelectedOrder.isDel == 1)
            {
                MessageBox.Show(
                    "Данный документ уже удален.",
                    "Информация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return; 
            }
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
                        var order = db.Orders.Find(SelectedOrder.id);
                        order.isDel = 1;
                        await db.SaveChangesAsync();
                    }

                    MessageBox.Show("Документ удален.");
                    await LoadOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении: " + ex.Message);
                }
            }
        }
        public async Task LoadExecutors()
        {
            using (var uow = new UnitOfWork())
            {
                var list = await uow.Executors.FindAsync(e => e.Active == 1);

                App.Current.Dispatcher.Invoke(() => {
                    AllExecutors.Clear();
                    foreach (var e in list.OrderBy(x => x.FIO))
                        AllExecutors.Add(e);
                });
            }
        }

    }
}