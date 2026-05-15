using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Windows;
using UDocStoreApp.Data;
using UDocStoreApp.Infrastructure;
using UDocStoreApp.Models;
using UDocStoreApp.Repositories; 

namespace UDocStoreApp.Views
{
    public partial class AddUserWindow : Window
    {
        private ArchiveDbContext _db = new ArchiveDbContext();

        public AddUserWindow()
        {
            InitializeComponent();
            LoadRoles();
            //using (var db = new ArchiveDbContext())
            //{
            //    // Загружаем все доступные роли из базы
            //    RoleCombo.ItemsSource = db.Rights.ToList();
            //}
        }
        
        private async void LoadRoles()
        {
            using (var uow = new UnitOfWork())
            {
                var roles = await uow.Rights.GetAllAsync();
                RoleCombo.ItemsSource = roles.ToList();
            }
        }
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LoginBox.Text) || RoleCombo.SelectedValue == null || string.IsNullOrWhiteSpace(NameBox.Text) || string.IsNullOrWhiteSpace(DolBox.Text) || PassBox.Password.IsNullOrEmpty())
            {
                MessageBox.Show("Заполните все поля чтобы добавить новго пользователя!");
                return;
            }

            this.DialogResult = true;
        }
    }
}