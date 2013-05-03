using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WeTongji.Converter
{
    public class StringIsIntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as String;
            int i;

            var result = int.TryParse(str, out i) ? Visibility.Visible : Visibility.Collapsed;
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
