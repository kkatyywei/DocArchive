using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UDocStoreApp.Infrastructure;
using UDocStoreApp.Models;
using UDocStoreApp.Repositories;
using UDocStoreApp.Views;

namespace UDocStoreApp.ViewModels
{
    public class AdminViewModel : ViewModelBase
    {
        private Section _selectedSection;
        private User _selectedUserForExecutor;
        private Executor _selectedExecutor;
        private User _selectedUser;
        private Catalog _selectedCatalog;

        // Коллекции для UI
        public ObservableCollection<User> Users { get; set; } = new ObservableCollection<User>();
        public ObservableCollection<Section> Sections { get; set; } = new ObservableCollection<Section>();
        public ObservableCollection<Executor> Executors { get; set; } = new ObservableCollection<Executor>();
        public PassParam Policy { get; set; }

        // Свойства выделения
        public Section SelectedSection { get => _selectedSection; set => SetProperty(ref _selectedSection, value); }
        public User SelectedUserForExecutor { get => _selectedUserForExecutor; set => SetProperty(ref _selectedUserForExecutor, value); }
        public Executor SelectedExecutor { get => _selectedExecutor; set => SetProperty(ref _selectedExecutor, value); }
        public User SelectedUser { get => _selectedUser; set => SetProperty(ref _selectedUser, value); }
        public Catalog SelectedCatalog
        {
            get => _selectedCatalog;
            set
            {
                if (SetProperty(ref _selectedCatalog, value))
                {
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // Команды
        public ICommand EditUserCommand { get; }
        public ICommand BlockUserCommand { get; }
        public ICommand SavePolicyCommand { get; }
        public ICommand AddSectionCommand { get; }
        public ICommand AddCatalogCommand { get; }
        public ICommand DeleteSectionCommand { get; }
        public ICommand MakeExecutorCommand { get; }
        public ICommand DeleteExecutorCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand DeleteCatalogCommand { get; }

        public AdminViewModel()
        {
            // Инициализация команд
            EditUserCommand = new RelayCommand(_ => EditUser(), _ => SelectedUser != null);
            BlockUserCommand = new RelayCommand(_ => BlockUser(), _ => SelectedUser != null);
            SavePolicyCommand = new RelayCommand(_ => SavePolicy());
            AddSectionCommand = new RelayCommand(_ => AddSection());
            AddCatalogCommand = new RelayCommand(_ => AddCatalog(), _ => SelectedSection != null);
            DeleteSectionCommand = new RelayCommand(_ => DeleteSection(), _ => SelectedSection != null);
            MakeExecutorCommand = new RelayCommand(_ => MakeExecutor(), _ => SelectedUserForExecutor != null);
            DeleteExecutorCommand = new RelayCommand(_ => DeleteExecutor(), _ => SelectedExecutor != null);
            AddUserCommand = new RelayCommand(_ => AddUser());
            DeleteCatalogCommand = new RelayCommand(_ => DeleteCatalog(), _ => SelectedCatalog != null);

            // Первичная загрузка данных
            _ = LoadAllData();
        }

        private async Task LoadAllData()
        {
            using (var uow = new UnitOfWork())
            {
                int? selectedId = SelectedSection?.id;

                var users = await uow.Users.GetAllWithRightsAsync();
                var sections = await uow.Sections.GetAllWithCatalogsAsync();
                var executors = await uow.Executors.GetAllAsync();
                var policy = (await uow.PassParams.GetAllAsync()).FirstOrDefault();
                var data = await uow.Sections.GetAllWithCatalogsAsync();

                App.Current.Dispatcher.Invoke(() =>
                {
                    Sections.Clear();
                    foreach (var s in data) Sections.Add(s);
                     if (selectedId.HasValue)
            {
                SelectedSection = Sections.FirstOrDefault(s => s.id == selectedId.Value);
            }
                    Users.Clear(); foreach (var u in users) Users.Add(u);
                    Sections.Clear(); foreach (var s in sections) Sections.Add(s);
                    Executors.Clear(); foreach (var e in executors) Executors.Add(e);
                    Policy = policy ?? new PassParam { id = 1 };
                    OnPropertyChanged(nameof(Policy));
                });
            }
        }

        private async void AddUser()
        {
            var win = new AddUserWindow();
            win.Owner = Application.Current.Windows.OfType<AdminWindow>().FirstOrDefault();

            if (win.ShowDialog() == true)
            {
                string inputName = win.NameBox.Text?.Trim();
                string inputLogin = win.LoginBox.Text?.Trim();

                if (string.IsNullOrEmpty(inputName) || string.IsNullOrEmpty(inputLogin))
                {
                    MessageBox.Show("ФИО и Логин не могут быть пустыми!", "Валидация");
                    return;
                }
                try
                {
                    using (var uow = new UnitOfWork())
                    {
                        // 2. ПРОВЕРКА НА СУЩЕСТВУЮЩЕЕ ФИО
                        // Ищем в базе пользователя с таким же именем (регистр игнорируется SQL сервером)
                        var existingUsers = await uow.Users.FindAsync(u => u.Name.ToLower() == inputName.ToLower());

                        if (existingUsers.Any())
                        {
                            MessageBox.Show($"Пользователь с ФИО '{inputName}' уже зарегистрирован в системе!",
                                            "Дубликат данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return; // Прерываем выполнение, не сохраняем
                        }

                        // 3. ПРОВЕРКА НА ЛОГИН (так как он Unique в БД)
                        var existingLogins = await uow.Users.FindAsync(u => u.Login.ToLower() == inputLogin.ToLower());
                        if (existingLogins.Any())
                        {
                            MessageBox.Show($"Логин '{inputLogin}' уже занят другим пользователем!",
                                            "Дубликат логина", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // 4. Если проверки пройдены — создаем
                        var newUser = new Models.User
                        {
                            Name = inputName,
                            Login = inputLogin,
                            Password = Infrastructure.PasswordHasher.GetMD5Hash(win.PassBox.Password),
                            idRights = (int)win.RoleCombo.SelectedValue,
                            Active = 1,
                            ChangePassword = win.ForceChangeCheck.IsChecked == true ? 1 : 0
                        };

                        await uow.Users.AddAsync(newUser);
                        await uow.CompleteAsync();

                        MessageBox.Show("Пользователь успешно создан!");
                    }

                    // Обновляем список в UI
                    await LoadAllData();
                }

                catch (Exception ex)
                {
                    MessageBox.Show($"Критическая ошибка базы данных: {ex.Message}\n\nПопробуйте повторить операцию позже.",
                                               "Системная ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void EditUser()
        {

            if (SelectedUser == null) return;

            var editWin = new AddUserWindow();
            editWin.NameBox.Text = SelectedUser.Name;
            editWin.LoginBox.Text = SelectedUser.Login;
            editWin.RoleCombo.SelectedValue = SelectedUser.idRights;
            editWin.Title = "Редактирование пользователя";

            if (editWin.ShowDialog() == true)
            {
                string inputName = editWin.NameBox.Text?.Trim();
                string inputLogin = editWin.LoginBox.Text?.Trim();

                // 1. Базовая валидация
                if (string.IsNullOrEmpty(inputName) || string.IsNullOrEmpty(inputLogin))
                {
                    MessageBox.Show("Поля ФИО и Логин должны быть заполнены!");
                    return;
                }
                try
                {
                    using (var uow = new UnitOfWork())
                    {
                        // 2. ПРОВЕРКА НА ДУБЛИКАТ ФИО
                        // Ищем: есть ли КТО-ТО ДРУГОЙ (id != мой ID) с таким же ФИО
                        var nameDuplicate = await uow.Users.FindAsync(u =>
                            u.Name.ToLower() == inputName.ToLower() &&
                            u.id != SelectedUser.id);

                        if (nameDuplicate.Any())
                        {
                            MessageBox.Show($"Пользователь с ФИО '{inputName}' уже существует в базе под другим ID!",
                                            "Дубликат ФИО", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // 3. ПРОВЕРКА НА ДУБЛИКАТ ЛОГИНА
                        // Ищем: есть ли КТО-ТО ДРУГОЙ (id != мой ID) с таким же Логином
                        var loginDuplicate = await uow.Users.FindAsync(u =>
                            u.Login.ToLower() == inputLogin.ToLower() &&
                            u.id != SelectedUser.id);

                        if (loginDuplicate.Any())
                        {
                            MessageBox.Show($"Логин '{inputLogin}' уже занят другим сотрудником!",
                                            "Дубликат логина", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // 4. Если всё чисто — обновляем данные
                        var user = await uow.Users.GetByIdAsync(SelectedUser.id);
                        if (user != null)
                        {
                            user.Name = inputName;
                            user.Login = inputLogin;
                            user.idRights = (int)editWin.RoleCombo.SelectedValue;

                            // Обновление пароля, если введено что-то новое
                            if (!string.IsNullOrEmpty(editWin.PassBox.Password))
                            {
                                user.Password = PasswordHasher.GetMD5Hash(editWin.PassBox.Password);
                                user.ChangePassword = editWin.ForceChangeCheck.IsChecked == true ? 1 : 0;
                            }

                            uow.Users.Update(user);
                            await uow.CompleteAsync();

                            MessageBox.Show("Данные успешно обновлены.");
                        }
                    }

                    // Обновляем таблицу в главном окне админки
                    await LoadAllData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}", "Ошибка");
                }
            }
        }

        private async void BlockUser()
        {
            using (var uow = new UnitOfWork())
            {
                var user = await uow.Users.GetByIdAsync(SelectedUser.id);
                user.Active = (user.Active == 1) ? 0 : 1;
                uow.Users.Update(user);
                await uow.CompleteAsync();
            }
            await LoadAllData();
        }

        private async void SavePolicy()
        {
            using (var uow = new UnitOfWork())
            {
                try
                {
                    // 1. Ищем в базе существующую запись политики (id всегда 1)
                    var dbPolicy = await uow.PassParams.GetByIdAsync(1);

                    if (dbPolicy == null)
                    {
                        // Если записи вдруг нет, создаем новую
                        await uow.PassParams.AddAsync(Policy);
                    }
                    else
                    {
                        // 2. Копируем значения из объекта Policy (который привязан к UI) 
                        // в объект dbPolicy (который отслеживается текущим UnitOfWork)
                        dbPolicy.Strength = Policy.Strength;
                        dbPolicy.MinWidth = Policy.MinWidth;
                        dbPolicy.MinWidthCheck = Policy.MinWidthCheck;
                        dbPolicy.MaxPeriod = Policy.MaxPeriod;
                        dbPolicy.MaxPeriodCheck = Policy.MaxPeriodCheck;
                        dbPolicy.MinPeriod = Policy.MinPeriod;
                        dbPolicy.MinPeriodCheck = Policy.MinPeriodCheck;
                        dbPolicy.CountLast = Policy.CountLast;
                        dbPolicy.CountLastCheck = Policy.CountLastCheck;

                        // Уведомляем EF, что объект изменен
                        uow.PassParams.Update(dbPolicy);
                    }

                    // 3. Сохраняем изменения
                    await uow.CompleteAsync();
                    MessageBox.Show("Политика безопасности успешно обновлена!", "Система");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при сохранении политики: " + ex.Message);
                }
            }
        }

        private async void AddSection()
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Название:", "Создание");
            if (string.IsNullOrWhiteSpace(name)) return;

            using (var uow = new UnitOfWork())
            {
                await uow.Sections.AddAsync(new Section { SectionName = name });
                await uow.CompleteAsync();
            }

            // 1. Обновляем список в самой админке (то, что мы делали)
            await LoadStructure();

            // 2. АВТОМАТИЧЕСКИ обновляем дерево в Главном окне
            DataBus.SendRefreshRequest();
        }

        private async void AddCatalog()
        {
            if (SelectedSection == null) return;
            string name = Microsoft.VisualBasic.Interaction.InputBox("Название:", "Создание");
            if (string.IsNullOrWhiteSpace(name)) return;

            using (var uow = new UnitOfWork())
            {
                await uow.Catalogs.AddAsync(new Catalog { CatalogName = name, idSection = SelectedSection.id, NumberNext = 1 });
                await uow.CompleteAsync();
            }

            // Обновляем админку
            await LoadStructure();

            // АВТОМАТИЧЕСКИ обновляем Главное окно
            DataBus.SendRefreshRequest();
        }

        private async void DeleteSection()
        {
            if (MessageBox.Show("Удалить раздел и все вложенные журналы?", "Внимание", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var uow = new UnitOfWork())
                {
                    var section = await uow.Sections.GetByIdAsync(SelectedSection.id);
                    uow.Sections.Remove(section);
                    await uow.CompleteAsync();
                }
                await LoadAllData();
            }
        }

        private async void MakeExecutor()
        {
            using (var uow = new UnitOfWork())
            {
                var executors = await uow.Executors.FindAsync(e => e.FIO == SelectedUserForExecutor.Name);
                if (executors.Any())
                {
                    MessageBox.Show("Исполнитель уже существует");
                    return;
                }

                await uow.BeginTransactionAsync();
                try
                {
                    var newExec = new Executor { FIO = SelectedUserForExecutor.Name, Dol = "Сотрудник", Active = 1 };
                    await uow.Executors.AddAsync(newExec);
                    await uow.CompleteAsync();

                    var user = await uow.Users.GetByIdAsync(SelectedUserForExecutor.id);
                    user.idExecutor = newExec.id;
                    uow.Users.Update(user);

                    await uow.CompleteAsync();
                    await uow.CommitTransactionAsync();
                    MessageBox.Show("Исполнитель назначен");
                }
                catch { await uow.RollbackTransactionAsync(); }
            }
            await LoadAllData();
        }

        private async void DeleteExecutor()
        {
            if (MessageBox.Show("Удалить исполнителя?", "Удаление", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            using (var uow = new UnitOfWork())
            {
                var executor = await uow.Executors.GetByIdAsync(SelectedExecutor.id);
                // Проверка связей через UnitOfWork
                var hasOrders = (await uow.OrderFiles.FindAsync(oe => oe.id == executor.id)).Any(); // Здесь лучше использовать OrderExecutor репозиторий

                if (hasOrders)
                {
                    executor.Active = 0;
                    uow.Executors.Update(executor);
                    MessageBox.Show("Исполнитель деактивирован, так как связан с документами");
                }
                else
                {
                    var linkedUsers = await uow.Users.FindAsync(u => u.idExecutor == executor.id);
                    foreach (var u in linkedUsers) { u.idExecutor = null; uow.Users.Update(u); }
                    uow.Executors.Remove(executor);
                    MessageBox.Show("Исполнитель полностью удален");
                }
                await uow.CompleteAsync();
            }
            await LoadAllData();
        }

        private async void SaveAll()
        {
            // В паттерне UnitOfWork мы сохраняем изменения по завершении конкретных действий.
            // Но если нужно массовое сохранение из DataGrid:
            using (var uow = new UnitOfWork())
            {
                // При такой архитектуре изменения в UI объектах (Users) должны быть засинхронены с БД
                foreach (var user in Users) uow.Users.Update(user);
                await uow.CompleteAsync();
            }
            MessageBox.Show("Данные синхронизированы");
        }

        private async void DeleteCatalog()
        {
            if (SelectedCatalog == null) return;

            var result = MessageBox.Show($"Вы действительно хотите удалить журнал '{SelectedCatalog.CatalogName}'?\n" +
                                         "Внимание: это действие невозможно отменить!",
                                         "Удаление журнала", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            using (var uow = new UnitOfWork())
            {
                try
                {
                    // Проверяем, есть ли в этом журнале документы
                    var hasOrders = (await uow.Orders.FindAsync(o => o.idCatalog == SelectedCatalog.id)).Any();

                    if (hasOrders)
                    {
                        MessageBox.Show("Невозможно удалить журнал, так как в нем содержатся документы. " +
                                        "Сначала удалите или перенесите все документы из этого журнала.",
                                        "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Если журнал пуст — удаляем
                    var catalogToDelete = await uow.Catalogs.GetByIdAsync(SelectedCatalog.id);
                    if (catalogToDelete != null)
                    {
                        uow.Catalogs.Remove(catalogToDelete);
                        await uow.CompleteAsync();

                        MessageBox.Show("Журнал успешно удален.");

                        // Обновляем структуру в интерфейсе
                        await LoadAllData();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при взаимодействии с базой данных: " + ex.Message);
                }
            }
        }

        private async Task LoadStructure()
        {
            using (var uow = new UnitOfWork())
            {
                // Загружаем данные
                var data = await uow.Sections.GetAllWithCatalogsAsync();

                App.Current.Dispatcher.Invoke(() =>
                {
                    Sections.Clear();
                    foreach (var s in data) Sections.Add(s);
                });
            }
        }
    }
}