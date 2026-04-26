using System.Linq;
using System.Windows;
using UDocStoreApp.Data;
using UDocStoreApp.Models;

namespace UDocStoreApp.Views
{
    public partial class FileArchiveWindow : Window
    {
        // Это свойство обязательно должно быть тут!
        public FileEntity SelectedFile { get; set; }

        private ArchiveDbContext _db = new ArchiveDbContext();

        public FileArchiveWindow()
        {
            InitializeComponent();
            // Загружаем список всех файлов
            FilesGrid.ItemsSource = _db.Files.ToList();
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            SelectedFile = FilesGrid.SelectedItem as FileEntity;
            if (SelectedFile != null)
            {
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("Выберите файл из списка!");
            }
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string search = (sender as System.Windows.Controls.TextBox).Text;
            FilesGrid.ItemsSource = _db.Files
                .Where(f => f.Name.Contains(search))
                .ToList();
        }
    }
}