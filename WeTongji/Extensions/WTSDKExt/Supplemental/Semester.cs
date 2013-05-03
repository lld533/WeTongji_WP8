using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;

namespace WeTongji.Api.Domain
{
    [Table()]
    public class Semester : IWTObjectExt, INotifyPropertyChanged
    {
        #region [Properties]

        /// <summary>
        /// A guid string
        /// </summary>
        [Column(IsPrimaryKey = true)]
        public String Id { get; set; }

        [Column()]
        public DateTime SchoolYearStartAt { get; set; }

        /// <summary>
        /// 学年总周数
        /// </summary>
        [Column()]
        public int SchoolYearWeekCount { get; set; }

        /// <summary>
        /// 学年教学周数
        /// </summary>
        [Column()]
        public int SchoolYearCourseWeekCount { get; set; }

        #endregion

        #region [Extended Properties]

        public DateTime SchoolYearCourseEndAt
        {
            get
            {
                return SchoolYearStartAt + TimeSpan.FromDays(7 * SchoolYearCourseWeekCount);
            }
        }

        /// <summary>
        /// Return SchoolYearStartAt + semester expansion
        /// </summary>
        public DateTime SchoolYearEndAt
        {
            get
            {
                return SchoolYearStartAt + TimeSpan.FromDays(7 * SchoolYearWeekCount);
            }
        }

        #endregion

        #region [Implementation]

        /// <summary>
        /// throw NotSupportException
        /// </summary>
        /// <returns></returns>
        public Type ExpectedType()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// throw NotSupportException
        /// </summary>
        /// <returns></returns>
        public void SetObject(WTObject obj)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// throw NotSupportException
        /// </summary>
        /// <returns></returns>
        public WTObject GetObject()
        {
            throw new NotSupportedException();
        }

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

        #region [Overridden]

        /// <summary>
        /// Same SchoolYearStartAt & Same SchoolYearWeekCount
        /// means the same semester.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var result = false;
            var s = obj as Semester;

            if (s != null)
                result = this.SchoolYearStartAt == s.SchoolYearStartAt
                    && this.SchoolYearWeekCount == s.SchoolYearWeekCount;

            return result;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region [Operator]

        public static Boolean operator <(Semester s1, Semester s2)
        {
            return s1.SchoolYearStartAt < s2.SchoolYearStartAt;
        }


        public static Boolean operator >(Semester s1, Semester s2)
        {

            return s1.SchoolYearStartAt > s2.SchoolYearStartAt;
        }

        #endregion

        #region [Functions]

        public Boolean IsInSemester(ExamExt exam)
        {
            if (exam == null)
                throw new ArgumentNullException();

            return exam.Begin >= this.SchoolYearStartAt && exam.End <= SchoolYearEndAt;
        }

        #endregion

    }
}
