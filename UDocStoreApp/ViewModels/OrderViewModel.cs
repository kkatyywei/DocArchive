using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.IdentityModel.Tokens;
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
                    if (IsDeleted) return false; 

                    if (CurrentOrder.id == 0) return true;

                    return CurrentOrder.idUser == currentUser.id;
                }
                return false;
            }
        }
        public Visibility AdminPanelVisibility => CanEditDocument ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ReadPanelVisibility => CanEditDocument ? Visibility.Collapsed : Visibility.Visible;
        public Visibility SaveBtnVisibility => (CanEditDocument && !IsDeleted) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility RestoreBtnVisibility => (IsDeleted && AuthService.CurrentUser.Right.Name == "Администратор")
                      ? Visibility.Visible : Visibility.Collapsed;

        public Order CurrentOrder { get => _currentOrder; set => SetProperty(ref _currentOrder, value); }
        public bool IsReadOnly { get => _isReadOnly; set => SetProperty(ref _isReadOnly, value); }

        public ObservableCollection<FileEntity> Files { get; set; } = new ObservableCollection<FileEntity>();
        public ObservableCollection<ExecutorSelection> AllExecutorsSelection { get; set; } = new ObservableCollection<ExecutorSelection>();
        public ObservableCollection<Executor> AssignedExecutors { get; set; } = new ObservableCollection<Executor>();

        public RelayCommand SaveCommand { get; }
        public RelayCommand AddFileCommand { get; }
        public RelayCommand AddFromArchiveCommand { get; }
        public ICommand DownloadSelectedFileCommand { get; }
        public ICommand DeleteFileCommand { get; }
        public ICommand RestoreCommand { get; }

        public FileEntity SelectedFile
        {
            get => _selectedFile;
            set
            {
                SetProperty(ref _selectedFile, value);
                CommandManager.InvalidateRequerySuggested();

            }
        }

        public OrderViewModel(Order order)
        {
            CurrentOrder = order;

            SaveCommand = new RelayCommand(async _ => await Save(), _ => !IsReadOnly);
            AddFileCommand = new RelayCommand(async _ => await AddFile(), _ => !IsReadOnly);
            DeleteFileCommand = new RelayCommand(_ => ExecuteDeleteFile(), _ => SelectedFile != null && !IsReadOnly);
            DownloadSelectedFileCommand = new RelayCommand(_ => DownloadFile(SelectedFile), _ => SelectedFile != null);
            RestoreCommand = new RelayCommand(async _ => await ExecuteRestore());

            RefreshAllProperties();

            _ = LoadInitialData();
        }

        private async Task LoadInitialData()
        {
            using (var uow = new UnitOfWork())
            {
                var fileLinks = await uow.OrderFiles.GetFilesByOrderIdAsync(CurrentOrder.id);
                var filesList = fileLinks.Select(f => f.File).Where(f => f != null).ToList();

                var allExecutorsFromDb = await uow.Executors.FindAsync(e => e.Active == 1);

                var currentExecLinks = await uow.OrderExecutors.FindAsync(oe => oe.idOrder == CurrentOrder.id);
                var selectedExecIds = currentExecLinks.Select(l => l.idExecutor).ToList();


                App.Current.Dispatcher.Invoke(() =>
                {
                    Files.Clear();
                    foreach (var f in filesList) Files.Add(f);
                    // for admin/reg
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

                    // for exec/nabl
                    AssignedExecutors.Clear();
                    var onlyAssigned = allExecutorsFromDb
                        .Where(e => selectedExecIds.Contains(e.id))
                        .ToList();

                    foreach (var a in onlyAssigned)
                    {
                        AssignedExecutors.Add(a);
                    }


                    RefreshAllProperties();
                });
            }
        }

        private async Task AddFile()
        {
            var opd = new OpenFileDialog { Filter = "Все файлы (*.*)|*.*" };
            if (opd.ShowDialog() != true) return;

            byte[] fileData = File.ReadAllBytes(opd.FileName);
            string fileName = Path.GetFileName(opd.FileName);

            string fileHash;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                fileHash = BitConverter.ToString(sha256.ComputeHash(fileData)).Replace("-", "").ToLower();
            }

            Files.Add(new FileEntity
            {
                Name = fileName,
                Data = fileData,
                FileHash = fileHash
            });
        }

        private void ExecuteDeleteFile()
        {
            if (SelectedFile == null) return;

            if (SelectedFile.id > 0)
            {
                _filesToDelete.Add(SelectedFile);
            }

            Files.Remove(SelectedFile);
            SelectedFile = null;
        }

      
        private async Task Save()
        {
            var selectedExecutorIds = AllExecutorsSelection
                .Where(e => e.IsSelected)
                .Select(e => e.id)
                .ToList();
            using (var uow = new UnitOfWork())
            {
                
                if (CurrentOrder.Text.IsNullOrEmpty() || CurrentOrder.NumberOrder.IsNullOrEmpty() || selectedExecutorIds == null)
                {
                    MessageBox.Show("Ошибка сохранения.");
                    return;
                }
                if (Files.Count() == 0)
                {
                    MessageBox.Show("Нет файлов для сохранения.");
                    return;
                }
            }
            using (var uow = new UnitOfWork())
            {

                await uow.BeginTransactionAsync();
                try
                {
                    // save doc
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
                    await uow.CompleteAsync(); // get doc id

                    // delete files
                    foreach (var file in _filesToDelete)
                    {
                        var links = await uow.OrderFiles.FindAsync(of => of.idOrder == CurrentOrder.id && of.idFile == file.id);
                        foreach (var link in links) uow.OrderFiles.Remove(link);
                    }
                    // add files
                    foreach (var file in Files.Where(f => f.id == 0))
                    {
                        // find dupl 
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

                        await uow.OrderFiles.AddAsync(new OrderFile { idOrder = CurrentOrder.id, idFile = finalFileId });
                    }
                    
                    //save executors
                    var currentLinks = await uow.OrderExecutors.FindAsync(oe => oe.idOrder == CurrentOrder.id);
                    var currentExecutorIds = currentLinks.Select(oe => oe.idExecutor).ToList();

                    var toAdd = selectedExecutorIds.Except(currentExecutorIds).ToList();
                    var toRemove = currentExecutorIds.Except(selectedExecutorIds).ToList();

                    foreach (var execId in toAdd)
                    {
                        await uow.OrderExecutors.AddAsync(new OrderExecutor
                        {
                            idOrder = CurrentOrder.id,
                            idExecutor = execId
                        });
                    }

                    // remove missing links
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
                        order.isDel = 0; 
                        uow.Orders.Update(order);
                        await uow.CompleteAsync();

                        CurrentOrder.isDel = 0; 

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