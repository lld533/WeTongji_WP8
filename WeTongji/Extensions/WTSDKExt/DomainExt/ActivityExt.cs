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
using ImageTools;
using System.Globalization;

namespace WeTongji.Api.Domain
{
    [Table(Name = "Activity")]
    public class ActivityExt : IWTObjectExt, INotifyPropertyChanged
    {
        #region

        public static readonly DateTime FirstActivityCreatedAt = new DateTime(2012, 5, 26);

        #endregion

        #region [Basic Properties]

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

        [Column()]
        public int Like { get; set; }

        [Column()]
        public bool CanLike { get; set; }

        [Column()]
        public int Favorite { get; set; }

        [Column()]
        public bool CanFavorite { get; set; }

        [Column()]
        public int Schedule { get; set; }

        [Column()]
        public bool CanSchedule { get; set; }

        [Column()]
        public int Channel_id { get; set; }

        [Column()]
        public String Organizer { get; set; }

        [Column()]
        public String OrganizerAvatar { get; set; }

        [Column()]
        public String Status { get; set; }

        [Column()]
        public String Image { get; set; }

        [Column()]
        public DateTime CreatedAt { get; set; }

        #endregion

        #region [Extended Properties]

        [Column()]
        public String ImageGuid { get; set; }

        [Column()]
        public String OrganizerAvatarGuid { get; set; }

        #endregion

        #region [Data Binding]

        public String DisplayTime
        {
            get
            {
                return String.Format("{0:yyyy}/{0:MM}/{0:dd}({1}) {0:HH}:{0:mm}~{2:HH}:{2:mm}",
                    Begin,
                    StringLibrary.ResourceManager.GetString("DayOfWeekAbbr_" + Begin.DayOfWeek.ToString()),
                    End);
            }
        }

        /// <summary>
        /// Get the organizer avatar for data binding.
        /// [Fall back value] missing.png
        /// </summary>
        public ImageSource OrganizerAvatarImageBrush
        {
            get
            {
                if (OrganizerAvatar.EndsWith("missing.png"))
                    return new BitmapImage(new Uri("/Images/default_avatar_org.png", UriKind.RelativeOrAbsolute));

                var fileExt = OrganizerAvatar.GetImageFileExtension();

                var imgSrc = String.Format("{0}.{1}", OrganizerAvatarGuid, fileExt).GetImageSource();

                if (imgSrc == null)
                    return new BitmapImage(new Uri("/Images/default_avatar_org.png", UriKind.RelativeOrAbsolute));
                else
                    return imgSrc;
            }
        }

        /// <summary>
        /// Get the illustration of activity for data binding
        /// [Fall back value] missing.png
        /// </summary>
        public ImageSource ActivityImageBrush
        {
            get
            {
                if (Image.EndsWith("missing.png"))
                    return new BitmapImage(new Uri("/Images/default_avatar_org.png", UriKind.RelativeOrAbsolute));

                var fileExt = Image.GetImageFileExtension();

                var imgSrc = String.Format("{0}.{1}", ImageGuid, fileExt).GetImageSource();

                if (imgSrc != null && imgSrc.PixelHeight < imgSrc.PixelWidth)
                {
                    var wb = new WriteableBitmap(imgSrc);
                    var imgExt = wb.ToImage();
                    imgSrc = ExtendedImage.Transform(imgExt, RotationType.Rotate90, FlippingType.None).ToBitmap();
                }
                return imgSrc;
            }
        }

        public Boolean IsValid
        {
            get { return this.Id != -1; }
        }

        #endregion

        #region [Implementation]

        public void SetObject(WTObject obj)
        {
            #region [Check Argument]

            if (obj == null)
                throw new ArgumentNullException("obj");
            if (!(obj is WeTongji.Api.Domain.Activity))
                throw new ArgumentOutOfRangeException("obj");

            #endregion

            var a = obj as WeTongji.Api.Domain.Activity;

            #region [Save Extended Properties]

            #region [Avatar]

            if (this.OrganizerAvatar != a.OrganizerAvatar)
            {
                //...create a new avatar image and store in database
                if (String.IsNullOrEmpty(this.OrganizerAvatarGuid))
                {
                    var imgExt = new ImageExt()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Url = a.OrganizerAvatar
                    };

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        db.Images.InsertOnSubmit(imgExt);
                        db.SubmitChanges();
                    }

                    this.OrganizerAvatarGuid = imgExt.Id;
                }
                else
                {
                    //...delete avatar image in database
                    if (a.OrganizerAvatar.EndsWith("missing.png"))
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            var imgInDB = db.Images.Where((img) => img.Id == ImageGuid).SingleOrDefault();
                            if (imgInDB != null)
                            {
                                db.Images.DeleteOnSubmit(imgInDB);
                                db.SubmitChanges();
                            }
                        }
                    }
                    //...update avatar image url in database
                    else
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            var imgInDB = db.Images.Where((img) => img.Id == ImageGuid).SingleOrDefault();
                            if (imgInDB != null)
                            {
                                imgInDB.Url = a.OrganizerAvatar;
                                db.SubmitChanges();
                            }
                        }
                    }

                    //...delete image file in isolated storage folder
                    var store = IsolatedStorageFile.GetUserStoreForApplication();
                    var fileName = String.Format("{0}.{1}", ImageGuid, Image.GetImageFileExtension());

                    if (store.FileExists(fileName))
                        store.DeleteFile(fileName);
                }
            }

            #endregion

            #region [Image]

            if (this.ImageGuid != a.Image)
            {
                //...create a new image and store in database
                if (String.IsNullOrEmpty(this.ImageGuid))
                {
                    var imgExt = new ImageExt()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Url = a.Image
                    };

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        db.Images.InsertOnSubmit(imgExt);
                        db.SubmitChanges();
                    }

                    this.ImageGuid = imgExt.Id;
                }
                else
                {
                    //...delete image in database
                    if (a.Image.EndsWith("missing.png"))
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            var imgInDB = db.Images.Where((img) => img.Id == ImageGuid).SingleOrDefault();
                            if (imgInDB != null)
                            {
                                db.Images.DeleteOnSubmit(imgInDB);
                                db.SubmitChanges();
                            }
                        }
                    }
                    //...update image url in database
                    else
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            var imgInDB = db.Images.Where((img) => img.Id == ImageGuid).SingleOrDefault();
                            if (imgInDB != null)
                            {
                                imgInDB.Url = a.Image;
                                db.SubmitChanges();
                            }
                        }
                    }

                    //...delete image file in isolated storage folder
                    var store = IsolatedStorageFile.GetUserStoreForApplication();
                    var fileName = String.Format("{0}.{1}", ImageGuid, Image.GetImageFileExtension());

                    if (store.FileExists(fileName))
                        store.DeleteFile(fileName);
                }
            }

            #endregion

            #endregion

            #region [Save Basic Properties]

            this.Id = a.Id;
            this.Begin = a.Begin;
            this.End = a.End;
            this.Title = a.Title;
            this.Location = a.Location;
            this.Description = a.Description;
            this.Like = a.Like;
            this.CanLike = a.CanLike;
            this.Favorite = a.Favorite;
            this.CanFavorite = a.CanFavorite;
            this.Schedule = a.Schedule;
            this.CanSchedule = a.CanSchedule;
            this.Organizer = a.Organizer;
            this.OrganizerAvatar = a.OrganizerAvatar;
            this.Status = a.Status;
            this.Image = a.Image;
            this.CreatedAt = a.CreatedAt;

            #endregion
        }

        public WTObject GetObject()
        {
            var a = new WeTongji.Api.Domain.Activity();
            a.Id = this.Id;
            a.Begin = this.Begin;
            a.End = this.End;
            a.Title = this.Title;
            a.Location = this.Location;
            a.Description = this.Description;
            a.Like = this.Like;
            a.CanLike = this.CanLike;
            a.Favorite = this.Favorite;
            a.CanFavorite = this.CanFavorite;
            a.Schedule = this.Schedule;
            a.CanSchedule = this.CanSchedule;
            a.Channel_id = this.Channel_id;
            a.Organizer = this.Organizer;
            a.OrganizerAvatar = this.OrganizerAvatar;
            a.Status = this.Status;
            a.Image = this.Image;
            a.CreatedAt = this.CreatedAt;

            return a;
        }

        public Type ExpectedType() { return typeof(WeTongji.Api.Domain.Activity); }

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

        #region [Extended Functions]

        public Boolean AvatarExists()
        {
            var store = IsolatedStorageFile.GetUserStoreForApplication();

            return store.FileExists(String.Format("{0}.{1}", this.OrganizerAvatarGuid, this.OrganizerAvatar.GetImageFileExtension()));
        }

        public Boolean ImageExists()
        {
            var store = IsolatedStorageFile.GetUserStoreForApplication();

            return store.FileExists(String.Format("{0}.{1}", this.ImageGuid, this.Image.GetImageFileExtension()));
        }

        /// <summary>
        /// Save image stream of the organizer avatar of the activity.
        /// </summary>
        /// <param name="stream">the organizer avatar file stream</param>
        /// <remarks>
        /// The name of the image file created in the isolated storage file:
        /// Name = [OrganizerAvatarGuid].[OrganizerAvatar.GetImageFileExtension()]
        /// </remarks>
        public void SaveAvatar(Stream stream)
        {
            var store = IsolatedStorageFile.GetUserStoreForApplication();

            var fileName = String.Format("{0}.{1}", OrganizerAvatarGuid, OrganizerAvatar.GetImageFileExtension());

            using (var fileStream = store.OpenFile(fileName, FileMode.OpenOrCreate))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
                fileStream.Flush();
                fileStream.Close();
            }
        }

        /// <summary>
        /// Save image stream of the illustration of the activity.
        /// </summary>
        /// <param name="stream">the image file stream</param>
        /// <remarks>
        /// The name of the image file created in the isolated storage file:
        /// Name = [ImageGuid].[Image.GetImageFileExtension()]
        /// </remarks>
        public void SaveImage(Stream stream)
        {
            var store = IsolatedStorageFile.GetUserStoreForApplication();

            var fileName = String.Format("{0}.{1}", ImageGuid, Image.GetImageFileExtension());

            using (var fileStream = store.OpenFile(fileName, FileMode.OpenOrCreate))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
                fileStream.Flush();
                fileStream.Close();
            }
        }

        public static ActivityExt InvalidActivityExt
        {
            get { return new ActivityExt() { Id = -1 }; }
        }

        #endregion
    }
}
