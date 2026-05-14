using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace UDocStoreApp.Infrastructure
{
    public class InverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool val = (value is bool b) ? b : false;
            return val ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
