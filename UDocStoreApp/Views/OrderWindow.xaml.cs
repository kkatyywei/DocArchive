using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UDocStoreApp.Views
{
    /// <summary>
    /// Логика взаимодействия для OrderWindow.xaml
    /// </summary>
    public partial class OrderWindow : Window
    {
        public OrderWindow()
        {
            InitializeComponent();
        }

        private void DatePicker_CalendarOpened(object sender, RoutedEventArgs e)
        {
            MainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        private void DatePicker_CalendarClosed(object sender, RoutedEventArgs e)
        {
            MainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }
    }
}
