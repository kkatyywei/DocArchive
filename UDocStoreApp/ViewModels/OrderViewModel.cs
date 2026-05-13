using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using UDocStoreApp.Infrastructure;
using UDocStoreApp.Models;
using UDocStoreApp.Repositories;
using UDocStoreApp.Services;

namespace UDocStoreApp.ViewModels
{
    public class OrderViewModel : ViewModelBase
    {
        private Order _currentOrder;
        private bool _isReadOnly;
        private FileEntity _selectedFile;
        private List<FileEntity> _filesToDelete = new List<FileEntity>();
        public bool IsDeleted => CurrentOrder != null && CurrentOrder.id != 0 && CurrentOrder.isDel == 1;

        public bool CanEditDocument
        {
            get
            {
                var currentUser = AuthService.CurrentUser;
                var role = currentUser?.Right?.Name?.Trim();
                if (role == "Администратор")
                {
                    return !IsDeleted;
                }
                if (role == "Регистратор")
                {
                    if (IsDeleted) return false; // Удаленные не правим

                    // Если документ новый — править можно (он станет его автором)
                    if (CurrentOrder.id == 0) return true;

                    // Если документ существующий — сверяем ID пользователя с ID автора
                    return CurrentOrder.idUser == currentUser.id;
                }
                return false;
            }
        }
        public Visibility AdminPanelVisibility => CanEditDocument ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ReadPanelVisibility => CanEditDocument ? Visibility.Collapsed : Visibility.Visible;
        public Visibility SaveBtnVisibility => (CanEditDocument && !IsDeleted) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility RestoreBtnVisibility =>
      (IsDeleted && AuthService.CurrentUser.Right.Name == "Администратор")
      ? Visibility.Visible : Visibility.Collapsed;

        public Order CurrentOrder { get => _currentOrder; set => SetProperty(ref _currentOrder, value); }
        public bool IsReadOnly { get => _isReadOnly; set => SetProperty(ref _isReadOnly, value); }

        public ObservableCollection<FileEntity> Files { get; set; } = new ObservableCollection<FileEntity>();
        public ObservableCollection<ExecutorSelection> AllExecutorsSelection { get; set; } = new ObservableCollection<ExecutorSelection>();
        public ObservableCollection<Executor> AssignedExecutors { get; set; } = new ObservableCollection<Executor>();

        public RelayCommand SaveCommand { get; }
        public RelayCommand AddFileCommand { get; }
        public ICommand DownloadSelectedFileCommand { get; }
        public ICommand DeleteFileCommand { get; }
        public RelayCommand AddFromArchiveCommand { get; }
        public ICommand RestoreCommand { get; }

        public FileEntity SelectedFile
        {
            get => _selectedFile;
            set
            {
                SetProperty(ref _selectedFile, value);
                // Это заставляет кнопки перепроверить свою доступность (CanExecute)
                CommandManager.InvalidateRequerySuggested();

            }
        }

        public OrderViewModel(Order order)
        {
            CurrentOrder = order;

            // Команды
            SaveCommand = new RelayCommand(async _ => await Save(), _ => !IsReadOnly);
            AddFileCommand = new RelayCommand(async _ => await AddFile(), _ => !IsReadOnly);
            DeleteFileCommand = new RelayCommand(_ => ExecuteDeleteFile(), _ => SelectedFile != null && !IsReadOnly);
            DownloadSelectedFileCommand = new RelayCommand(_ => DownloadFile(SelectedFile), _ => SelectedFile != null);
            RestoreCommand = new RelayCommand(async _ => await ExecuteRestore());

            RefreshAllProperties();

            // Инициализация данных
            _ = LoadInitialData();
        }

        private async Task LoadInitialData()
        {
            using (var uow = new UnitOfWork())
            {
                // --- БЛОК ФАЙЛОВ (Логика сохранена полностью) ---
                var fileLinks = await uow.OrderFiles.GetFilesByOrderIdAsync(CurrentOrder.id);
                var filesList = fileLinks.Select(f => f.File).Where(f => f != null).ToList();

                // --- БЛОК ИСПОЛНИТЕЛЕЙ (Логика расширена) ---
                // Загружаем всех активных из справочника
                var allExecutorsFromDb = await uow.Executors.FindAsync(e => e.Active == 1);

                // Загружаем связи этого конкретного документа
                var currentExecLinks = await uow.OrderExecutors.FindAsync(oe => oe.idOrder == CurrentOrder.id);
                var selectedExecIds = currentExecLinks.Select(l => l.idExecutor).ToList();

                // --- СИНХРОНИЗАЦИЯ С ИНТЕРФЕЙСОМ ---
                App.Current.Dispatcher.Invoke(() =>
                {
                    // 1. Файлы
                    Files.Clear();
                    foreach (var f in filesList) Files.Add(f);

                    // 2. Список для АДМИНА/РЕГИСТРАТОРА (с чекбоксами)
                    AllExecutorsSelection.Clear();

                    foreach (var e in allExecutorsFromDb)
                    {
                        AllExecutorsSelection.Add(new ExecutorSelection
                        {
                            id = e.id,
                            FIO = e.FIO,
                            IsSelected = selectedExecIds.Contains(e.id)
                        });
                    }

                    // 3. Список для ИСПОЛНИТЕЛЯ/НАБЛЮДАТЕЛЯ (только текст назначенных)
                    // Фильтруем загруженных исполнителей, оставляя только тех, чьи ID в связях документа
                    AssignedExecutors.Clear();
                    var onlyAssigned = allExecutorsFromDb
                        .Where(e => selectedExecIds.Contains(e.id))
                        .ToList();

                    foreach (var a in onlyAssigned)
                    {
                        AssignedExecutors.Add(a);
                    }


                    // 4. Пересчитываем видимость кнопок и полей
                    RefreshAllProperties();
                });

                //// Блокировка (если не новый)
                //if (CurrentOrder.id != 0) await CheckLock(uow);
            }
        }

        //private async Task CheckLock(IUnitOfWork uow)

        //{
        //    if (CurrentOrder.id == 0) return;

        //    var order = await uow.Orders.GetByIdAsync(CurrentOrder.id);
        //    if (order.idUserOpen != null && order.idUserOpen != AuthService.CurrentUser.id)
        //    {
        //        IsReadOnly = true;
        //        MessageBox.Show($"Документ заблокирован пользователем ID: {order.idUserOpen}");
        //    }
        //    else
        //    {
        //        order.idUserOpen = AuthService.CurrentUser.id;
        //        uow.Orders.Update(order);
        //        await uow.CompleteAsync();
        //    }
        //}


        private async Task AddFile()
        {
            var opd = new OpenFileDialog { Filter = "Все файлы (*.*)|*.*" };
            if (opd.ShowDialog() != true) return;

            byte[] fileData = File.ReadAllBytes(opd.FileName);
            string fileName = Path.GetFileName(opd.FileName);

            // Считаем хеш заранее
            string fileHash;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                fileHash = BitConverter.ToString(sha256.ComputeHash(fileData)).Replace("-", "").ToLower();
            }

            // Просто добавляем в коллекцию на экране (id = 0, значит файл новый)
            Files.Add(new FileEntity
            {
                Name = fileName,
                Data = fileData,
                FileHash = fileHash
            });
        }

        // 2. УДАЛЕНИЕ ИЗ СПИСКА
        private void ExecuteDeleteFile()
        {
            if (SelectedFile == null) return;

            // Если у файла есть ID > 0, значит он уже в базе, запоминаем его для удаления связи при сохранении
            if (SelectedFile.id > 0)
            {
                _filesToDelete.Add(SelectedFile);
            }

            Files.Remove(SelectedFile);
            SelectedFile = null;
        }

        // 3. ОБНОВЛЕННЫЙ МЕТОД СОХРАНЕНИЯ (ЕДИНАЯ ТРАНЗАКЦИЯ)
        private async Task Save()
        {
            using (var uow = new UnitOfWork())
            {
                await uow.BeginTransactionAsync();
                try
                {
                    // А. Сохраняем документ
                    if (CurrentOrder.id == 0)
                    {
                        var catalog = await uow.Catalogs.GetByIdAsync(CurrentOrder.idCatalog);
                        CurrentOrder.NumberReg = catalog.NumberNext++;
                        uow.Catalogs.Update(catalog);
                        await uow.Orders.AddAsync(CurrentOrder);
                    }
                    else
                    {
                        uow.Orders.Update(CurrentOrder);
                    }
                    await uow.CompleteAsync(); // Получаем ID документа

                    // Б. Обрабатываем УДАЛЕНИЕ файлов (разрыв связей)
                    foreach (var file in _filesToDelete)
                    {
                        var links = await uow.OrderFiles.FindAsync(of => of.idOrder == CurrentOrder.id && of.idFile == file.id);
                        foreach (var link in links) uow.OrderFiles.Remove(link);
                    }

                    // В. Обрабатываем ДОБАВЛЕНИЕ новых файлов (те, у которых id == 0)
                    foreach (var file in Files.Where(f => f.id == 0))
                    {
                        // Ищем дубликат контента в БД
                        var existing = await uow.Files.GetByHashAsync(file.FileHash);
                        int finalFileId;

                        if (existing == null)
                        {
                            await uow.Files.AddAsync(file);
                            await uow.CompleteAsync();
                            finalFileId = file.id;
                        }
                        else
                        {
                            finalFileId = existing.id;
                        }

                        // Создаем связь
                        await uow.OrderFiles.AddAsync(new OrderFile { idOrder = CurrentOrder.id, idFile = finalFileId });
                    }
                    // Г. СОХРАНЕНИЕ ИСПОЛНИТЕЛЕЙ (НОВАЯ ЛОГИКА)
                    // Получаем выбранных исполнителей из интерфейса
                    var selectedExecutorIds = AllExecutorsSelection
                        .Where(e => e.IsSelected)
                        .Select(e => e.id)
                        .ToList();

                    // Получаем текущие связи из БД
                    var currentLinks = await uow.OrderExecutors
                        .FindAsync(oe => oe.idOrder == CurrentOrder.id);
                    var currentExecutorIds = currentLinks.Select(oe => oe.idExecutor).ToList();

                    // Определяем, какие нужно добавить, а какие удалить
                    var toAdd = selectedExecutorIds.Except(currentExecutorIds).ToList();
                    var toRemove = currentExecutorIds.Except(selectedExecutorIds).ToList();

                    // Добавляем новые связи
                    foreach (var execId in toAdd)
                    {
                        await uow.OrderExecutors.AddAsync(new OrderExecutor
                        {
                            idOrder = CurrentOrder.id,
                            idExecutor = execId
                        });
                    }

                    // Удаляем отсутствующие связи
                    foreach (var execId in toRemove)
                    {
                        var linkToRemove = currentLinks.First(oe => oe.idExecutor == execId);
                        uow.OrderExecutors.Remove(linkToRemove);
                    }
                    await uow.CompleteAsync();
                    await uow.CommitTransactionAsync();

                    _filesToDelete.Clear();
                    MessageBox.Show("Все изменения сохранены!");
                    CloseWindow();

                }
                catch (Exception ex)
                {
                    await uow.RollbackTransactionAsync();
                    MessageBox.Show("Ошибка сохранения: " + ex.Message);
                }
            }
        }
        private void CloseWindow()
        {
            // Ищем окно, DataContext которого является текущая ViewModel
            foreach (Window win in Application.Current.Windows)
            {
                if (win.DataContext == this)
                {
                    win.Close();
                    break;
                }
            }
        }
       
        private async Task DeleteFile(FileEntity file)
        {
            if (file == null) return;
            using (var uow = new UnitOfWork())
            {
                var links = await uow.OrderFiles.FindAsync(of => of.idOrder == CurrentOrder.id && of.idFile == file.id);
                foreach (var l in links) uow.OrderFiles.Remove(l);
                await uow.CompleteAsync();
                Files.Remove(file);
            }
        }



        public async void ReleaseLock()
        {
            // Если документ новый, в базе нет записи о блокировке - ничего не делаем
            if (CurrentOrder == null || CurrentOrder.id == 0) return;

            using (var uow = new UnitOfWork())
            {
                var order = await uow.Orders.GetByIdAsync(CurrentOrder.id);
                if (order != null && order.idUserOpen == AuthService.CurrentUser.id)
                {
                    order.idUserOpen = null;
                    uow.Orders.Update(order);
                    await uow.CompleteAsync();
                }
            }
        }

        private void DownloadFile(FileEntity file)
        {
            if (file == null)
            {
                MessageBox.Show("Сначала выделите файл в списке выше!");
                return;
            }

            var sfd = new SaveFileDialog
            {
                FileName = file.Name,
                Filter = "Все файлы (*.*)|*.*"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllBytes(sfd.FileName, file.Data);
                    MessageBox.Show("Файл успешно выгружен на диск.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }
        private void ExecuteDownload()
        {
            if (SelectedFile == null) return;
            DownloadFile(SelectedFile);
        }

        private async Task ExecuteRestore()
        {
            var result = MessageBox.Show("Восстановить этот документ из корзины?", "Восстановление",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                using (var uow = new UnitOfWork())
                {
                    var order = await uow.Orders.GetByIdAsync(CurrentOrder.id);
                    if (order != null)
                    {
                        order.isDel = 0; // Снимаем флаг удаления
                        uow.Orders.Update(order);
                        await uow.CompleteAsync();

                        CurrentOrder.isDel = 0; // Обновляем локальный объект

                        RefreshAllProperties();

                        MessageBox.Show("Документ успешно восстановлен!");
                    }
                }
            }
        }

        public void RefreshAllProperties()
        {
            OnPropertyChanged(nameof(IsDeleted));
            OnPropertyChanged(nameof(CanEditDocument));
            OnPropertyChanged(nameof(CanEditDocument));
            OnPropertyChanged(nameof(AdminPanelVisibility));
            OnPropertyChanged(nameof(ReadPanelVisibility));
            OnPropertyChanged(nameof(SaveBtnVisibility));
            OnPropertyChanged(nameof(RestoreBtnVisibility));
        }
    }

}