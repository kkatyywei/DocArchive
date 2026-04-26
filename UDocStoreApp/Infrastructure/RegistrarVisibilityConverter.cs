using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UDocStoreApp.Infrastructure
{
    public class RegistrarVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value — это имя роли (AuthService.CurrentUser.Right.Name)
            if (value is string roleName)
            {
                if (roleName == "Регистратор" || roleName == "Администратор")
                    return Visibility.Visible;
            }

            // Для Наблюдателей и Исполнителей скрываем элемент
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}