using System;
using System.Globalization;
using System.Windows.Data;

namespace UDocStoreApp.Infrastructure
{
    public class IntToBoolConverter : IValueConverter
    {
        // Из базы (int) в интерфейс (bool)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int i && i == 1;
        }

        // Из интерфейса (bool) в базу (int)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? 1 : 0;
        }
    }
}