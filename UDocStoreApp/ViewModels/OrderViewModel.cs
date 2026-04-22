using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using UDocStoreApp.Data;
using UDocStoreApp.Infrastructure;
using UDocStoreApp.Models;
using UDocStoreApp.Services;

namespace UDocStoreApp.ViewModels
{
    public class OrderViewModel : ViewModelBase
    {
        private readonly ArchiveDbContext _db;
        private Order _currentOrder;
        private bool _isReadOnly;

        public OrderViewModel(Order order)
        {
            _db = new ArchiveDbContext();
            CurrentOrder = order;

            // Загружаем файлы и исполнителей
            LoadRelatedData();

            // Проверяем блокировку
            CheckLock();

            SaveCommand = new RelayCommand(async _ => await Save(), _ => !IsReadOnly);
            AddFileCommand = new RelayCommand(_ => AddFile(), _ => !IsReadOnly);
            DownloadFileCommand = new RelayCommand(obj => DownloadFile(obj as FileEntity));
            DeleteFileCommand = new RelayCommand(obj => DeleteFile(obj as FileEntity), _ => !IsReadOnly);
        }

        public Order CurrentOrder
        {
            get => _currentOrder;
            set => SetProperty(ref _currentOrder, value);
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        public ObservableCollection<FileEntity> Files { get; set; } = new ObservableCollection<FileEntity>();
        public ObservableCollection<Executor> AllExecutors { get; set; } = new ObservableCollection<Executor>();

        public RelayCommand SaveCommand { get; }
        public RelayCommand AddFileCommand { get; }
        public RelayCommand DownloadFileCommand { get; }
        public RelayCommand DeleteFileCommand { get; }

        private void LoadRelatedData()
        {
            var files = _db.Files.Where(f => f.idOrder == CurrentOrder.id).ToList();
            Files.Clear();
            foreach (var f in files) Files.Add(f);

            var executors = _db.Executors.Where(e => e.Active == 1).ToList();
            AllExecutors.Clear();
            foreach (var e in executors) AllExecutors.Add(e);
        }

        private void CheckLock()
        {
            if (CurrentOrder.id == 0) return; // Новый документ не блокируем

            if (CurrentOrder.idUserOpen != null && CurrentOrder.idUserOpen != AuthService.CurrentUser.id)
            {
                IsReadOnly = true;
                MessageBox.Show($"Документ заблокирован пользователем ID: {CurrentOrder.idUserOpen}");
            }
            else
            {
                // Блокируем для себя
                CurrentOrder.idUserOpen = AuthService.CurrentUser.id;
                _db.Orders.Update(CurrentOrder);
                _db.SaveChanges();
            }
        }

        private async Task Save()
        {
            try
            {
                if (CurrentOrder.id == 0) // Если это новый документ
                {
                    // 1. Находим журнал, в который добавляем
                    var catalog = _db.Catalogs.Find(CurrentOrder.idCatalog);
                    if (catalog != null)
                    {
                        // 2. Присваиваем номер из журнала
                        CurrentOrder.NumberReg = catalog.NumberNext;
                        // 3. Увеличиваем счетчик в журнале для следующего документа
                        catalog.NumberNext++;
                    }

                    CurrentOrder.idUser = AuthService.CurrentUser.id;
                    CurrentOrder.RegDate = DateTime.Now;
                    _db.Orders.Add(CurrentOrder);
                }
                else
                {
                    _db.Orders.Update(CurrentOrder);
                }

                await _db.SaveChangesAsync();
                OnPropertyChanged(nameof(CurrentOrder)); // Обновляем UI, чтобы номер 0 сменился на реальный
                MessageBox.Show($"Документ зарегистрирован под № {CurrentOrder.NumberReg}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }

        private void AddFile()
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                var fileData = File.ReadAllBytes(openFileDialog.FileName);
                var newFile = new FileEntity
                {
                    idOrder = CurrentOrder.id,
                    Name = Path.GetFileName(openFileDialog.FileName),
                    Data = fileData
                };
                _db.Files.Add(newFile);
                _db.SaveChanges();
                Files.Add(newFile);
            }
        }

        private void DownloadFile(FileEntity file)
        {
            if (file == null) return;
            var saveFileDialog = new SaveFileDialog { FileName = file.Name };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, file.Data);
                MessageBox.Show("Файл сохранен!");
            }
        }

        private void DeleteFile(FileEntity file)
        {
            if (file == null) return;
            _db.Files.Remove(file);
            _db.SaveChanges();
            Files.Remove(file);
        }

        public void ReleaseLock()
        {
            if (CurrentOrder.id != 0 && CurrentOrder.idUserOpen == AuthService.CurrentUser.id)
            {
                var order = _db.Orders.Find(CurrentOrder.id);
                if (order != null)
                {
                    order.idUserOpen = null;
                    _db.SaveChanges();
                }
            }
        }
    }
}