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


        public ObservableCollection<User> Users { get; set; }
        public ObservableCollection<Section> Sections { get; set; } // Список разделов для админки
        public PassParam Policy { get; set; }

        public Section SelectedSection
        {
            get => _selectedSection;
            set => SetProperty(ref _selectedSection, value);
        }

        public AdminViewModel()
        {
            _db = new ArchiveDbContext();
            Users = new ObservableCollection<User>(_db.Users.Include(u => u.Right).ToList());
            Sections = new ObservableCollection<Section>(_db.Sections.Include(s => s.Catalogs).ToList());
            Policy = _db.PassParams.FirstOrDefault() ?? new PassParam { id = 1 };

            SavePolicyCommand = new RelayCommand(_ => SavePolicy());
            AddSectionCommand = new RelayCommand(_ => AddSection());
            DeleteSectionCommand = new RelayCommand(_ => DeleteSection(), _ => SelectedSection != null);

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
        public ICommand BlockUserCommand => new RelayCommand(obj =>
        {
            if (obj is User user)
            {
                user.Active = user.Active == 1 ? 0 : 1;
                _db.SaveChanges();
                MessageBox.Show(user.Active == 1 ? "Разблокирован" : "Заблокирован");
            }
        });
    }
}