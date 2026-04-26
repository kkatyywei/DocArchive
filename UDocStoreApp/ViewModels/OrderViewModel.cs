using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
        public ObservableCollection<ExecutorSelection> AllExecutorsSelection { get; set; } = new ObservableCollection<ExecutorSelection>();


        public RelayCommand SaveCommand { get; }
        public RelayCommand AddFileCommand { get; }
        public RelayCommand DownloadFileCommand { get; }
        public RelayCommand DeleteFileCommand { get; }

        public ICommand AddFromArchiveCommand => new RelayCommand(_ => {
            var archiveWin = new Views.FileArchiveWindow();
            if (archiveWin.ShowDialog() == true)
            {
                // Вызываем метод привязки выбранного файла
                LinkExistingFile(archiveWin.SelectedFile);
            }
        });


        private void LoadFiles()
        {
            var files = _db.OrderFiles
                .Where(of => of.idOrder == CurrentOrder.id)
                .Select(of => of.File)
                .ToList();

            Files.Clear();
            foreach (var f in files) Files.Add(f);
        }
        private void LoadRelatedData()
        {
            // Загружаем всех активных исполнителей
            var allExecs = _db.Executors.Where(e => e.Active == 1).ToList();

            // Загружаем тех, кто уже назначен на этот документ
            var currentExecIds = _db.OrderExecutors
                .Where(oe => oe.idOrder == CurrentOrder.id)
                .Select(oe => oe.idExecutor)
                .ToList();

            AllExecutorsSelection.Clear();
            foreach (var e in allExecs)
            {
                AllExecutorsSelection.Add(new ExecutorSelection
                {
                    id = e.id,
                    FIO = e.FIO,
                    IsSelected = currentExecIds.Contains(e.id)
                });
            }
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
                var oldLinks = _db.OrderExecutors.Where(oe => oe.idOrder == CurrentOrder.id);
                _db.OrderExecutors.RemoveRange(oldLinks);

                foreach (var selection in AllExecutorsSelection.Where(s => s.IsSelected))
                {
                    _db.OrderExecutors.Add(new OrderExecutor
                    {
                        idOrder = CurrentOrder.id,
                        idExecutor = selection.id
                    });
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
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Все файлы (*.*)|*.*"; // РАЗРЕШАЕМ ВСЁ: html, docx, exe, и т.д.

            if (openFileDialog.ShowDialog() == true)
            {
                var fileData = System.IO.File.ReadAllBytes(openFileDialog.FileName);
                var fileName = System.IO.Path.GetFileName(openFileDialog.FileName);

                // 1. Создаем запись самого файла
                var newFile = new FileEntity { Name = fileName, Data = fileData };
                _db.Files.Add(newFile);
                _db.SaveChanges(); // Получаем id файла

                // 2. Создаем связь с текущим документом
                var link = new OrderFile { idOrder = CurrentOrder.id, idFile = newFile.id };
                _db.OrderFiles.Add(link);
                _db.SaveChanges();

                Files.Add(newFile);
            }
        }

        private void LinkExistingFile(FileEntity existingFile)
        {
            if (existingFile == null) return;

            try
            {
                // 1. Проверяем, не привязан ли этот файл уже к ЭТОМУ документу
                bool alreadyLinked = _db.OrderFiles.Any(of =>
                    of.idOrder == CurrentOrder.id &&
                    of.idFile == existingFile.id);

                if (alreadyLinked)
                {
                    MessageBox.Show("Этот файл уже прикреплен к данному документу.");
                    return;
                }

                // 2. Создаем новую запись в связующей таблице
                var link = new OrderFile
                {
                    idOrder = CurrentOrder.id,
                    idFile = existingFile.id
                };

                _db.OrderFiles.Add(link);
                _db.SaveChanges();

                // 3. Обновляем коллекцию на экране
                Files.Add(existingFile);

                MessageBox.Show($"Файл '{existingFile.Name}' успешно привязан из архива!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при привязке файла: " + ex.Message);
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