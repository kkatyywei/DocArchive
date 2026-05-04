using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

        public Order CurrentOrder { get => _currentOrder; set => SetProperty(ref _currentOrder, value); }
        public bool IsReadOnly { get => _isReadOnly; set => SetProperty(ref _isReadOnly, value); }

        public ObservableCollection<FileEntity> Files { get; set; } = new ObservableCollection<FileEntity>();
        public ObservableCollection<ExecutorSelection> AllExecutorsSelection { get; set; } = new ObservableCollection<ExecutorSelection>();

        public RelayCommand SaveCommand { get; }
        public RelayCommand AddFileCommand { get; }
        public RelayCommand DownloadFileCommand { get; }
        public RelayCommand DeleteFileCommand { get; }
        public RelayCommand AddFromArchiveCommand { get; }

        public OrderViewModel(Order order)
        {
            CurrentOrder = order;

            // Команды
            SaveCommand = new RelayCommand(async _ => await Save(), _ => !IsReadOnly);
            AddFileCommand = new RelayCommand(async _ => await AddFile(), _ => !IsReadOnly);
            DownloadFileCommand = new RelayCommand(obj => DownloadFile(obj as FileEntity));
            DeleteFileCommand = new RelayCommand(async obj => await DeleteFile(obj as FileEntity), _ => !IsReadOnly);
            AddFromArchiveCommand = new RelayCommand(async _ => await OpenArchive());

            // Инициализация данных
            _ = LoadInitialData();
        }

        private async Task LoadInitialData()
        {
            using (var uow = new UnitOfWork())
            {
                List<FileEntity> filesList = new List<FileEntity>();
                try
                {
                    // Используем новый метод с Include
                    var fileLinks = await uow.OrderFiles.GetFilesByOrderIdAsync(CurrentOrder.id);

                    if (fileLinks != null && fileLinks.Any())
                    {
                        filesList = fileLinks
                            .Where(f => f.File != null) // Защита от битых связей
                            .Select(f => f.File)
                            .ToList();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка загрузки файлов: " + ex.Message);
                }
                // 2. Загрузка исполнителей
                var executors = await uow.Executors.FindAsync(e => e.Active == 1);
                var currentExecLinks = await uow.OrderExecutors.FindAsync(oe => oe.idOrder == CurrentOrder.id);
                var currentExecIds = currentExecLinks.Select(l => l.idExecutor).ToList();

                App.Current.Dispatcher.Invoke(() =>
                {
                    Files.Clear();
                    foreach (var f in filesList) Files.Add(f);

                    AllExecutorsSelection.Clear();
                    foreach (var e in executors)
                    {
                        AllExecutorsSelection.Add(new ExecutorSelection
                        {
                            id = e.id,
                            FIO = e.FIO,
                            IsSelected = currentExecIds.Contains(e.id)
                        });
                    }
                    //AllExecutorsSelection.Clear();
                    //foreach (var a in AllExecutorsSelection.Where(e => currentExecIds.Contains(e.id)))
                    //{
                    //    AllExecutorsSelection.Add(a);
                    //}
                });

                // 3. Проверка блокировки
                //await CheckLock(uow);
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

        private async Task Save()
        {
            using (var uow = new UnitOfWork())
            {
                await uow.BeginTransactionAsync();
                try
                {
                    // 1. Сохранение/Регистрация документа
                    if (CurrentOrder.id == 0)
                    {
                        var catalog = await uow.Catalogs.GetByIdAsync(CurrentOrder.idCatalog);
                        CurrentOrder.NumberReg = catalog.NumberNext++;
                        CurrentOrder.idUser = AuthService.CurrentUser.id;
                        CurrentOrder.RegDate = DateTime.Now;

                        uow.Catalogs.Update(catalog);
                        await uow.Orders.AddAsync(CurrentOrder);
                    }
                    else
                    {
                        uow.Orders.Update(CurrentOrder);
                    }
                    await uow.CompleteAsync(); // Чтобы получить ID для нового документа

                    // 2. Синхронизация исполнителей (Многие-ко-Многим)
                    await uow.OrderExecutors.RemoveLinksByOrderIdAsync(CurrentOrder.id);
                    foreach (var selection in AllExecutorsSelection.Where(s => s.IsSelected))
                    {
                        await uow.OrderExecutors.AddAsync(new OrderExecutor
                        {
                            idOrder = CurrentOrder.id,
                            idExecutor = selection.id
                        });
                    }

                    await uow.CompleteAsync();
                    await uow.CommitTransactionAsync();
                    MessageBox.Show($"Сохранено. Рег. №{CurrentOrder.NumberReg}");
                }
                catch (Exception ex)
                {
                    await uow.RollbackTransactionAsync();
                    MessageBox.Show("Ошибка сохранения: " + ex.Message);
                }
            }
        }

        private async Task AddFile()
        {
            var opd = new OpenFileDialog { Filter = "Все файлы (*.*)|*.*" };
            if (opd.ShowDialog() == true)
            {
                using (var uow = new UnitOfWork())
                {
                    var fileEntity = new FileEntity
                    {
                        Name = Path.GetFileName(opd.FileName),
                        Data = File.ReadAllBytes(opd.FileName)
                    };
                    await uow.Files.AddAsync(fileEntity);
                    await uow.CompleteAsync();

                    await uow.OrderFiles.AddAsync(new OrderFile
                    {
                        idOrder = CurrentOrder.id,
                        idFile = fileEntity.id
                    });
                    await uow.CompleteAsync();

                    Files.Add(fileEntity);
                }
            }
        }

        private async Task OpenArchive()
        {
            var win = new Views.FileArchiveWindow { Owner = Application.Current.MainWindow };
            if (win.ShowDialog() == true)
            {
                using (var uow = new UnitOfWork())
                {
                    var existingFile = win.SelectedFile;
                    // Проверка на дубликат связи
                    var exists = (await uow.OrderFiles.FindAsync(of => of.idOrder == CurrentOrder.id && of.idFile == existingFile.id)).Any();
                    if (exists) return;

                    await uow.OrderFiles.AddAsync(new OrderFile { idOrder = CurrentOrder.id, idFile = existingFile.id });
                    await uow.CompleteAsync();
                    Files.Add(existingFile);
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

        private void DownloadFile(FileEntity file)
        {
            if (file == null) return;
            var sfd = new SaveFileDialog { FileName = file.Name };
            if (sfd.ShowDialog() == true)
            {
                File.WriteAllBytes(sfd.FileName, file.Data);
            }
        }

        public async void ReleaseLock()
        {
            if (CurrentOrder.id == 0) return;
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
    }

}