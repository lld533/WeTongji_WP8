using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using WeTongji.Api.Domain;
using System.IO.IsolatedStorage;

namespace WeTongji.DataBase
{
    public class WTShareDataContext : DataContext
    {
        private static String ShareDBConnectionString = "Data Source='isostore:/WeTongji.sdf'";

        private WTShareDataContext(String connectionString) : base(connectionString) { }

        public static WTShareDataContext ShareDB
        {
            get
            {
                return new WTShareDataContext(ShareDBConnectionString);
            }
        }

        public void ResetLikeFavoriteSchedule()
        {
            foreach (var p in People)
            {
                p.CanLike = true;
                p.CanFavorite = true;
            }

            foreach (var a in Activities)
            {
                a.CanLike = true;
                a.CanFavorite = true;
                a.CanSchedule = true;
            }

            foreach (var sn in SchoolNewsTable)
            {
                sn.CanLike = true;
                sn.CanFavorite = true;
            }

            foreach (var an in AroundTable)
            {
                an.CanLike = true;
                an.CanFavorite = true;
            }

            foreach (var fs in ForStaffTable)
            {
                fs.CanLike = true;
                fs.CanFavorite = true;
            }

            foreach (var cn in ClubNewsTable)
            {
                cn.CanLike = true;
                cn.CanFavorite = true;
            }

            this.SubmitChanges();
        }
        
        #region [Tables]

        public Table<PersonExt> People;

        public Table<ImageExt> Images;

        public Table<ActivityExt> Activities;

        public Table<ChannelExt> Channels;

        public Table<SchoolNewsExt> SchoolNewsTable;

        public Table<AroundExt> AroundTable;

        public Table<ForStaffExt> ForStaffTable;

        public Table<ClubNewsExt> ClubNewsTable;

        public Table<Event> Events;

        #endregion
    }

    public class WTUserDataContext : DataContext
    {
        public WTUserDataContext(String uid)
            : base(String.Format("Data Source='isostore:/{0}.sdf'", uid))
        {
            if (!this.DatabaseExists())
            {
                CreateDatabase();

                for (uint i = 0; i < (uint)FavoriteIndex.FavoriteTypeCount;++i )
                {
                    this.Favorites.InsertOnSubmit(new FavoriteObject() { Id = i, Value = String.Empty });
                }
                this.SubmitChanges();
            }
        }

        public static Boolean UserDataContextExists(String uid)
        {
            var store = IsolatedStorageFile.GetUserStoreForApplication();

            return store.FileExists(uid + ".sdf");
        }

        #region [Tables]

        public Table<UserExt> UserInfo;

        public Table<ImageExt> Images;

        public Table<CourseExt> Courses;

        public Table<ExamExt> Exams;

        public Table<FavoriteObject> Favorites;

        public Table<Semester> Semesters;

        #endregion
    }
}
