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
        public ICommand ToggleExecutorActiveCommand { get; }


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
            ToggleExecutorActiveCommand = new RelayCommand(obj => ToggleExecutorActive(obj as Executor));

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

                //// 1. Базовая валидация
                //if (string.IsNullOrEmpty(inputName) || string.IsNullOrEmpty(inputLogin))
                //{
                //    MessageBox.Show("Поля ФИО и Логин должны быть заполнены!");
                //    return;
                //}
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
                            user.Dol = editWin.DolBox.Text.Trim();
                            user.Login = inputLogin;
                            user.idRights = (int)editWin.RoleCombo.SelectedValue;

                            // Обновление пароля, если введено что-то новое
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
            // 1. ПРОВЕРКА: Выбран ли пользователь в списке?
            if (SelectedUser == null)
            {
                MessageBox.Show("Пожалуйста, выберите пользователя в таблице для изменения статуса.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. ПРОВЕРКА: Не пытается ли админ заблокировать самого себя? (Senior Practice)
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
                        // Переключаем статус
                        user.Active = (user.Active == 1) ? 0 : 1;

                        uow.Users.Update(user);
                        await uow.CompleteAsync();

                        // Если есть связанный исполнитель — синхронизируем и его статус
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

                // Обновляем данные в интерфейсе
                await LoadAllData();

                // Оповещаем другие окна об изменении (если этот пользователь был активным исполнителем)
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
            if (SelectedSection == null) return;

            // 1. Спрашиваем подтверждение
            var result = MessageBox.Show(
                $"Вы уверены, что хотите полностью удалить раздел '{SelectedSection.SectionName}'?\n" +
                "Все журналы и документы внутри него будут удалены БЕЗВОЗВРАТНО!",
                "Удаление раздела", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var uow = new UnitOfWork())
                {
                    // Начинаем транзакцию
                    await uow.BeginTransactionAsync();

                    try
                    {
                        // 2. Находим раздел в базе со всеми вложенными данными
                        // (Предполагаем, что у тебя в репозитории есть доступ к коллекциям)
                        var sectionFromDb = await uow.Sections.GetByIdAsync(SelectedSection.id);

                        if (sectionFromDb != null)
                        {
                            // 3. Удаляем журналы этой секции
                            // Сначала найдем все журналы, принадлежащие этой секции
                            var catalogs = await uow.Catalogs.FindAsync(c => c.idSection == sectionFromDb.id);

                            foreach (var catalog in catalogs)
                            {
                                // 4. Удаляем документы каждого журнала
                                var orders = await uow.Orders.FindAsync(o => o.idCatalog == catalog.id);
                                foreach (var order in orders)
                                {
                                    uow.Orders.Remove(order);
                                }

                                // Удаляем сам журнал
                                uow.Catalogs.Remove(catalog);
                            }

                            // 5. Удаляем саму секцию
                            uow.Sections.Remove(sectionFromDb);

                            // Сохраняем всё разом
                            await uow.CompleteAsync();
                            await uow.CommitTransactionAsync();

                            MessageBox.Show("Раздел и всё его содержимое успешно удалены.");
                        }
                    }
                    catch (Exception innerEx)
                    {
                        await uow.RollbackTransactionAsync();
                        throw innerEx; // Пробрасываем ошибку в основной блок
                    }
                }

                // Обновляем интерфейс
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
                    // 1. ПРАВИЛЬНАЯ ПРОВЕРКА: есть ли этот человек в таблице OrderExecutor (назначен на документы)
                    // Раньше здесь могла быть ошибка с поиском в другой таблице
                    var orderLinks = await uow.OrderExecutors.FindAsync(oe => oe.idExecutor == SelectedExecutor.id);

                    if (orderLinks.Any())
                    {
                        // Если связи с документами ЕСТЬ — удалять нельзя (целостность данных)
                        MessageBox.Show("Этот исполнитель назначен на документы в архиве. " +
                                        "Его нельзя удалить, но мы деактивируем его, чтобы он не предлагался в новых списках.", "Информация");

                        var execToDeactivate = await uow.Executors.GetByIdAsync(SelectedExecutor.id);
                        execToDeactivate.Active = 0;
                        uow.Executors.Update(execToDeactivate);
                        await uow.CompleteAsync();
                    }
                    else
                    {
                        // 2. Связей с документами НЕТ. Теперь проверяем связи с Пользователями (таблица User)
                        // Если какой-то пользователь ссылается на этого исполнителя, SQL не даст его удалить.
                        var linkedUsers = await uow.Users.FindAsync(u => u.idExecutor == SelectedExecutor.id);

                        foreach (var user in linkedUsers)
                        {
                            user.idExecutor = null; // Разрываем связь в таблице User
                            uow.Users.Update(user);
                        }
                        // Сохраняем разрыв связей с пользователями
                        await uow.CompleteAsync();

                        // 3. Теперь, когда все связи разорваны, удаляем из таблицы Executor физически
                        var executorToDelete = await uow.Executors.GetByIdAsync(SelectedExecutor.id);
                        if (executorToDelete != null)
                        {
                            uow.Executors.Remove(executorToDelete);
                            await uow.CompleteAsync();
                        }

                        // 4. Удаляем из коллекции на экране
                        App.Current.Dispatcher.Invoke(() => {
                            Executors.Remove(SelectedExecutor);
                            SelectedExecutor = null;
                        });

                        MessageBox.Show("Исполнитель полностью удален из справочника.");
                    }
                }

                // Обновляем всё дерево и списки для синхронизации
                await LoadAllData();
                DataBus.SendRefreshRequest();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}\n\n" +
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
                    // 1. Считаем, сколько документов привязано к этому журналу
                    var ordersInCatalog = await uow.Orders.FindAsync(o => o.idCatalog == SelectedCatalog.id);
                    int count = ordersInCatalog.Count();

                    string message;
                    MessageBoxImage icon;

                    if (count > 0)
                    {
                        // Если документы есть — жесткое предупреждение
                        message = $"ВНИМАНИЕ! В журнале '{SelectedCatalog.CatalogName}' найдено документов: {count} шт.\n\n" +
                                  "Если вы удалите журнал, ВСЕ эти документы будут БЕЗВОЗВРАТНО удалены из системы вместе с файлами!\n\n" +
                                  "Вы действительно хотите продолжить?";
                        icon = MessageBoxImage.Stop; // Иконка критического предупреждения
                    }
                    else
                    {
                        // Если журнал пуст — обычный вопрос
                        message = $"Вы уверены, что хотите удалить пустой журнал '{SelectedCatalog.CatalogName}'?";
                        icon = MessageBoxImage.Question;
                    }

                    // 2. Запрашиваем подтверждение
                    var result = MessageBox.Show(message, "Удаление журнала", MessageBoxButton.YesNo, icon);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Если пользователь подтвердил (даже массовое удаление)
                        var catalogToDelete = await uow.Catalogs.GetByIdAsync(SelectedCatalog.id);
                        if (catalogToDelete != null)
                        {
                            uow.Catalogs.Remove(catalogToDelete);
                            await uow.CompleteAsync(); // EF Core удалит вложенные Orders благодаря ON DELETE CASCADE

                            MessageBox.Show("Журнал и все связанные данные успешно удалены.");

                            // Обновляем интерфейс админки
                            await LoadStructure();

                            // Сообщаем Главному окну, что дерево изменилось
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