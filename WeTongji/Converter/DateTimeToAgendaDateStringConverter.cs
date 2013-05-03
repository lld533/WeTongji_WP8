using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace WeTongji.Converter
{
    public class DateTimeToAgendaDateStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var date = (DateTime)value;

            //...Todo @_@ Localizable
            if (date == DateTime.Now.Date)
            {
                return StringLibrary.CalendarNode_Today;
            }

            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "zh")
            {
                return date.ToString("M月d日");
            }
            else
            {
                var sb = new StringBuilder();

                switch (date.Month)
                {
                    case 1:
                        sb.Append("January");
                        break;
                    case 2:
                        sb.Append("February");
                        break;
                    case 3:
                        sb.Append("March");
                        break;
                    case 4:
                        sb.Append("April");
                        break;
                    case 5:
                        sb.Append("May");
                        break;
                    case 6:
                        sb.Append("June");
                        break;
                    case 7:
                        sb.Append("July");
                        break;
                    case 8:
                        sb.Append("August");
                        break;
                    case 9:
                        sb.Append("September");
                        break;
                    case 10:
                        sb.Append("October");
                        break;
                    case 11:
                        sb.Append("November");
                        break;
                    case 12:
                        sb.Append("December");
                        break;
                    default:
                        break;
                }

                sb.AppendFormat(" {0}", date.Day);

                return sb.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}