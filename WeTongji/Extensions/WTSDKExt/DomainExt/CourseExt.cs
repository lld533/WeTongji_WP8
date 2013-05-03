using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WeTongji.DataBase;
using WeTongji.Utility;

namespace WeTongji.Api.Domain
{
    public enum CourseWeekProperty
    {
        kOddWeek,
        kEvenWeek,
        kWeekly
    }

    [Table(Name = "Course")]
    public class CourseExt : IWTObjectExt, INotifyPropertyChanged
    {
        #region [Basic Properties]

        [Column()]
        public String NO { get; set; }

        [Column()]
        public int Hours { get; set; }

        [Column()]
        public float Point { get; set; }

        [Column()]
        public String Name { get; set; }

        [Column()]
        public String Teacher { get; set; }

        [Column()]
        public String WeekType { get; set; }

        [Column()]
        public String WeekDay { get; set; }

        [Column()]
        public int SectionStart { get; set; }

        [Column()]
        public int SectionEnd { get; set; }

        [Column()]
        public String Required { get; set; }

        [Column()]
        public String Location { get; set; }

        #endregion

        #region [Extended Properties]

        [Column(IsPrimaryKey = true)]
        public String Id { get; set; }

        /// <summary>
        /// The guid of the course's semester.
        /// </summary>
        [Column(CanBeNull = false)]
        public String SemesterGuid { get; set; }

        /// <summary>
        /// Refers to the exam of the course.
        /// Equals null or String.Empty if it has no exam info temporarily.
        /// </summary>
        [Column()]
        public String ExamNO { get; set; }

        /// <summary>
        /// The UID of the user that takes this course.
        /// </summary>
        [Column(CanBeNull = false)]
        public String UID { get; set; }

        /// <summary>
        /// Returns DayofWeek.Sunday~DayofWeek.Saturday
        /// </summary>
        public DayOfWeek DayOfWeek
        {
            get
            {
                switch (WeekDay)
                {
                    case "星期一":
                        return DayOfWeek.Monday;
                    case "星期二":
                        return DayOfWeek.Tuesday;
                    case "星期三":
                        return DayOfWeek.Wednesday;
                    case "星期四":
                        return DayOfWeek.Thursday;
                    case "星期五":
                        return DayOfWeek.Friday;
                    case "星期六":
                        return DayOfWeek.Saturday;
                    default:
                        return DayOfWeek.Sunday;
                }
            }
        }

        public CourseWeekProperty WeekTypeExt
        {
            get 
            {
                if (WeekType == "单")
                    return CourseWeekProperty.kOddWeek;
                else if (WeekType == "双")
                    return CourseWeekProperty.kEvenWeek;
                else if (WeekType == "全")
                    return CourseWeekProperty.kWeekly;
                else
                    throw new ArgumentOutOfRangeException("WeekType");
            }
        }

        public Boolean IsObliged
        {
            get
            {
                return Required == "必修";
            }
        }
        #endregion

        #region [Implementation]

        /// <summary>
        /// </summary>
        /// <remarks>
        /// This function ONLY sets the public properties of
        /// an instance of a WeTongji.Api.Domain.Course.
        /// </remarks>
        /// <param name="obj"></param>
        public void SetObject(WTObject obj)
        {
            #region [Check Argument]

            if (obj == null)
                throw new ArgumentNullException("obj");
            if (!(obj is WeTongji.Api.Domain.Course))
                throw new ArgumentOutOfRangeException("obj");

            #endregion

            var c = obj as WeTongji.Api.Domain.Course;

            #region [Save Basic Properties]

            this.NO = c.NO;
            this.Hours = c.Hours;
            this.Point = c.Point;
            this.Name = c.Name;
            this.Teacher = c.Teacher;
            this.WeekType = c.WeekType;
            this.WeekDay = c.WeekDay;
            this.SectionStart = c.SectionStart;
            this.SectionEnd = c.SectionEnd;
            this.Required = c.Required;
            this.Location = c.Location;

            #endregion
        }

        public WTObject GetObject()
        {
            var c = new WeTongji.Api.Domain.Course();

            c.NO = this.NO;
            c.Hours = this.Hours;
            c.Point = this.Point;
            c.Name = this.Name;
            c.Teacher = this.Teacher;
            c.WeekDay = this.WeekDay;
            c.SectionStart = this.SectionStart;
            c.SectionEnd = this.SectionEnd;
            c.Required = this.Required;
            c.Location = this.Location;

            return c;
        }

        public Type ExpectedType() { return typeof(WeTongji.Api.Domain.Course); }

        #region [PropertyChanged]

        public event PropertyChangedEventHandler PropertyChanged;

        public void SendPropertyChanged(String propertyName)
        {
            NotifyPropertyChanged(propertyName);
        }

        private void NotifyPropertyChanged(String propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #endregion

        #region [Extended Methods]

        /// <summary>
        /// Update the value of the exam 
        /// </summary>
        /// <param name="no"></param>
        public void SetExamNO(String no)
        {
            this.ExamNO = no;
            using (var db = new WTUserDataContext(UID))
            {
                var courseInDB = db.Courses.Where((c) => c.Id == this.Id).SingleOrDefault();

                if (courseInDB != null)
                {
                    courseInDB.ExamNO = no;
                    db.SubmitChanges();
                }
            }
        }

        #endregion
    }
}
