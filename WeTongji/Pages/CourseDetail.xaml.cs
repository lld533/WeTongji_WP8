using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Navigation;
using System.Text.RegularExpressions;
using System.Text;
using WeTongji.DataBase;
using WeTongji.Business;
using WeTongji.Api.Domain;
using System.Threading;
using WeTongji.Pages;
using System.Reflection;
using System.Globalization;

namespace WeTongji
{
    public partial class CourseDetail : PhoneApplicationPage
    {
        public CourseDetail()
        {
            InitializeComponent();
        }

        /// <remarks>
        /// [View] e.g. /Pages/CourseDetail.xaml?q={Id}&d={Date}
        /// [Id] a guid string, course id or exam id
        /// [Date] JUST for course, Date component
        /// </remarks>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var uri = e.Uri.ToString();
            var strTrimmed = uri.TrimStart("/Pages/CourseDetail.xaml".ToCharArray());
            if (!String.IsNullOrEmpty(strTrimmed))
            {
                strTrimmed = strTrimmed.TrimStart("?".ToCharArray());

                var thread = new Thread(new ParameterizedThreadStart(LoadData))
                {
                    IsBackground = true,
                    Name = "LoadData"
                };

                var strs = strTrimmed.Split("&".ToArray(), StringSplitOptions.RemoveEmptyEntries);

                if (strs.Count() == 1)
                {
                    Pivot_Core.SelectedIndex = 1;
                }

                ProgressBarPopup.Instance.Open();
                thread.Start(strTrimmed);
            }
        }

        public class CourseNode : CourseExt
        {
            private DateTime date;

            public CourseNode(CourseExt course, DateTime date)
            {
                base.SetObject(course.GetObject());
                this.date = date.Date;
            }

            public CourseNode(ExamExt exam)
            {
                this.NO = exam.NO;
                this.Point = exam.Point;
                this.Required = exam.Required;
                this.Teacher = exam.Teacher;
                this.Hours = exam.Hours;
                this.UID = exam.UID;
                this.SemesterGuid = exam.SemesterGuid;
            }

            public String DisplayBeginTimeAndEndTime
            {
                get
                {
                    if (date == DateTime.MinValue)
                        return String.Empty;

                    var sb = new StringBuilder(date.ToString("yyyy/MM/dd("));
                    sb.Append(StringLibrary.ResourceManager.GetString("DayOfWeekAbbr_" + date.DayOfWeek.ToString()));
                    
                    sb.AppendFormat(") {0}~{1}", date.GetCourseStartTime(base.SectionStart).ToString("HH:mm"), date.GetCourseEndTime(base.SectionEnd).ToString("HH:mm"));


                    return sb.ToString();
                }
            }
        }

        /// <summary>
        /// Load data
        /// </summary>
        /// <param name="param">trimmed query string</param>
        private void LoadData(Object param)
        {
            var strTrimmed = (String)param;

            string[] strs = strTrimmed.Split("&".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            //...Exam
            if (strs.Count() == 1)
            {
                var id = strs[0].TrimStart("q=".ToCharArray());

                ExamExt examExt = null;

                using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                {
                    examExt = db.Exams.Where((exam) => exam.Id == id).SingleOrDefault();
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    if (examExt != null)
                    {
                        PivotItem_Exam.DataContext = examExt;
                        TextBlock_PageTitle.DataContext = examExt;
                        TextBlock_QueryExam.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        TextBlock_NoExamHint.Visibility = Visibility.Visible;
                        TextBlock_NoCourseHint.Visibility = Visibility.Visible;
                        ProgressBarPopup.Instance.Close();
                    }
                });

                //...Get corresponding course
                if (examExt != null)
                {
                    CourseExt[] courses = null;

                    using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                    {
                        courses = db.Courses.Where((course) => course.SemesterGuid == examExt.SemesterGuid && course.NO == examExt.NO && course.Teacher == examExt.Teacher).ToArray();
                    }

                    if (courses != null && courses.Count() > 0)
                    {
                        //...Todo @_@ Aggregate course location and time of week
                        //...By default, use the first
                        Semester semester = null;
                        using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                        {
                            semester = db.Semesters.Where((s) => s.Id == examExt.SemesterGuid).SingleOrDefault();
                        }

                        if (semester != null)
                        {
                            var nodes = courses.First().GetCalendarNodes(semester);
                            this.Dispatcher.BeginInvoke(() =>
                            {
                                this.PivotItem_Course.DataContext = new CourseNode(courses.First(), DateTime.MinValue);
                                TextBlock_QueryCourse.Visibility = Visibility.Collapsed;
                                ProgressBarPopup.Instance.Close();
                            });
                        }
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            TextBlock_NoCourseHint.Visibility = Visibility.Visible;
                            ProgressBarPopup.Instance.Close();
                        });
                    }
                }
            }
            //...Course
            else if (strs.Count() == 2)
            {
                var idStr = strs.Where((s) => s.StartsWith("q=")).SingleOrDefault();
                var dateStr = strs.Where((s) => s.StartsWith("d=")).SingleOrDefault();

                var id = idStr.TrimStart("q=".ToCharArray());
                CourseExt courseExt = null;
                DateTime date;

                DateTime.TryParse(dateStr.TrimStart("d=".ToCharArray()), out date);

                using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                {
                    courseExt = db.Courses.Where((course) => course.Id == id).SingleOrDefault();
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    if (courseExt != null)
                    {
                        PivotItem_Course.DataContext = new CourseNode(courseExt, date);
                        TextBlock_QueryCourse.Visibility = Visibility.Collapsed;
                        TextBlock_PageTitle.DataContext = courseExt;
                    }
                    else
                    {
                        TextBlock_NoExamHint.Visibility = Visibility.Visible;
                        TextBlock_NoCourseHint.Visibility = Visibility.Visible;
                        ProgressBarPopup.Instance.Close();
                    }
                });

                if (courseExt != null)
                {
                    ExamExt examInfo = null;

                    //...Get corresponding exam
                    using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                    {
                        examInfo = db.Exams.Where((exam) => exam.SemesterGuid == courseExt.SemesterGuid && exam.NO == courseExt.NO && exam.Teacher == courseExt.Teacher).FirstOrDefault();
                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        if (examInfo != null)
                        {
                            PivotItem_Exam.DataContext = examInfo;
                            TextBlock_QueryExam.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            TextBlock_NoExamHint.Visibility = Visibility.Visible;
                        }

                        ProgressBarPopup.Instance.Close();
                    });
                }

            }
        }
    }
}