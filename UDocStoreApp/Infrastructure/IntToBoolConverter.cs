using System;
using System.Globalization;
using System.Windows.Data;

namespace UDocStoreApp.Infrastructure
{
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int i && i == 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? 1 : 0; 
            }
            return 0;
        }
    }
}