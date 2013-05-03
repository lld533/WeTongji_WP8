using System;

namespace WeTongji.Api.Response
{
    public class ScheduleGetResponse : WTResponse
    {
        public ScheduleGetResponse() 
        {
            Exams = null;
            Activities = null;
            CourseInstances = null;
        }

        public WeTongji.Api.Domain.Exam[] Exams { get; set; }
        public WeTongji.Api.Domain.Activity[] Activities { get; set; }
        public WeTongji.Api.Domain.CourseInstance[] CourseInstances { get; set; }
    }
}
