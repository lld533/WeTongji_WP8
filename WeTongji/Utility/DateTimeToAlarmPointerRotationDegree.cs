using System;

namespace WeTongji.Utility
{
    public static class DateTimeToAlarmPointerRotationDegree
    {
        public static double GetHourPointerRotationDegree(this DateTime dt)
        {
            int h = dt.Hour >= 12 ? dt.Hour - 12 : dt.Hour;

            double result = h * 30 + ((dt.Minute & 1) == 1 ? ((dt.Minute >> 1) + 0.5f) : (dt.Minute >> 1));

            return result > 180 ? result - 360 : result;
        }

        public static double GetMinutePointerRotationDegree(this DateTime dt)
        {
            return dt.Minute > 30 ? (dt.Minute * 6 - 360) : (dt.Minute * 6);
        }
    }
}
