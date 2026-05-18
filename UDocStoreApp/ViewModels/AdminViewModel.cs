using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UDocStoreApp.Infrastructure;
using UDocStoreApp.Models;
using UDocStoreApp.Repositories;
using UDocStoreApp.Services;
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

        public ObservableCollection<User> Users { get; set; } = new ObservableCollection<User>();
        public ObservableCollection<Section> Sections { get; set; } = new ObservableCollection<Section>();
        public ObservableCollection<Executor> Executors { get; set; } = new ObservableCollection<Executor>();
        public PassParam Policy { get; set; }

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

        public ICommand EditUserCommand { get; }
        public ICommand BlockUserCommand { get; }
        public ICommand SavePolicyCommand { get; }
        public ICommand AddSectionCommand { get; }
        public ICommand AddCatalogCommand { get; }
        public ICommand DeleteSectionCommand { get; }
        public ICommand MakeExecutorCommand { get; }
        public ICommand DeleteExecutorCommand { get; }
        public ICommand UnlockExecutorCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand DeleteCatalogCommand { get; }
        public ICommand ToggleExecutorActiveCommand { get; }


        public AdminViewModel()
        {
            EditUserCommand = new RelayCommand(_ => EditUser(), _ => SelectedUser != null);
            BlockUserCommand = new RelayCommand(_ => BlockUser(), _ => SelectedUser != null);
            SavePolicyCommand = new RelayCommand(_ => SavePolicy());
            AddSectionCommand = new RelayCommand(_ => AddSection());
            AddCatalogCommand = new RelayCommand(_ => AddCatalog(), _ => SelectedSection != null);
            DeleteSectionCommand = new RelayCommand(_ => DeleteSection(), _ => SelectedSection != null);
            MakeExecutorCommand = new RelayCommand(_ => MakeExecutor(), _ => SelectedUserForExecutor != null);
            DeleteExecutorCommand = new RelayCommand(_ => DeleteExecutor(), _ => SelectedExecutor != null);
            UnlockExecutorCommand = new RelayCommand(_ => UnlockExecutor(), _ => SelectedExecutor != null);
            AddUserCommand = new RelayCommand(_ => AddUser());
            DeleteCatalogCommand = new RelayCommand(_ => DeleteCatalog(), _ => SelectedCatalog != null);
            ToggleExecutorActiveCommand = new RelayCommand(obj => ToggleExecutorActive(obj as Executor));

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
        private async void ToggleExecutorActive(Executor exec)
        {
            if (exec == null) return;

            try
            {
                using (var uow = new UnitOfWork())
                {
                    uow.Executors.Update(exec);
                    await uow.CompleteAsync();
                }

                DataBus.SendRefreshRequest();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении статуса: " + ex.Message);
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
                string inputDol = win.DolBox.Text?.Trim();
                string inputPass = win.PassBox.Password;

                if (string.IsNullOrEmpty(inputName) || string.IsNullOrEmpty(inputLogin) || string.IsNullOrEmpty(inputPass))
                {
                    MessageBox.Show("Для создания нового пользователя заполните все поля, включая временный пароль!");
                    return;
                }
                try
                {
                    using (var uow = new UnitOfWork())
                    {
                        var existingUsers = await uow.Users.FindAsync(u => u.Name.ToLower() == inputName.ToLower());
                        if (existingUsers.Any())
                        {
                            MessageBox.Show($"Пользователь с ФИО '{inputName}' уже зарегистрирован в системе!",
                                            "Дубликат данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return; 
                        }

                        var existingLogins = await uow.Users.FindAsync(u => u.Login.ToLower() == inputLogin.ToLower());
                        if (existingLogins.Any())
                        {
                            MessageBox.Show($"Логин '{inputLogin}' уже занят другим пользователем!",
                                            "Дубликат логина", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var newUser = new Models.User
                        {
                            Name = inputName,
                            Dol = win.DolBox.Text.Trim(),
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
            editWin.DolBox.Text = SelectedUser.Dol;
            editWin.RoleCombo.SelectedValue = SelectedUser.idRights;
            editWin.Title = "Редактирование пользователя";
            editWin.PassBox.Tag = "Оставьте пустым, чтобы сохранить старый пароль";
            editWin.ForceChangeCheck.IsChecked = SelectedUser.ChangePassword == 1;

            if (editWin.ShowDialog() == true)
            {
                string inputName = editWin.NameBox.Text?.Trim();
                string inputLogin = editWin.LoginBox.Text?.Trim();
                string inputDol = editWin.DolBox.Text?.Trim();
                string inputPass = editWin.PassBox.Password;

                try
                {
                    using (var uow = new UnitOfWork())
                    {
                        
                        var nameDuplicate = await uow.Users.FindAsync(u => u.Name.ToLower() == inputName.ToLower() && u.id != SelectedUser.id);
                        if (nameDuplicate.Any())
                        {
                            MessageBox.Show($"Пользователь с ФИО '{inputName}' уже существует в базе под другим ID!",
                                            "Дубликат ФИО", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        
                        var loginDuplicate = await uow.Users.FindAsync(u => u.Login.ToLower() == inputLogin.ToLower() && u.id != SelectedUser.id);
                        if (loginDuplicate.Any())
                        {
                            MessageBox.Show($"Логин '{inputLogin}' уже занят другим сотрудником!",
                                            "Дубликат логина", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var user = await uow.Users.GetByIdAsync(SelectedUser.id);
                        if (user != null)
                        {
                            user.Name = inputName;
                            user.Dol = editWin.DolBox.Text.Trim();
                            user.Login = inputLogin;
                            user.idRights = (int)editWin.RoleCombo.SelectedValue;

                            if (!string.IsNullOrEmpty(inputPass))
                            {
                                user.Password = Infrastructure.PasswordHasher.GetMD5Hash(inputPass);
                                user.ChangePassword = editWin.ForceChangeCheck.IsChecked == true ? 1 : 0;
                                MessageBox.Show("Пароль пользователя был обновлен.");
                            }

                            uow.Users.Update(user);
                            await uow.CompleteAsync();

                            MessageBox.Show("Данные успешно обновлены.");
                        }
                    }

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
            if (SelectedUser == null)
            {
                MessageBox.Show("Пожалуйста, выберите пользователя в таблице для изменения статуса.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedUser.id == AuthService.CurrentUser.id)
            {
                MessageBox.Show("Вы не можете заблокировать собственную учетную запись!",
                                "Действие отклонено", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var uow = new UnitOfWork())
                {
                    var user = await uow.Users.GetByIdAsync(SelectedUser.id);
                    if (user != null)
                    {
                        user.Active = (user.Active == 1) ? 0 : 1;

                        uow.Users.Update(user);
                        await uow.CompleteAsync();

                        // status sync with executor
                        if (user.idExecutor.HasValue)
                        {
                            var exec = await uow.Executors.GetByIdAsync(user.idExecutor.Value);
                            if (exec != null)
                            {
                                exec.Active = user.Active;
                                uow.Executors.Update(exec);
                                await uow.CompleteAsync();
                            }
                        }

                        string action = user.Active == 1 ? "разблокирован" : "заблокирован";
                        MessageBox.Show($"Пользователь {user.Name} успешно {action}.");
                    }
                }

                await LoadAllData();

                DataBus.SendRefreshRequest();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении статуса пользователя: {ex.Message}", "Ошибка");
            }
        }
        private async void SavePolicy()
        {
            using (var uow = new UnitOfWork())
            {
                try
                {
                     var dbPolicy = await uow.PassParams.GetByIdAsync(1);

                    if (dbPolicy == null)
                    {
                        await uow.PassParams.AddAsync(Policy);
                    }
                    else
                    {
                        dbPolicy.Strength = Policy.Strength;
                        dbPolicy.MinWidth = Policy.MinWidth;
                        dbPolicy.MinWidthCheck = Policy.MinWidthCheck;
                        dbPolicy.MaxPeriod = Policy.MaxPeriod;
                        dbPolicy.MaxPeriodCheck = Policy.MaxPeriodCheck;
                        dbPolicy.MinPeriod = Policy.MinPeriod;
                        dbPolicy.MinPeriodCheck = Policy.MinPeriodCheck;
                        dbPolicy.CountLast = Policy.CountLast;
                        dbPolicy.CountLastCheck = Policy.CountLastCheck;

                        uow.PassParams.Update(dbPolicy);
                    }

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

            string name = Microsoft.VisualBasic.Interaction.InputBox("Введите название нового раздела:", "Создание раздела")?.Trim(); 
            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                using (var uow = new UnitOfWork())
                {
                    var existing = await uow.Sections.FindAsync(s => s.SectionName.ToLower() == name.ToLower());

                    if (existing.Any())
                    {
                        MessageBox.Show($"Раздел с названием '{name}' уже существует в архиве!",
                                        "Дубликат", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await uow.Sections.AddAsync(new Section { SectionName = name });
                    await uow.CompleteAsync();
                }

                await LoadStructure();
                DataBus.SendRefreshRequest();
                MessageBox.Show("Раздел успешно добавлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private async void AddCatalog()
        {
            if (SelectedSection == null)
            {
                MessageBox.Show("Сначала выберите раздел, в который хотите добавить журнал!");
                return;
            }
            string name = Microsoft.VisualBasic.Interaction.InputBox($"Новый журнал для раздела '{SelectedSection.SectionName}':", "Создание журнала")?.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                using (var uow = new UnitOfWork())
                {
                    var existing = await uow.Catalogs.FindAsync(c =>
                        c.CatalogName.ToLower() == name.ToLower() &&
                        c.idSection == SelectedSection.id);

                    if (existing.Any())
                    {
                        MessageBox.Show($"В разделе '{SelectedSection.SectionName}' уже есть журнал с названием '{name}'!",
                                        "Дубликат внутри раздела", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var newCat = new Catalog
                    {
                        CatalogName = name,
                        idSection = SelectedSection.id,
                        NumberNext = 1
                    };

                    await uow.Catalogs.AddAsync(newCat);
                    await uow.CompleteAsync();
                }

                await LoadStructure();
                DataBus.SendRefreshRequest();
                MessageBox.Show("Журнал успешно добавлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private async void DeleteSection()
        {
            if (SelectedSection == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите полностью удалить раздел '{SelectedSection.SectionName}'?\n" +
                "Все журналы и документы внутри него будут удалены БЕЗВОЗВРАТНО!",
                "Удаление раздела", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var uow = new UnitOfWork())
                {
                    await uow.BeginTransactionAsync();

                    try
                    {
                        // find sectiob with all catalogs
                        var sectionFromDb = await uow.Sections.GetByIdAsync(SelectedSection.id);

                        if (sectionFromDb != null)
                        {
                            // delete all catalogs in this section
                            var catalogs = await uow.Catalogs.FindAsync(c => c.idSection == sectionFromDb.id);

                            foreach (var catalog in catalogs)
                            {
                                // delete all docs in this catalog
                                var orders = await uow.Orders.FindAsync(o => o.idCatalog == catalog.id);
                                foreach (var order in orders)
                                {
                                    uow.Orders.Remove(order);
                                }

                                uow.Catalogs.Remove(catalog);
                            }

                            uow.Sections.Remove(sectionFromDb);

                            await uow.CompleteAsync();
                            await uow.CommitTransactionAsync();

                            MessageBox.Show("Раздел и всё его содержимое успешно удалены.");
                        }
                    }
                    catch (Exception innerEx)
                    {
                        await uow.RollbackTransactionAsync();
                        throw innerEx; 
                    }
                }

                await LoadAllData();
                DataBus.SendRefreshRequest();
                SelectedSection = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении раздела: {ex.Message}\n" +
                                $"Возможно, есть документы, которые нельзя удалить.");
            }
        }

        private async void MakeExecutor()
        {
            if (SelectedUserForExecutor == null || string.IsNullOrWhiteSpace(SelectedUserForExecutor.Name))
            {
                MessageBox.Show("Пожалуйста, выберите пользователя перед назначением исполнителя.", "Нет выбора",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
                    var newExec = new Executor { FIO = SelectedUserForExecutor.Name, Dol = SelectedUserForExecutor.Dol, Active = 1 };
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
            DataBus.SendRefreshRequest();

        }

        private async void DeleteExecutor()
        {
            if (SelectedExecutor == null) return;

            var result = MessageBox.Show($"Вы действительно хотите удалить исполнителя '{SelectedExecutor.FIO}' из справочника?",
                                         "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var uow = new UnitOfWork())
                {
                    var orderLinks = await uow.OrderExecutors.FindAsync(oe => oe.idExecutor == SelectedExecutor.id);

                    if (orderLinks.Any())
                    {
                        MessageBox.Show("Этот исполнитель назначен на документы в архиве. " +
                                        "Его нельзя удалить, но мы деактивируем его, чтобы он не предлагался в новых списках.", "Информация");

                        var execToDeactivate = await uow.Executors.GetByIdAsync(SelectedExecutor.id);
                        execToDeactivate.Active = 0;
                        uow.Executors.Update(execToDeactivate);
                        await uow.CompleteAsync();
                    }
                    else
                    {
                        // no links with docs, delete link with user
                        var linkedUsers = await uow.Users.FindAsync(u => u.idExecutor == SelectedExecutor.id);

                        foreach (var user in linkedUsers)
                        {
                            user.idExecutor = null;
                            uow.Users.Update(user);
                        }
                        await uow.CompleteAsync();

                        var executorToDelete = await uow.Executors.GetByIdAsync(SelectedExecutor.id);
                        if (executorToDelete != null)
                        {
                            uow.Executors.Remove(executorToDelete);
                            await uow.CompleteAsync();
                        }

                        App.Current.Dispatcher.Invoke(() => {
                            Executors.Remove(SelectedExecutor);
                            SelectedExecutor = null;
                        });

                        MessageBox.Show("Исполнитель полностью удален из справочника.");
                    }
                }

                await LoadAllData();
                DataBus.SendRefreshRequest();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}\n\n" +
                                $"Внутренняя ошибка: {ex.InnerException?.Message}");
            }
        }
        private async void UnlockExecutor()
        {
            if (SelectedExecutor == null) return;

            var result = MessageBox.Show($"Вы действительно разблокировать исполнителя '{SelectedExecutor.FIO}' ?",
                                         "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var uow = new UnitOfWork())
                {
                    var execToUnlock = await uow.Executors.GetByIdAsync(SelectedExecutor.id);
                    if (execToUnlock.Active == 0)
                    {
                        

                        execToUnlock.Active = 1;
                        uow.Executors.Update(execToUnlock);
                        await uow.CompleteAsync();
                    }
                    else
                    {
                        // link to docs
                        MessageBox.Show("Этот исполнитель в данный момент активен ", "Информация");
                    }
                }

                await LoadAllData();
                DataBus.SendRefreshRequest();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при разблокировке: {ex.Message}\n\n" +
                                $"Внутренняя ошибка: {ex.InnerException?.Message}");
            }
        }

        private async void DeleteCatalog()
        {
            if (SelectedCatalog == null) return;

            try
            {
                using (var uow = new UnitOfWork())
                {
                    // count docs in catalog
                    var ordersInCatalog = await uow.Orders.FindAsync(o => o.idCatalog == SelectedCatalog.id);
                    int count = ordersInCatalog.Count();

                    string message;
                    MessageBoxImage icon;

                    if (count > 0)
                    {
                        message = $"ВНИМАНИЕ! В журнале '{SelectedCatalog.CatalogName}' найдено документов: {count} шт.\n\n" +
                                  "Если вы удалите журнал, ВСЕ эти документы будут БЕЗВОЗВРАТНО удалены из системы вместе с файлами!\n\n" +
                                  "Вы действительно хотите продолжить?";
                        icon = MessageBoxImage.Stop; 
                    }
                    else
                    {
                        message = $"Вы уверены, что хотите удалить пустой журнал '{SelectedCatalog.CatalogName}'?";
                        icon = MessageBoxImage.Question;
                    }

                    var result = MessageBox.Show(message, "Удаление журнала", MessageBoxButton.YesNo, icon);

                    if (result == MessageBoxResult.Yes)
                    {
                        var catalogToDelete = await uow.Catalogs.GetByIdAsync(SelectedCatalog.id);
                        if (catalogToDelete != null)
                        {
                            uow.Catalogs.Remove(catalogToDelete);
                            await uow.CompleteAsync(); 

                            MessageBox.Show("Журнал и все связанные данные успешно удалены.");

                            await LoadStructure();

                            DataBus.SendRefreshRequest();

                            SelectedCatalog = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}");
            }
        }

        private async Task LoadStructure()
        {
            using (var uow = new UnitOfWork())
            {
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