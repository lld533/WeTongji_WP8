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
    [Table(Name = "User")]
    public class UserExt : IWTObjectExt, INotifyPropertyChanged
    {
        #region [Basic Properties]

        [Column(IsPrimaryKey = true)]
        public String NO { get; set; }

        [Column()]
        public String Name { get; set; }

        [Column()]
        public String Avatar { get; set; }

        [Column()]
        public String UID { get; set; }

        [Column()]
        public String Phone { get; set; }

        [Column()]
        public String DisplayName { get; set; }

        [Column()]
        public String Major { get; set; }

        [Column()]
        public String NativePlace { get; set; }

        [Column()]
        public String Degree { get; set; }

        [Column()]
        public String Gender { get; set; }

        [Column()]
        public String Year { get; set; }

        [Column()]
        public DateTime Birthday { get; set; }

        [Column()]
        public String Plan { get; set; }

        [Column()]
        public String SinaWeibo { get; set; }

        [Column()]
        public String QQ { get; set; }

        [Column()]
        public String Department { get; set; }

        [Column()]
        public String Email { get; set; }

        #endregion

        #region [Extended Properties]

        [Column()]
        public String AvatarGuid { get; set; }

        #endregion

        #region [Implementation]

        public void SetObject(WTObject obj)
        {
            #region [Check argument]

            if (obj == null)
                throw new ArgumentNullException("obj");

            if (!(obj is WeTongji.Api.Domain.User))
                throw new ArgumentOutOfRangeException("obj");

            #endregion

            var user = obj as WeTongji.Api.Domain.User;

            #region [Save Extended Properties]

            if (Avatar != user.Avatar)
            {
                //...Update image url in the database and delete existing avatar file in isolated storage folder
                if (!String.IsNullOrEmpty(AvatarGuid))
                {
                    #region [Handles database conflicts]

                    //...Remove user's avatar if new avatar is missing.
                    if (user.Avatar.EndsWith("missing.png"))
                    {
                        using (var db = new WTUserDataContext(user.UID))
                        {
                            // find out the previous image in database
                            var imgInDB = db.Images.Where((img) => img.Id == AvatarGuid).SingleOrDefault();

                            //...delete avatar image info
                            if (imgInDB != null)
                            {
                                db.Images.DeleteOnSubmit(imgInDB);

                                db.SubmitChanges();
                            }
                        }

                        this.AvatarGuid = String.Empty;
                    }
                    //..Update avatar url.
                    else
                    {
                        using (var db = new WTUserDataContext(user.UID))
                        {
                            // find out the previous image in database
                            var imgInDB = db.Images.Where((img) => img.Id == AvatarGuid).SingleOrDefault();

                            //...Update url info.
                            if (imgInDB != null)
                            {
                                imgInDB.Url = user.Avatar;

                                db.SubmitChanges();
                            }
                        }
                    }
                    #endregion

                    #region [Handles Isolated storage folder conflicts]

                    var store = IsolatedStorageFile.GetUserStoreForApplication();

                    var previousAvatarFileName = String.Format("{0}.{1}", AvatarGuid, Avatar.GetImageFileExtension());

                    //...Delete previous avatar file in the isolated storage folder
                    if (store.FileExists(previousAvatarFileName))
                    {
                        store.DeleteFile(previousAvatarFileName);
                    }

                    #endregion
                }
                //...Store new avatar image info in user's database.
                else
                {
                    var img = new ImageExt()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Url = Avatar
                    };
                    using (var db = new WTUserDataContext(user.UID))
                    {
                        db.Images.InsertOnSubmit(img);
                        db.SubmitChanges();
                    }
                    AvatarGuid = img.Id;
                }
            }

            #endregion

            #region [Save Basic Properties]

            this.NO = user.NO;
            this.Name = user.Name;
            this.Avatar = user.Avatar;
            this.UID = user.UID;
            this.Phone = user.Phone;
            this.DisplayName = user.DisplayName;
            this.Major = user.Major;
            this.NativePlace = user.NativePlace;
            this.Degree = user.Degree;
            this.Gender = user.Gender;
            this.Year = user.Year;
            this.Birthday = user.Birthday;
            this.Plan = user.Plan;
            this.SinaWeibo = user.SinaWeibo;
            this.QQ = user.QQ;
            this.Department = user.Department;
            this.Email = user.Email;

            #endregion
        }

        public WTObject GetObject()
        {
            var user = new WeTongji.Api.Domain.User();

            user.NO = this.NO;
            user.Name = this.Name;
            user.Avatar = this.Avatar;
            user.UID = this.UID;
            user.Phone = this.Phone;
            user.DisplayName = this.DisplayName;
            user.Major = this.Major;
            user.NativePlace = this.NativePlace;
            user.Degree = this.Degree;
            user.Gender = this.Gender;
            user.Year = this.Year;
            user.Birthday = this.Birthday;
            user.Plan = this.Plan;
            user.SinaWeibo = this.SinaWeibo;
            user.QQ = this.QQ;
            user.Department = this.Department;
            user.Email = this.Email;

            return user;
        }

        public Type ExpectedType()
        {
            return typeof(WeTongji.Api.Domain.User);
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

        #region [Extended Methods]

        public Boolean AvatarImageExists()
        {
            var store = IsolatedStorageFile.GetUserStoreForApplication();

            var fileExt = Avatar.GetImageFileExtension();
            var fileName = String.Format("{0}.{1}", AvatarGuid, fileExt);

            return store.FileExists(fileName);
        }

        public void SaveAvatarImage(Stream stream)
        {
            var store = IsolatedStorageFile.GetUserStoreForApplication();

            var fileExt = Avatar.GetImageFileExtension();
            var fileName = String.Format("{0}.{1}", AvatarGuid, fileExt);

            using (var fs = store.CreateFile(fileName))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fs);

                fs.Flush();
                fs.Close();
            }
        }

        public UserExt Clone()
        {
            return this.MemberwiseClone() as UserExt;
        }

        #endregion

        #region [Data Binding]

        public int FavoritesCount
        {
            get
            {
                int result = 0;
                FavoriteObject[] foArr = null;

                using (var db = new WTUserDataContext(this.UID))
                {
                    foArr = db.Favorites.ToArray();
                }

                foreach (var fo in foArr)
                {
                    result += fo.Value.Split("_".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Count();
                }

                return result;
            }
        }

        /// <summary>
        /// Used in MainPage.xaml, restrict 2 digits of FavoritesCount.
        /// </summary>
        public String DisplayFavoritesCount
        {
            get 
            {
                return Math.Min(FavoritesCount, 99).ToString();
            }
        }

        public ImageSource AvatarImageBrush
        {
            get
            {
                if (AvatarGuid.EndsWith("missing.png"))
                    return new BitmapImage(new Uri("/Images/default_avatar_profile.png", UriKind.RelativeOrAbsolute));

                var fileExt = Avatar.GetImageFileExtension();

                var imgSrc = String.Format("{0}.{1}", AvatarGuid, fileExt).GetImageSource();

                if (imgSrc == null)
                    return new BitmapImage(new Uri("/Images/default_avatar_profile.png", UriKind.RelativeOrAbsolute));
                else
                    return imgSrc;
            }
        }

        public Boolean IsMale
        {
            get { return this.Gender == "男"; }
        }

        #endregion
    }
}
