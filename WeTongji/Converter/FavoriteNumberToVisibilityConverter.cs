using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WeTongji.Converter
{
    public class FavoriteNumberToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int i = 0;

            int.TryParse(value.ToString(), out i);

            return i > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}