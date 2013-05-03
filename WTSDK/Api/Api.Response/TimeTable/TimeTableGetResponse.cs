using System;

namespace WeTongji.Api.Response
{
    public class TimeTableGetResponse : WTResponse
    {
        public TimeTableGetResponse() 
        {
            Courses = null;
            SchoolYearStartAt = DateTime.MinValue;
            SchoolYearWeekCount = -1;
            SchoolYearCourseWeekCount = -1;
        }

        public WeTongji.Api.Domain.Course[] Courses { get; set; }
        public DateTime SchoolYearStartAt { get; set; }
        public int SchoolYearWeekCount { get; set; }
        public int SchoolYearCourseWeekCount { get; set; }
    }
}
