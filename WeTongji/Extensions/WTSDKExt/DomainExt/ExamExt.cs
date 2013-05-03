using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Globalization;
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
    [Table(Name = "Exam")]
    public class ExamExt : IWTObjectExt, INotifyPropertyChanged
    {
        #region [Basic Properties]

        [Column()]
        public String NO { get; set; }

        [Column()]
        public String Name { get; set; }

        [Column()]
        public String Teacher { get; set; }

        [Column()]
        public String Location { get; set; }

        [Column()]
        public DateTime Begin { get; set; }

        [Column()]
        public DateTime End { get; set; }

        [Column()]
        public float Point { get; set; }

        [Column()]
        public String Required { get; set; }

        [Column()]
        public int Hours { get; set; }

        #endregion

        #region [Extended Properties]

        /// <summary>
        /// [SemesterGuid]_[NO]
        /// </summary>
        [Column(IsPrimaryKey = true)]
        public String Id
        {
            get
            {
                return String.Format("{0}_{1}", this.SemesterGuid, this.NO);
            }
            set { }
        }

        /// <summary>
        /// The guid of the course's semester.
        /// Equals null or String.Empty if it is invalid.
        /// </summary>
        [Column(CanBeNull = false)]
        public String SemesterGuid { get; set; }

        /// <summary>
        /// Refers to the exam of the course.
        /// Equals null or String.Empty if it has no exam info temporarily.
        /// </summary>
        [Column()]
        public String CourseNO { get; set; }

        /// <summary>
        /// The UID of the user that takes this exam.
        /// </summary>
        [Column(CanBeNull = false)]
        public String UID { get; set; }

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
            if (!(obj is WeTongji.Api.Domain.Exam))
                throw new ArgumentOutOfRangeException("obj");

            #endregion

            var c = obj as WeTongji.Api.Domain.Exam;

            #region [Save Basic Properties]

            this.NO = c.NO;
            this.Name = c.Name;
            this.Teacher = c.Teacher;
            this.Location = c.Location;
            this.Begin = c.Begin;
            this.End = c.End;
            this.Point = c.Point;
            this.Required = c.Required;
            this.Hours = c.Hours;

            #endregion
        }

        public WTObject GetObject()
        {
            var c = new WeTongji.Api.Domain.Exam();

            c.NO = this.NO;
            c.Name = this.Name;
            c.Teacher = this.Teacher;
            c.Location = this.Location;
            c.Begin = this.Begin;
            c.End = this.End;
            c.Point = this.Point;
            c.Hours = this.Hours;

            return c;
        }

        public Type ExpectedType() { return typeof(WeTongji.Api.Domain.Exam); }

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

        #region [Data Binding]

        public String DisplayBeginTimeAndEndTime
        {
            get
            {
                //...Todo @_@ Localizable

                StringBuilder sb = new StringBuilder(Begin.ToString("yyyy/MM/dd("));

                sb.Append(StringLibrary.ResourceManager.GetString("DayOfWeekAbbr_" + Begin.DayOfWeek.ToString()));

                sb.AppendFormat(") {0}~{1}", Begin.ToString("HH:mm"), End.ToString("HH:mm"));


                return sb.ToString();
            }
        }
        
        #endregion


        #region [Extended Methods]

        /// <summary>
        /// Update this.CourseNO & the value in database.
        /// </summary>
        /// <param name="no">Course NO</param>
        public void SetCourseNO(String no)
        {
            this.CourseNO = no;
            using (var db = new WTUserDataContext(UID))
            {
                var examInDB = db.Exams.Where((e) => e.Id == this.Id).SingleOrDefault();

                if (examInDB != null)
                {
                    examInDB.CourseNO = no;
                    db.SubmitChanges();
                }
            }
        }

        #endregion
    }
}
