using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace WeTongji.Api.Domain
{
    public abstract class WTObject { }

    #region [WTNews]

    public class WTNews : WTObject
    {
        public int Id { get; set; }

        public int Like { get; set; }

        public bool CanLike { get; set; }

        public int Favorite { get; set; }

        public bool CanFavorite { get; set; }

        public int Read { get; set; }

        public String Title { get; set; }

        public String Source { get; set; }

        public String Summary { get; set; }

        public String Context { get; set; }

        /// <summary>
        /// Images' uri array
        /// </summary>
        public String[] Images { get; set; }

        public DateTime CreatedAt { get; set; }
    }
    #endregion

    #region [User]
    public class User : WTObject
    {
        public String NO { get; set; }

        public String Name { get; set; }

        public String Avatar { get; set; }

        public String UID { get; set; }

        public String Phone { get; set; }

        public String DisplayName { get; set; }

        public String Major { get; set; }

        public String NativePlace { get; set; }

        public String Degree { get; set; }

        public String Gender { get; set; }

        public String Year { get; set; }

        public DateTime Birthday { get; set; }

        public String Plan { get; set; }

        public String SinaWeibo { get; set; }

        public String QQ { get; set; }

        public String Department { get; set; }

        public String Email { get; set; }

        public User Clone()
        {
            return this.MemberwiseClone() as User;
        }
    }
    #endregion

    #region [UserProfile]

    public class UserProfile : WTObject
    {
        public Boolean CanBeFound { get; set; }
        public Boolean AllowAddFriend { get; set; }
        public Boolean PublicSchedule { get; set; }
        public Boolean PublicScheduleToAll { get; set; }
        public Boolean PublicInformation { get; set; }
        public Boolean PublicInformationToAll { get; set; }
    }

    #endregion

    #region [Course]

    public class Course : WTObject
    {
        public String NO { get; set; }

        public int Hours { get; set; }

        public float Point { get; set; }

        public String Name { get; set; }

        public String Teacher { get; set; }

        public String WeekType { get; set; }

        public String WeekDay { get; set; }

        public int SectionStart { get; set; }

        public int SectionEnd { get; set; }

        public String Required { get; set; }

        public String Location { get; set; }
    }

    #endregion

    #region [Course Instance]

    public class CourseInstance : WTObject
    {
        public String NO { get; set; }
        public String Name { get; set; }
        public String Teacher { get; set; }
        public String Hours { get; set; }
        public String Location { get; set; }
        public int SectionStart { get; set; }
        public int SectionEnd { get; set; }
        public float Point { get; set; }
        public String Required { get; set; }
        public DateTime Day { get; set; }
    }

    #endregion

    #region [Event]
    [Table]
    public class Event : WTObject
    {
        [Column(IsPrimaryKey = true)]
        public int Id { get; set; }

        [Column()]
        public DateTime Begin { get; set; }

        [Column()]
        public DateTime End { get; set; }

        [Column()]
        public String Title { get; set; }

        [Column()]
        public String Location { get; set; }

        [Column()]
        public String Description { get; set; }
    }
    #endregion

    #region [Activity]
    public class Activity : WTObject
    {
        public int Id { get; set; }

        public DateTime Begin { get; set; }

        public DateTime End { get; set; }

        public String Title { get; set; }

        public String Location { get; set; }

        public String Description { get; set; }

        public int Like { get; set; }

        public bool CanLike { get; set; }

        public int Favorite { get; set; }

        public bool CanFavorite { get; set; }

        public int Schedule { get; set; }

        public bool CanSchedule { get; set; }

        public int Channel_id { get; set; }

        public String Organizer { get; set; }

        public String OrganizerAvatar { get; set; }

        public String Status { get; set; }

        public String Image { get; set; }

        public DateTime CreatedAt { get; set; }
    }
    #endregion

    #region [Channel]

    public class Channel : WTObject
    {
        public int Id { get; set; }

        public String Title { get; set; }

        public String Image { get; set; }

        public int Follow { get; set; }

        public String Description { get; set; }
    }

    #endregion

    #region [Exam]

    public class Exam : WTObject
    {
        public String NO { get; set; }

        public String Name { get; set; }

        public String Teacher { get; set; }

        public String Location { get; set; }

        public DateTime Begin { get; set; }

        public DateTime End { get; set; }

        public float Point { get; set; }

        public String Required { get; set; }

        public int Hours { get; set; }
    }

    #endregion

    #region [School News]
    public class SchoolNews : WTNews { }
    #endregion

    #region [Club News]
    public class ClubNews : WTNews
    {
        public String Organizer { get; set; }

        public String OrganizerAvatar { get; set; }
    }
    #endregion

    #region [Around]
    public class Around : WTNews
    {
        /// <summary>
        /// A string that contains the url info of a title image
        /// </summary>
        public String Image { get; set; }

        public String Location { get; set; }

        public String Contact { get; set; }

        public String TicketService { get; set; }
    }
    #endregion

    #region [For Staff]
    public class ForStaff : WTNews { }
    #endregion

    #region [Version]
    public class Version : WTObject
    {
        public String Current { get; set; }

        public String Latest { get; set; }

        public String Url { get; set; }
    }
    #endregion

    #region [Person]
    public class Person : WTObject
    {
        public int Id { get; set; }

        public String Name { get; set; }

        public String JobTitle { get; set; }

        public String Words { get; set; }

        public String NO { get; set; }

        public String Avatar { get; set; }

        public String StudentNO { get; set; }

        /// Key := Url
        /// Value := Description
        /// </summary>
        public Dictionary<String, String> Images { get; set; }

        public String Description { get; set; }

        public int Read { get; set; }

        public int Like { get; set; }

        public bool CanLike { get; set; }

        public int Favorite { get; set; }

        public bool CanFavorite { get; set; }
    }
    #endregion
}
