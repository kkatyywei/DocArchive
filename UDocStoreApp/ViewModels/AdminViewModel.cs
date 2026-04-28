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

            // Первичная загрузка данных
            _ = LoadAllData();
        }

        private async Task LoadAllData()
        {
            using (var uow = new UnitOfWork())
            {
                var users = await uow.Users.GetAllWithRightsAsync();
                var sections = await uow.Sections.GetAllWithCatalogsAsync();
                var executors = await uow.Executors.GetAllAsync();
                var policy = (await uow.PassParams.GetAllAsync()).FirstOrDefault();

                App.Current.Dispatcher.Invoke(() =>
                {
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
                try
                {
                    using (var uow = new UnitOfWork())
                    {
                        // Создаем сущность из данных окна
                        var newUser = new Models.User
                        {
                            Name = win.NameBox.Text,
                            Login = win.LoginBox.Text,
                            Password = Infrastructure.PasswordHasher.GetMD5Hash(win.PassBox.Password),
                            idRights = (int)win.RoleCombo.SelectedValue,
                            Active = 1,
                            ChangePassword = win.ForceChangeCheck.IsChecked == true ? 1 : 0
                        };

                        // Сохраняем через репозиторий
                        await uow.Users.AddAsync(newUser);
                        await uow.CompleteAsync();
                    }

                    // Обновляем список в UI
                    await LoadAllData();
                    MessageBox.Show("Пользователь успешно создан!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при создании пользователя: " + ex.Message);
                }
            }
        }

        private async void EditUser()
        {
            var editWin = new AddUserWindow();
            editWin.NameBox.Text = SelectedUser.Name;
            editWin.LoginBox.Text = SelectedUser.Login;
            editWin.RoleCombo.SelectedValue = SelectedUser.idRights;
            editWin.Title = "Редактирование пользователя";

            if (editWin.ShowDialog() == true)
            {
                using (var uow = new UnitOfWork())
                {
                    var user = await uow.Users.GetByIdAsync(SelectedUser.id);
                    user.Name = editWin.NameBox.Text;
                    user.Login = editWin.LoginBox.Text;
                    user.idRights = (int)editWin.RoleCombo.SelectedValue;

                    if (!string.IsNullOrEmpty(editWin.PassBox.Password))
                    {
                        user.Password = PasswordHasher.GetMD5Hash(editWin.PassBox.Password);
                        user.ChangePassword = editWin.ForceChangeCheck.IsChecked == true ? 1 : 0;
                    }

                    uow.Users.Update(user);
                    await uow.CompleteAsync();
                }
                await LoadAllData();
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
                var p = await uow.PassParams.GetByIdAsync(1);
                if (p == null) await uow.PassParams.AddAsync(Policy);
                else uow.PassParams.Update(Policy);

                await uow.CompleteAsync();
                MessageBox.Show("Политика обновлена");
            }
        }

        private async void AddSection()
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Название раздела:", "Новый раздел");
            if (!string.IsNullOrWhiteSpace(name))
            {
                using (var uow = new UnitOfWork())
                {
                    await uow.Sections.AddAsync(new Section { SectionName = name });
                    await uow.CompleteAsync();
                }
                await LoadAllData();
            }
        }

        private async void AddCatalog()
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Название журнала:", "Новый журнал");
            if (!string.IsNullOrWhiteSpace(name))
            {
                using (var uow = new UnitOfWork())
                {
                    await uow.Catalogs.AddAsync(new Catalog { CatalogName = name, idSection = SelectedSection.id, NumberNext = 1 });
                    await uow.CompleteAsync();
                }
                await LoadAllData();
            }
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
    }
}