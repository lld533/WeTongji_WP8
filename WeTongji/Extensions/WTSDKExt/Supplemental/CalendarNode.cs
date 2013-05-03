using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace WeTongji.Api.Domain
{
    public enum CalendarNodeType : int
    {
        kActivity,
        kOptionalCourse,
        kObligedCourse,
        kExam
    }

    public class CalendarNode : INotifyPropertyChanged, IComparable
    {
        #region [Properties]

        public DateTime BeginTime { get; set; }

        public DateTime EndTime { get; set; }

        public String Title { get; set; }

        public String Location { get; set; }

        /// <summary>
        /// The identifier of the type.
        /// </summary>
        /// <remarks>
        /// Int if NodeType == kActivity,
        /// Otherwise, String.
        /// </remarks>
        public Object Id { get; set; }

        public CalendarNodeType NodeType { get; set; }

        public CalendarNode Self { get { return this; } }

        #endregion

        #region [Operators]

        public static Boolean operator <(CalendarNode n1, CalendarNode n2)
        {
            if (n1.BeginTime != n2.BeginTime)
                return n1.BeginTime < n2.BeginTime;
            if (n1.NodeType != n2.NodeType)
                return (int)n1.NodeType < (int)n2.NodeType;
            if (n1.EndTime != n2.EndTime)
                return n1.EndTime < n2.EndTime;

            switch (n1.NodeType)
            {
                case CalendarNodeType.kActivity:
                    return (int)n1.Id < (int)n2.Id;
                default:
                    return String.Compare((String)n1.Id, (String)n2.Id) < 0;
            }
        }

        public static Boolean operator >(CalendarNode n1, CalendarNode n2)
        {
            if (n1.BeginTime != n2.BeginTime)
                return n1.BeginTime > n2.BeginTime;
            if (n1.NodeType != n2.NodeType)
                return (int)n1.NodeType > (int)n2.NodeType;
            if (n1.EndTime != n2.EndTime)
                return n1.EndTime > n2.EndTime;

            switch (n1.NodeType)
            {
                case CalendarNodeType.kActivity:
                    return (int)n1.Id > (int)n2.Id;
                default:
                    return String.Compare((String)n1.Id, (String)n2.Id) > 0;
            }
        }

        public static Boolean operator <(CalendarNode node, DateTime time)
        {
            return node.EndTime < time;
        }

        public static Boolean operator >(CalendarNode node, DateTime time)
        {
            return node.BeginTime > time;
        }

        #endregion

        #region [Data Bindings]

        public String DisplayBeginTime
        {
            get { return BeginTime.ToString("HH:mm"); }
        }

        public String DisplayEndTime
        {
            get { return EndTime.ToString("HH:mm"); }
        }

        public String DisplayDateofBeginTime
        {
            get
            {
                //...Todo @_@ Localizable
                if (BeginTime.Date == DateTime.Now.Date)
                {
                    return StringLibrary.CalendarNode_Today;
                }

                if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "zh")
                {
                    return BeginTime.ToString("M月d日");
                }
                else
                {
                    var sb = new StringBuilder();

                    switch (BeginTime.Month)
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

                    sb.AppendFormat(" {0}", BeginTime.Day);

                    return sb.ToString();
                }
            }
        }

        public String DisplayDayTimeOfBeginTime
        {
            get
            {
                return BeginTime.ToString("HH:mm");
            }
        }

        public Boolean IsNoArrangementNode
        {
            get { return BeginTime == DateTime.Now.Date && EndTime == DateTime.MinValue; }
        }

        public SolidColorBrush NodeBrush
        {
            get
            {
                switch (NodeType)
                {
                    case CalendarNodeType.kActivity:
                        return App.Current.Resources["ActivityAgendaTitleBrush"] as SolidColorBrush;
                    case CalendarNodeType.kObligedCourse:
                        return App.Current.Resources["RequiredCourseAgendaTitleBrush"] as SolidColorBrush;
                    case CalendarNodeType.kOptionalCourse:
                        return App.Current.Resources["OptionalCourseAgendaTitleBrush"] as SolidColorBrush;
                    case CalendarNodeType.kExam:
                        return App.Current.Resources["ExamInfoAgendaTitleBrush"] as SolidColorBrush;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #endregion

        #region [ICompare]

        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException();

            var node = obj as CalendarNode;

            if (node == null)
                throw new NotSupportedException("CalendarNode is expected.");

            if (IsNoArrangementNode)
                return node.IsNoArrangementNode ? 0 : 1;
            else if (node.IsNoArrangementNode)
                return -1;

            if (BeginTime != node.BeginTime)
            {
                return BeginTime.CompareTo(node.BeginTime);
            }
            else if (this.NodeType == node.NodeType)
            {
                if (NodeType == CalendarNodeType.kActivity)
                {
                    return ((int)Id).CompareTo((int)node.Id);
                }
                else
                {
                    return ((String)Id).CompareTo((String)node.Id);
                }
            }
            else
            {
                return ((int)this.NodeType).CompareTo((int)node.NodeType);
            }
        }

        #endregion

        #region [Notify Property Changed]

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region [Special Calendar node]

        public static CalendarNode NoArrangementNode
        {
            get
            {
                return new CalendarNode() { BeginTime = DateTime.Now.Date, EndTime = DateTime.MinValue };
            }
        }

        #endregion
    }

    public static class CanlendarNodeUtil
    {
        public static readonly TimeSpan[] CourseStartTime = new TimeSpan[]
        {
            // [8:00]
            TimeSpan.FromHours(8),
            // [8:55]
            TimeSpan.FromHours(8) + TimeSpan.FromMinutes(55),
            // [10:00]
            TimeSpan.FromHours(10),
            // [10:55]
            TimeSpan.FromHours(10) + TimeSpan.FromMinutes(55),
            // [13:30]
            TimeSpan.FromHours(13) + TimeSpan.FromMinutes(30),
            // [14:20]
            TimeSpan.FromHours(14) + TimeSpan.FromMinutes(20),
            // [15:25]
            TimeSpan.FromHours(15) + TimeSpan.FromMinutes(25),
            // [16:15]
            TimeSpan.FromHours(16) + TimeSpan.FromMinutes(15),
            // [18:30]
            TimeSpan.FromHours(18) + TimeSpan.FromMinutes(30),
            // [19:25]
            TimeSpan.FromHours(19) + TimeSpan.FromMinutes(25),
            // [20:20]
            TimeSpan.FromHours(20) + TimeSpan.FromMinutes(20)
        };

        public static readonly TimeSpan[] CourseEndTime = new TimeSpan[]
        {
            // [8:45]
            TimeSpan.FromHours(8) + TimeSpan.FromMinutes(45),
            // [9:40]
            TimeSpan.FromHours(9) + TimeSpan.FromMinutes(40),
            // [10:45]
            TimeSpan.FromHours(10) + TimeSpan.FromMinutes(45),
            // [11:40]
            TimeSpan.FromHours(11) + TimeSpan.FromMinutes(40),
            // [14:15]
            TimeSpan.FromHours(14) + TimeSpan.FromMinutes(15),
            // [15:05]
            TimeSpan.FromHours(15) + TimeSpan.FromMinutes(5),
            // [16:10]
            TimeSpan.FromHours(16) + TimeSpan.FromMinutes(10),
            // [17:00]
            TimeSpan.FromHours(17),
            // [19:15]
            TimeSpan.FromHours(19) + TimeSpan.FromMinutes(15),
            // [20:10]
            TimeSpan.FromHours(20) + TimeSpan.FromMinutes(10),
            // [21:05]
            TimeSpan.FromHours(21) + TimeSpan.FromMinutes(5)
        };

        /// <summary>
        /// Get a course's start time from an appointed day.
        /// </summary>
        /// <param name="dt">date component of an appointed day</param>
        /// <param name="section">1-based, from 1 to 11</param>
        /// <returns></returns>
        public static DateTime GetCourseStartTime(this DateTime dt, int section)
        {
            if (section < 1 || section > 11)
                throw new ArgumentOutOfRangeException("section");

            return dt.Date + CourseStartTime[section - 1];
        }

        /// <summary>
        /// Get a course's end time from an appointed day.
        /// </summary>
        /// <param name="dt">date component of an appointed day</param>
        /// <param name="section">1-based, from 1 to 11</param>
        /// <returns></returns>
        public static DateTime GetCourseEndTime(this DateTime dt, int section)
        {
            if (section < 1 || section > 11)
                throw new ArgumentOutOfRangeException("section");

            return dt.Date + CourseEndTime[section - 1];
        }

        public static CalendarNode GetCalendarNode(this ActivityExt a)
        {
            if (a == null)
                throw new ArgumentNullException();

            return new CalendarNode()
            {
                BeginTime = a.Begin,
                EndTime = a.End,
                Title = a.Title,
                Location = a.Location,
                Id = a.Id,
                NodeType = CalendarNodeType.kActivity
            };
        }

        /// <summary>
        /// Generate a calendar node from a course on an appointed day
        /// </summary>
        /// <param name="c">the course</param>
        /// <param name="s">the semester</param>
        /// <param name="day">the appointed day</param>
        /// <returns>Returns null if the course does not fit the given day. Otherwise, a calendar node.</returns>
        public static CalendarNode GetCalendarNode(this CourseExt c, Semester s, DateTime day)
        {
            if (c == null)
                throw new ArgumentException("c");
            if (s == null)
                throw new ArgumentNullException("s");

            return c.GetCalendarNode(s.SchoolYearStartAt, s.SchoolYearCourseEndAt, day);
        }

        /// <summary>
        /// Generate a calendar node from a course on an appointed day
        /// </summary>
        /// <param name="c">the course</param>
        /// <param name="courseWeekStart">semester start</param>
        /// <param name="courseWeekEnd">semester end</param>
        /// <param name="day">the appointed day</param>
        /// <returns>Returns null if the course does not fit the given day. Otherwise, a calendar node.</returns>
        public static CalendarNode GetCalendarNode(this CourseExt c, DateTime courseWeekStart, DateTime courseWeekEnd, DateTime day)
        {
            var date = day.Date;
            if (date < courseWeekStart || date > courseWeekEnd || c.DayOfWeek != date.DayOfWeek)
                return null;

            DateTime begin;

            //...Check week type
            if (c.WeekTypeExt == CourseWeekProperty.kEvenWeek)
            {
                begin = courseWeekStart + TimeSpan.FromDays(6 + (int)c.DayOfWeek);

                if ((int)(date - begin).TotalDays % 14 != 0)
                {
                    return null;
                }
            }
            else if (c.WeekTypeExt == CourseWeekProperty.kOddWeek)
            {
                int fromDays = c.DayOfWeek == DayOfWeek.Sunday ? 6 : ((int)c.DayOfWeek - 1);

                begin = courseWeekStart + TimeSpan.FromDays(fromDays);

                if ((int)(date - begin).TotalDays % 14 != 0)
                {
                    return null;
                }
            }

            return new CalendarNode()
                {
                    Id = c.Id,
                    Title = c.Name,
                    BeginTime = date.GetCourseStartTime(c.SectionStart),
                    EndTime = date.GetCourseEndTime(c.SectionEnd),
                    Location = c.Location,
                    NodeType = c.IsObliged ? CalendarNodeType.kObligedCourse : CalendarNodeType.kOptionalCourse
                };
        }

        /// <summary>
        /// Generate all course nodes in a semester from a course
        /// </summary>
        /// <param name="c">the course</param>
        /// <param name="courseWeekStart">semester school year start at</param>
        /// <param name="courseWeekEnd">semester school course year end at</param>
        /// <returns>an enumerable calendar nodes</returns>
        public static IEnumerable<CalendarNode> GetCanlendarNodes(this CourseExt c, DateTime courseWeekStart, DateTime courseWeekEnd)
        {
            if (courseWeekStart > courseWeekEnd)
                throw new ArgumentOutOfRangeException("courseWeekEnd");

            var result = new List<CalendarNode>();

            DateTime begin, end;
            TimeSpan delta;
            CalendarNodeType type = c.IsObliged ? CalendarNodeType.kObligedCourse : CalendarNodeType.kOptionalCourse;

            if (c.WeekTypeExt == CourseWeekProperty.kEvenWeek)
            {
                int fromDays = c.DayOfWeek == DayOfWeek.Sunday ? 13 : (6 + (int)c.DayOfWeek);

                begin = (courseWeekStart + TimeSpan.FromDays(fromDays)).GetCourseStartTime(c.SectionStart);
                end = (courseWeekStart + TimeSpan.FromDays(fromDays)).GetCourseEndTime(c.SectionEnd);
            }
            else
            {
                int fromDays = c.DayOfWeek == DayOfWeek.Sunday ? 6 : ((int)c.DayOfWeek - 1);

                begin = (courseWeekStart + TimeSpan.FromDays(fromDays)).GetCourseStartTime(c.SectionStart);
                end = (courseWeekStart + TimeSpan.FromDays(fromDays)).GetCourseEndTime(c.SectionEnd);
            }

            if (c.WeekTypeExt == CourseWeekProperty.kWeekly)
            {
                delta = TimeSpan.FromDays(7);
            }
            else
            {
                delta = TimeSpan.FromDays(14);
            }

            while (begin < courseWeekEnd)
            {
                result.Add(new CalendarNode()
                {
                    Id = c.Id,
                    Title = c.Name,
                    Location = c.Location,
                    BeginTime = begin,
                    EndTime = end,
                    NodeType = type
                });

                begin += delta;
                end += delta;
            }

            return result;
        }

        public static IEnumerable<CalendarNode> GetCalendarNodes(this CourseExt c, Semester s)
        {
            if (c == null)
                throw new ArgumentNullException("c");
            if (s == null)
                throw new ArgumentNullException("s");

            return c.GetCanlendarNodes(s.SchoolYearStartAt, s.SchoolYearCourseEndAt);
        }

        public static CalendarNode GetCalendarNode(this ExamExt e)
        {
            return new CalendarNode()
                {
                    BeginTime = e.Begin,
                    EndTime = e.End,
                    Title = e.Name,
                    Location = e.Location,
                    Id = e.Id,
                    NodeType = CalendarNodeType.kExam
                };
        }
    }
}
