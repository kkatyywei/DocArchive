using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using UDocStoreApp.Data;
using UDocStoreApp.Models;
using Microsoft.EntityFrameworkCore;
using UDocStoreApp.Infrastructure;
using System.Windows.Input;
using System;
using UDocStoreApp.Views;

namespace UDocStoreApp.ViewModels
{
    public class AdminViewModel : ViewModelBase
    {
        private readonly ArchiveDbContext _db;
        private Section _selectedSection;
        private User _selectedUserForExecutor;
        private Executor _selectedExecutor;
        private User _selectedUser;



        public ObservableCollection<User> Users { get; set; }
        public ObservableCollection<Section> Sections { get; set; } // Список разделов для админки
        public PassParam Policy { get; set; }
        public ObservableCollection<Executor> Executors { get; set; }

        public Section SelectedSection
        {
            get => _selectedSection;
            set => SetProperty(ref _selectedSection, value);
        }
        public User SelectedUserForExecutor
        {
            get => _selectedUserForExecutor;
            set => SetProperty(ref _selectedUserForExecutor, value);
        }
        public Executor SelectedExecutor
        {
            get => _selectedExecutor;
            set => SetProperty(ref _selectedExecutor, value);
        }
        public User SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public ICommand EditUserCommand { get; }
        public ICommand BlockUserCommand { get; }
        public ICommand SaveChangesCommand { get; }

        public AdminViewModel()
        {
            _db = new ArchiveDbContext();

            Users = new ObservableCollection<User>(_db.Users.Include(u => u.Right).ToList());
            Sections = new ObservableCollection<Section>(_db.Sections.Include(s => s.Catalogs).ToList());
            Policy = _db.PassParams.FirstOrDefault() ?? new PassParam { id = 1 };

            LoadUsers();
            LoadStructure();

            Policy = _db.PassParams.FirstOrDefault() ?? new PassParam { id = 1 };

            SavePolicyCommand = new RelayCommand(_ => SavePolicy());
            AddSectionCommand = new RelayCommand(_ => AddSection());
            DeleteSectionCommand = new RelayCommand(_ => DeleteSection(), _ => SelectedSection != null);
            Executors = new ObservableCollection<Executor>(_db.Executors.ToList());
            MakeExecutorCommand = new RelayCommand(_ => MakeExecutor(), _ => SelectedUserForExecutor != null);
            DeleteExecutorCommand = new RelayCommand(obj => DeleteExecutor(), _ => SelectedExecutor != null);
            EditUserCommand = new RelayCommand(_ => EditUser(), _ => SelectedUser != null);
            BlockUserCommand = new RelayCommand(_ => BlockUser(), _ => SelectedUser != null);
            SaveChangesCommand = new RelayCommand(_ => SaveAll());

        }

        private void LoadUsers()
        {
            // Загружаем список пользователей вместе с их ролями из базы
            var data = _db.Users.Include(u => u.Right).ToList();

            // Очищаем текущую коллекцию и заполняем заново
            Users.Clear();
            foreach (var u in data)
            {
                Users.Add(u);
            }
        }
        public ICommand SavePolicyCommand { get; }
        public ICommand AddSectionCommand { get; }
        public ICommand AddCatalogCommand => new RelayCommand(_ =>
        {
            if (SelectedSection == null)
            {
                MessageBox.Show("Сначала выделите раздел в списке слева!");
                return;
            }

            string name = Microsoft.VisualBasic.Interaction.InputBox($"Новый журнал для '{SelectedSection.SectionName}':", "Название", "");
            if (!string.IsNullOrWhiteSpace(name))
            {
                var newCat = new Catalog
                {
                    CatalogName = name,
                    idSection = SelectedSection.id,
                    NumberNext = 1 // Начальный номер для документов
                };
                _db.Catalogs.Add(newCat);
                _db.SaveChanges();

                // Перезагружаем разделы, чтобы дерево обновилось везде
                LoadStructure();
                MessageBox.Show("Журнал добавлен!");
            }
        });
        public ICommand DeleteSectionCommand { get; }
        public ICommand MakeExecutorCommand { get; }
        public ICommand DeleteExecutorCommand { get; }
        public ICommand AddUserCommand => new RelayCommand(_ =>
        {
            var win = new AddUserWindow();
            win.Owner = Application.Current.Windows.OfType<AdminWindow>().FirstOrDefault();
            if (win.ShowDialog() == true)
            {
                // Обновляем список пользователей в таблице
                var updatedUsers = _db.Users.Include(u => u.Right).ToList();
                Users.Clear();
                foreach (var u in updatedUsers) Users.Add(u);
                MessageBox.Show("Пользователь успешно создан!");
            }
        });

        private void SavePolicy()
        {
            if (!_db.PassParams.Any()) _db.PassParams.Add(Policy);
            else _db.PassParams.Update(Policy);

            _db.SaveChanges();
            MessageBox.Show("Политика паролей обновлена!");
        }

        private void AddSection()
        {
            // Простой способ получить строку от пользователя в WPF
            string name = Microsoft.VisualBasic.Interaction.InputBox("Введите название раздела:", "Новый раздел", "Новый раздел");
            if (!string.IsNullOrWhiteSpace(name))
            {
                var newSection = new Section { SectionName = name };
                _db.Sections.Add(newSection);
                _db.SaveChanges();
                Sections.Add(newSection);
            }
        }

        private void AddCatalog()
        {
            if (SelectedSection == null) return;

            string name = Microsoft.VisualBasic.Interaction.InputBox("Название журнала:", "Новый журнал");
            if (!string.IsNullOrWhiteSpace(name))
            {
                var newCat = new Catalog
                {
                    CatalogName = name,
                    idSection = SelectedSection.id,
                    NumberNext = 1
                };
                _db.Catalogs.Add(newCat);
                _db.SaveChanges();

                // ЧТОБЫ ОБНОВИЛОСЬ В ИНТЕРФЕЙСЕ:
                // Перезагружаем структуру разделов полностью
                var updatedSections = _db.Sections.Include(s => s.Catalogs).ToList();
                Sections.Clear();
                foreach (var s in updatedSections) Sections.Add(s);

                MessageBox.Show("Журнал добавлен и дерево обновлено!");
            }
        }
        private void LoadStructure()
        {
            var data = _db.Sections.Include(s => s.Catalogs).ToList();
            Sections.Clear();
            foreach (var s in data) Sections.Add(s);
        }
        private void DeleteSection()
        {
            if (SelectedSection == null) return;

            var result = MessageBox.Show($"Удалить раздел '{SelectedSection.SectionName}' и все его журналы?", "Подтверждение", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _db.Sections.Remove(SelectedSection);
                _db.SaveChanges();
                Sections.Remove(SelectedSection);
            }
        }

        // Команда удаления (блокировки)
        //public ICommand BlockUserCommand => new RelayCommand(obj =>
        //{
        //    if (obj is User user)
        //    {
        //        user.Active = user.Active == 1 ? 0 : 1;
        //        _db.SaveChanges();
        //        MessageBox.Show(user.Active == 1 ? "Разблокирован" : "Заблокирован");
        //    }
        //});

        private void BlockUser()
        {
            if (SelectedUser == null) return;

            // Переключаем статус: если был 1, станет 0, и наоборот
            SelectedUser.Active = (SelectedUser.Active == 1) ? 0 : 1;

            _db.SaveChanges(); // Сразу сохраняем в базу

            // Уведомляем UI, чтобы чекбокс обновился
            OnPropertyChanged(nameof(SelectedUser));
            LoadUsers();
        }

        private void MakeExecutor()
        {
            if (SelectedUserForExecutor == null) return;

            // Проверяем, нет ли его уже в исполнителях (по ФИО или по связи)
            if (_db.Executors.Any(e => e.FIO == SelectedUserForExecutor.Name))
            {
                MessageBox.Show("Этот человек уже есть в справочнике исполнителей!");
                return;
            }

            var newExecutor = new Executor
            {
                FIO = SelectedUserForExecutor.Name,
                Dol = "Сотрудник", // Можно добавить ввод должности
                Active = 1
            };

            _db.Executors.Add(newExecutor);
            _db.SaveChanges();

            // Связываем пользователя с записью исполнителя (по схеме)
            SelectedUserForExecutor.idExecutor = newExecutor.id;
            _db.SaveChanges();

            Executors.Add(newExecutor);
            MessageBox.Show($"{SelectedUserForExecutor.Name} теперь официально является исполнителем.");
        }

        private void DeleteExecutor()
        {
            if (SelectedExecutor == null) return;

            if (MessageBox.Show($"Удалить '{SelectedExecutor.FIO}'?", "Удаление", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            using (var db = new ArchiveDbContext())
            {
                // 1. Проверяем, есть ли реальные документы, связанные с ним
                bool hasOrders = db.OrderExecutors.Any(oe => oe.idExecutor == SelectedExecutor.id);

                if (hasOrders)
                {
                    // Если есть документы - удалять физически НЕЛЬЗЯ, только деактивация
                    var ex = db.Executors.Find(SelectedExecutor.id);
                    ex.Active = 0;
                    db.SaveChanges();
                    MessageBox.Show("Исполнитель связан с документами. Он деактивирован (Active = 0).");
                }
                else
                {
                    // 2. Если документов нет, но есть связь с Пользователем - убираем связь в таблице User
                    var linkedUsers = db.Users.Where(u => u.idExecutor == SelectedExecutor.id).ToList();
                    foreach (var u in linkedUsers)
                    {
                        u.idExecutor = null; // Разрываем связь, чтобы можно было удалить исполнителя
                    }
                    db.SaveChanges();

                    // 3. Теперь удаляем из справочника исполнителей физически
                    var ex = db.Executors.Find(SelectedExecutor.id);
                    if (ex != null)
                    {
                        db.Executors.Remove(ex);
                        db.SaveChanges();
                        Executors.Remove(SelectedExecutor);
                        MessageBox.Show("Исполнитель полностью удален.");
                    }
                }
            }
        }

        private void EditUser()
        {
            if (SelectedUser == null) return;

            // Открываем то же окно, что и для добавления
            var editWin = new Views.AddUserWindow();

            // Заполняем поля текущими данными
            editWin.NameBox.Text = SelectedUser.Name;
            editWin.LoginBox.Text = SelectedUser.Login;
            editWin.RoleCombo.SelectedValue = SelectedUser.idRights;
            editWin.Title = "Редактирование пользователя";

            if (editWin.ShowDialog() == true)
            {
                // Обновляем данные в выбранном объекте
                SelectedUser.Name = editWin.NameBox.Text;
                SelectedUser.Login = editWin.LoginBox.Text;
                SelectedUser.idRights = (int)editWin.RoleCombo.SelectedValue;

                // Если ввели новый пароль в окне - обновляем и его
                if (!string.IsNullOrEmpty(editWin.PassBox.Password))
                {
                    SelectedUser.Password = Infrastructure.PasswordHasher.GetMD5Hash(editWin.PassBox.Password);
                    SelectedUser.ChangePassword = editWin.ForceChangeCheck.IsChecked == true ? 1 : 0;
                }

                _db.SaveChanges();
                LoadUsers(); // Перегружаем список, чтобы увидеть изменения ролей
            }
        }

        private void SaveAll()
        {
            _db.SaveChanges();
            MessageBox.Show("Все изменения успешно сохранены!");
        }


        //public ICommand SaveChangesCommand => new RelayCommand(_ => {
        //    try
        //    {
        //        _db.SaveChanges();
        //        MessageBox.Show("Все изменения (статусы, ФИО, логины) сохранены в базе!");
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Ошибка сохранения: " + ex.Message);
        //    }
        //});
    }
}