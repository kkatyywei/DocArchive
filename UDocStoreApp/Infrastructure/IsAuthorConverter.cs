using System;
using System.Globalization;
using System.Windows.Data;
using UDocStoreApp.Services;

namespace UDocStoreApp.Infrastructure
{
    public class IsAuthorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int authorId && AuthService.CurrentUser != null)
            {
                return authorId == AuthService.CurrentUser.id;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}