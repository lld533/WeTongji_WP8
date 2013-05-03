using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Utility
{
    public static class DateTimeToChineseDate
    {
        public static String GetChineseDate(this DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    return "周日";
                case DayOfWeek.Monday:
                    return "周一";
                case DayOfWeek.Tuesday:
                    return "周二";
                case DayOfWeek.Wednesday:
                    return "周三";
                case DayOfWeek.Thursday:
                    return "周四";
                case DayOfWeek.Friday:
                    return "周五";
                case DayOfWeek.Saturday:
                    return "周六";
                default:
                    throw new NotSupportedException("date");
            }
        }
    }
}
