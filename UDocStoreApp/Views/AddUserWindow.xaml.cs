using System.Linq;
using System.Windows;
using UDocStoreApp.Data;
using UDocStoreApp.Infrastructure;
using UDocStoreApp.Models;

namespace UDocStoreApp.Views
{
    public partial class AddUserWindow : Window
    {
        private ArchiveDbContext _db = new ArchiveDbContext();

        public AddUserWindow()
        {
            InitializeComponent();
            using (var db = new ArchiveDbContext())
            {
                // Загружаем все доступные роли из базы
                RoleCombo.ItemsSource = db.Rights.ToList();
            }
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(LoginBox.Text) || RoleCombo.SelectedValue == null) return;

            var newUser = new User
            {
                Name = NameBox.Text,
                Login = LoginBox.Text,
                Password = PasswordHasher.GetMD5Hash(PassBox.Password),
                idRights = (int)RoleCombo.SelectedValue,
                Active = 1,
                ChangePassword = ForceChangeCheck.IsChecked == true ? 1 : 0
            };

            _db.Users.Add(newUser);
            _db.SaveChanges();
            this.DialogResult = true;
        }
    }
}