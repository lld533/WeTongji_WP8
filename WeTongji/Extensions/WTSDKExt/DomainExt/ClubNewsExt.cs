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
    [Table(Name = "ClubNews")]
    public class ClubNewsExt : IWTObjectExt, ICampusInfo, INotifyPropertyChanged
    {
        #region [Basic Properties]

        [Column()]
        public String Organizer { get; set; }

        [Column()]
        public String OrganizerAvatar { get; set; }

        [Column(IsPrimaryKey = true)]
        public int Id { get; set; }

        [Column()]
        public String Title { get; set; }

        [Column()]
        public String Source { get; set; }

        [Column()]
        public String Summary { get; set; }

        [Column()]
        public String Context { get; set; }

        [Column()]
        public DateTime CreatedAt { get; set; }

        [Column()]
        public int Read { get; set; }

        [Column()]
        public int Like { get; set; }

        [Column()]
        public bool CanLike { get; set; }

        [Column()]
        public int Favorite { get; set; }

        [Column()]
        public bool CanFavorite { get; set; }


        #endregion

        #region [Extended Properties]

        [Column()]
        public String ImageExtList { get; set; }

        [Column()]
        public String OrganizerAvatarGuid { get; set; }

        public String DisplaySummary
        {
            get 
            {
                if (String.IsNullOrEmpty(Summary))
                {
                    var subStr = this.Context.Substring(0, 20);
                    return subStr.Split("\r\t".ToCharArray()).FirstOrDefault();
                }
                else
                    return Summary;
            }
        }

        #endregion

        #region [Implementation]

        public Type ExpectedType()
        {
            return typeof(WeTongji.Api.Domain.ClubNews);
        }

        public void SetObject(WTObject obj)
        {
            #region [Check Argument]

            if (obj == null)
                throw new ArgumentNullException("obj");
            if (!(obj is WeTongji.Api.Domain.ClubNews))
                throw new ArgumentOutOfRangeException("obj");

            #endregion

            var news = obj as WeTongji.Api.Domain.ClubNews;

            #region [Save Extended Property]

            #region [Save organizer avatar image]

            if (this.OrganizerAvatar != news.OrganizerAvatar)
            {
                if (String.IsNullOrEmpty(this.OrganizerAvatar))
                {
                    var titleImg = new ImageExt()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Url = news.OrganizerAvatar
                    };
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        db.Images.InsertOnSubmit(titleImg);
                        db.SubmitChanges();
                    }
                    OrganizerAvatarGuid = titleImg.Id;
                }
                else
                {
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        var imgInDB = db.Images.Where((img) => img.Id == this.OrganizerAvatarGuid).SingleOrDefault();

                        if (imgInDB != null)
                        {
                            imgInDB.Url = news.OrganizerAvatar;
                            db.SubmitChanges();
                        }
                    }

                    var store = IsolatedStorageFile.GetUserStoreForApplication();
                    var fileName = String.Format("{0}.{1}", OrganizerAvatarGuid, news.OrganizerAvatar.GetImageFileExtension());

                    if (store.FileExists(fileName))
                        store.DeleteFile(fileName);

                    OrganizerAvatarGuid = String.Empty;
                }

            }

            #endregion

            #region [Save Images]

            if (news.Images.Count() > 0)
            {
                //...Save images if all images are not stored.
                if (String.IsNullOrEmpty(ImageExtList))
                {
                    StringBuilder sb = new StringBuilder();

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        foreach (var img in news.Images)
                        {
                            var imgExt = new ImageExt(img) { Id = Guid.NewGuid().ToString() };

                            sb.AppendFormat("\"{0}\":\"{1}\";", imgExt.Id, imgExt.Url.GetImageFileExtension());

                            db.Images.InsertOnSubmit(imgExt);
                        }

                        db.SubmitChanges();
                    }

                    ImageExtList = sb.ToString(0, sb.Length - 1);
                }
                else
                {
                    //...do nothing if previous images exist.
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(ImageExtList))
                {
                    var kvpairs = ImageExtList.Split(';');

                    var store = IsolatedStorageFile.GetUserStoreForApplication();

                    foreach (var pair in kvpairs)
                    {
                        var imgkv = pair.Split(':');
                        var imgId = imgkv[0].Trim('\"');
                        var imgExt = imgkv[1].Trim('\"');

                        using (var db = WTShareDataContext.ShareDB)
                        {
                            var imgInDB = db.Images.Where((img) => img.Id == imgId).SingleOrDefault();
                            if (imgInDB != null)
                            {
                                db.Images.DeleteOnSubmit(imgInDB);
                                db.SubmitChanges();
                            }
                        }

                        var fileName = String.Format("{0}.{1}", imgId, imgExt);

                        if (store.FileExists(fileName))
                        {
                            store.DeleteFile(fileName);
                        }
                    }

                    ImageExtList = String.Empty;
                }
            }

            #endregion

            #endregion

            #region [Save Basic Properties]

            this.Id = news.Id;
            this.Title = news.Title;
            this.Source = news.Source;
            this.Summary = news.Summary;
            this.Context = news.Context;
            this.CreatedAt = news.CreatedAt;
            this.Read = news.Read;
            this.Like = news.Like;
            this.CanLike = news.CanLike;
            this.Favorite = news.Favorite;
            this.CanFavorite = news.CanFavorite;
            this.Organizer = news.Organizer;
            this.OrganizerAvatar = news.OrganizerAvatar;

            #endregion
        }

        public WTObject GetObject()
        {
            var cn = new WeTongji.Api.Domain.ClubNews();

            cn.Id = this.Id;
            cn.Title = this.Title;
            cn.Source = this.Source;
            cn.Summary = this.Summary;
            cn.Context = this.Context;
            cn.CreatedAt = this.CreatedAt;
            cn.Read = this.Read;
            cn.Like = this.Like;
            cn.CanLike = this.CanLike;
            cn.Favorite = this.Favorite;
            cn.CanFavorite = this.CanFavorite;
            cn.Organizer = this.Organizer;
            cn.OrganizerAvatar = this.OrganizerAvatar;

            if (String.IsNullOrEmpty(ImageExtList))
            {
                cn.Images = new String[0];
            }
            else
            {
                var imgs = ImageExtList.Split(';');
                for (int i = 0; i < imgs.Count(); ++i)
                {
                    var tmp = imgs[i].Split(':').First();
                    imgs[i] = tmp.Trim('\"');
                }
                cn.Images = imgs;
            }

            return cn;
        }

        public ImageSource CampusInfoImageBrush
        {
            get { return FirstImageBrush; }
        }

        public String CampusInfoImageUrl
        {
            get
            {
                try
                {
                    var guid = ImageExtList.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).First().Split(':').First().Trim('\"');
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        var img = db.Images.Where((image) => image.Id == guid).SingleOrDefault();
                        if (img != null)
                            return img.Url;
                        return String.Empty;
                    }
                }
                catch
                {
                    return String.Empty;
                }

            }
        }

        public String CampusInfoImageFileName
        {
            get { return ImageExtList.GetImageFilesNames().First() + "." + CampusInfoImageUrl.GetImageFileExtension(); }
        }

        public Boolean CampusInfoImageExists { get { return ImageExists(); } }

        public void SaveCampusInfoImage(Stream stream)
        {
            SaveImage(stream);
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

        public ClubNewsExt Clone()
        {
            return this.MemberwiseClone() as ClubNewsExt;
        }

        public Boolean ImageExists(int index = 0)
        {
            try
            {
                var imgKVs = ImageExtList.Split(';');
                var imgKV = imgKVs[index].Split(':');
                var fileName = String.Format("{0}.{1}", imgKV[0].Trim('\"'), imgKV[1].Trim('\"'));

                var store = IsolatedStorageFile.GetUserStoreForApplication();
                return store.FileExists(fileName);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Save an image stream.
        /// </summary>
        /// <param name="stream">image stream</param>
        /// <param name="index">
        /// The zero-based index of Person's images. By default, the value is 0.
        /// </param>
        public void SaveImage(Stream stream, int index = 0)
        {
            try
            {
                var imgKVs = ImageExtList.Split(';');
                var imgKV = imgKVs[index].Split(':');
                var fileName = String.Format("{0}.{1}", imgKV[0].Trim('\"'), imgKV[1].Trim('\"'));

                var store = IsolatedStorageFile.GetUserStoreForApplication();
                using (var fileStream = store.CreateFile(fileName))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                    fileStream.Flush();
                    fileStream.Close();
                }
            }
            catch { }
        }

        public void SaveAvatar(Stream stream)
        {
            try
            {
                stream.Seek(0, SeekOrigin.Begin);
                var store = IsolatedStorageFile.GetUserStoreForApplication();
                using (var fileStream = store.CreateFile(String.Format("{0}.{1}", OrganizerAvatarGuid, OrganizerAvatar.GetImageFileExtension())))
                {
                    stream.CopyTo(fileStream);
                    fileStream.Flush();
                    fileStream.Close();
                }
            }
            catch { }
        }

        public IEnumerable<ImageExt> GetImageExts()
        {
            var result = new ObservableCollection<ImageExt>();

            if (!String.IsNullOrEmpty(ImageExtList))
            {
                var imgList = ImageExtList.Split(';');

                using (var db = WTShareDataContext.ShareDB)
                {
                    foreach (var img in imgList)
                    {
                        var guid = img.Split(':').First().Trim('\"');
                        var target = db.Images.Where((dbImg) => dbImg.Id == guid).SingleOrDefault();

                        if (target != null)
                            result.Add(target);
                    }
                }
            }
            return result;
        }

        public IEnumerable<String> GetImagesURL()
        {
            var Images = new List<String>();

            if (!String.IsNullOrEmpty(ImageExtList))
            {
                var imgList = ImageExtList.Split(';');

                using (var db = WTShareDataContext.ShareDB)
                {
                    foreach (var img in imgList)
                    {
                        var guid = img.Split(':').First().Trim('\"');
                        var target = db.Images.Where((dbImg) => dbImg.Id == guid).SingleOrDefault();

                        if (target != null)
                            Images.Add(target.Url);
                    }
                }
            }

            return Images;
        }

        public Boolean AvatarExists()
        {
            var store = IsolatedStorageFile.GetUserStoreForApplication();

            return store.FileExists(String.Format("{0}.{1}", this.OrganizerAvatarGuid, this.OrganizerAvatar.GetImageFileExtension()));
        }

        public static ClubNewsExt InvalidClubNews()
        {
            return new ClubNewsExt() { Id = int.MinValue };
        }

        #endregion

        #region [Data Binding]

        public ImageSource AvatarImageBrush
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

        public ImageSource FirstImageBrush
        {
            get
            {
                if (String.IsNullOrEmpty(ImageExtList))
                    return null;

                var imgKV = ImageExtList.Split(';').FirstOrDefault();
                if (String.IsNullOrEmpty(imgKV))
                    return null;

                var fileKV = imgKV.Split(':');

                var fileName = String.Format("{0}.{1}", fileKV[0].Trim('\"'), fileKV[1].Trim('\"'));

                return fileName.GetImageSource();
            }
        }

        public Boolean IsIllustrated
        {
            get { return !String.IsNullOrEmpty(ImageExtList); }
        }

        /// <summary>
        /// 5分钟前 or 5小时前 or 10:20:05 or 2013/02/05
        /// </summary>
        public String DisplayCreationTime
        {
            get
            {
                //...Todo @_@ Localizable

                if (DateTime.Now < CreatedAt)
                {
                    return String.Format(StringLibrary.Common_JustNow);
                }

                var span = DateTime.Now - CreatedAt;

                if (span < TimeSpan.FromHours(1))
                {
                    return String.Format(StringLibrary.Common_WithinOneHourTemplate, (int)span.TotalMinutes);
                }
                else if (span < TimeSpan.FromHours(2))
                {
                    return String.Format(StringLibrary.Common_WithinTwoHours, (int)span.TotalHours);
                }
                else if (span < TimeSpan.FromHours(6))
                {
                    return String.Format(StringLibrary.Common_WithinSixHoursTemplate, (int)span.TotalHours);
                }
                else if (CreatedAt.Date == DateTime.Now.Date)
                {
                    return CreatedAt.ToString("HH:mm:ss");
                }
                else
                    return CreatedAt.ToString("yyyy/MM/dd");
            }
        }

        /// <summary>
        /// 2013/02/03 20:18
        /// </summary>
        public String FullDisplayCreationTime
        {
            get
            {
                return CreatedAt.ToString("yyyy/MM/dd hh:mm");
            }
        }

        public Boolean IsInvalidClubNews
        {
            get { return Id == int.MinValue; }
        }

        #endregion
    }
}
