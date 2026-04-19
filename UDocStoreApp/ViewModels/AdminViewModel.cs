using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using UDocStoreApp.Data;
using UDocStoreApp.Models;
using Microsoft.EntityFrameworkCore;

namespace UDocStoreApp.ViewModels
{
    public class AdminViewModel : ViewModelBase
    {
        private readonly ArchiveDbContext _db;
        public ObservableCollection<User> Users { get; set; }
        public PassParam Policy { get; set; }

        public AdminViewModel()
        {
            _db = new ArchiveDbContext();
            Users = new ObservableCollection<User>(_db.Users.Include(u => u.Right).ToList());
            Policy = _db.PassParams.FirstOrDefault() ?? new PassParam { id = 1 };

            SavePolicyCommand = new Infrastructure.RelayCommand(_ => SavePolicy());
        }

        public Infrastructure.RelayCommand SavePolicyCommand { get; }

        private void SavePolicy()
        {
            if (!_db.PassParams.Any()) _db.PassParams.Add(Policy);
            else _db.PassParams.Update(Policy);

            _db.SaveChanges();
            MessageBox.Show("Политика паролей обновлена!");
        }
    }
}