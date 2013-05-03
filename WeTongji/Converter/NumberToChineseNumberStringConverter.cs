using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace WeTongji.Converter
{
    public class NumberToChineseNumberStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName != "zh")
                return value;

            int i;

            if (!int.TryParse(value.ToString(), out i))
                return value;

            if (i == 0)
                return "零";
            else if (i < 0)
                return "负" + Convert(-i, targetType, parameter, culture);

            var str = i.ToString();
            if (str.Length <= 4)
            {
                return InterpretCore(str);
            }
            else if (str.Length <= 8)
            {
                var baseOne = str.Substring(8 - str.Length);
                var baseTenThousand = str.Substring(0, str.Length - 4);

                return InterpretCore(baseTenThousand) + "万" + InterpretCore(baseOne);
            }
            else
            {
                var baseOne = str.Substring(str.Length - 4);
                var baseTenThousand = str.Substring(str.Length - 8, 4);
                var baseHundredMillion = str.Substring(0, str.Length - 8);

                var baseOneStr = InterpretCore(baseOne);
                var baseTenThousandStr = InterpretCore(baseTenThousand);
                var baseHundredMillionStr = InterpretCore(baseHundredMillion);

                var b = false;
                var sb = new StringBuilder(baseHundredMillionStr + "亿");

                if (String.IsNullOrEmpty(baseTenThousandStr))
                {
                    sb.Append("零");
                    b = true;
                }
                else
                {
                    sb.Append(baseTenThousandStr + "万");
                }

                if (String.IsNullOrEmpty(baseOneStr))
                {
                    if (b)
                    {
                        return sb.ToString(0, sb.Length - 1);
                    }
                    else
                        sb.Append("零");
                }
                else
                {
                    if (b)
                        sb.Append(baseOneStr.TrimStart('零'));
                    else
                        sb.Append(baseOneStr);
                }

                return sb.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private String InterpretCore(String arr)
        {
            int k;
            if (Int32.TryParse(arr, out k))
            {
                if (k == 0)
                    return String.Empty;

                var sb = new StringBuilder();
                Stack<char> stack = new Stack<char>();

                var s = new char[] { ' ', '十', '百', '千' };
                var material = arr.Reverse();
                bool b = true;

                for (int i = 0; i < arr.Length; ++i)
                {
                    var c = material.ElementAt(i);
                    switch (c)
                    {
                        case '0':
                            if (stack.Count > 0 && b)
                            {
                                stack.Push('零');
                                b = false;
                            }
                            break;
                        case '1':
                            if (i > 0)
                                stack.Push(s[i]);
                            stack.Push('一');

                            break;

                        case '2':
                            if (i > 0)
                                stack.Push(s[i]);
                            stack.Push('二');
                            break;
                        case '3':
                            if (i > 0)
                                stack.Push(s[i]);
                            stack.Push('三');

                            break;
                        case '4':
                            if (i > 0)
                                stack.Push(s[i]);
                            stack.Push('四');

                            break;
                        case '5':
                            if (i > 0)
                                stack.Push(s[i]);
                            stack.Push('五');

                            break;
                        case '6':
                            if (i > 0)
                                stack.Push(s[i]);
                            stack.Push('六');

                            break;
                        case '7':
                            if (i > 0)
                                stack.Push(s[i]);
                            stack.Push('七');

                            break;
                        case '8':
                            if (i > 0)
                                stack.Push(s[i]);
                            stack.Push('八');

                            break;
                        case '9':
                            if (i > 0)
                                stack.Push(s[i]);
                            stack.Push('九');

                            break;
                        default:
                            break;
                    }
                }

                int count = stack.Count;
                for (int i = 0; i < count; ++i)
                    sb.Append(stack.Pop());


                var str = sb.ToString();
                str = str.Replace("一十", "十");
                return str;
            }

            return String.Empty;
        }
    }
}
