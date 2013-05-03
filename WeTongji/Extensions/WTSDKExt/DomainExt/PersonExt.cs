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
    [Table(Name = "Person")]
    public class PersonExt : IWTObjectExt, INotifyPropertyChanged
    {
        #region [Basic Properties]

        [Column(IsPrimaryKey = true)]
        public int Id { get; set; }

        [Column()]
        public String Name { get; set; }

        [Column()]
        public String JobTitle { get; set; }

        [Column()]
        public String Words { get; set; }

        [Column()]
        public String NO { get; set; }

        [Column()]
        public String Avatar { get; set; }

        [Column()]
        public String StudentNO { get; set; }

        [Column()]
        public String Description { get; set; }

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

        /// <summary>
        /// "[Guid(0)]":"[FileExt(0)]";"[Guid(1)]":"[FileExt(1)]";...."[Guid(n)]":"[FileExt(n)]"
        /// where n = number of images
        /// </summary>
        [Column()]
        public String ImageExtList { get; set; }

        [Column()]
        public String AvatarGuid { get; set; }

        #endregion

        #region [Extended Methods]

        public PersonExt Clone()
        {
            return this.MemberwiseClone() as PersonExt;
        }

        /// <summary>
        /// Key: Url
        /// Value: Description
        /// </summary>
        /// <returns></returns>
        public Dictionary<String, String> GetImages()
        {
            var Images = new Dictionary<String, String>();

            var imgList = ImageExtList.Split(';');

            using (var db = WTShareDataContext.ShareDB)
            {
                foreach (var img in imgList)
                {
                    var guid = img.Split(':').First().Trim('\"');
                    var target = db.Images.Where((dbImg) => dbImg.Id == guid).SingleOrDefault();

                    if (target != null)
                        Images[target.Url] = target.Description;
                }
            }


            return Images;
        }

        public Boolean AvatarExists()
        {
            var store = IsolatedStorageFile.GetUserStoreForApplication();

            return store.FileExists(String.Format("{0}.{1}", this.AvatarGuid, this.Avatar.GetImageFileExtension()));
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

        public void SaveAvatar(Stream stream)
        {
            try
            {
                stream.Seek(0, SeekOrigin.Begin);
                var store = IsolatedStorageFile.GetUserStoreForApplication();
                using (var fileStream = store.CreateFile(String.Format("{0}.{1}", AvatarGuid, Avatar.GetImageFileExtension())))
                {
                    stream.CopyTo(fileStream);
                    fileStream.Flush();
                    fileStream.Close();
                }
            }
            catch { }
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

        public IEnumerable<ImageExt> GetImageExts()
        {
            var result = new ObservableCollection<ImageExt>();

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

            return result;
        }

        #endregion

        #region [Data Binding]

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

        public IEnumerable<ImageSource> ImageBrushList
        {
            get
            {
                if (String.IsNullOrEmpty(ImageExtList))
                    return null;

                var imgKVs = ImageExtList.Split(';');

                var result = new ObservableCollection<ImageSource>();

                foreach (var imgKV in imgKVs)
                {
                    var fileKV = imgKV.Split(':');

                    var fileName = String.Format("{0}.{1}", fileKV[0].Trim('\"'), fileKV[1].Trim('\"'));

                    var imgSrc = fileName.GetImageSource();

                    if (imgSrc != null)
                        result.Add(imgSrc);
                }

                return result;
            }
        }

        #endregion

        #region [Implementation]

        public WTObject GetObject()
        {
            var p = new WeTongji.Api.Domain.Person();

            p.Id = this.Id;
            p.Name = this.Name;
            p.JobTitle = this.JobTitle;
            p.Words = this.Words;
            p.NO = this.NO;
            p.Avatar = this.Avatar;
            p.StudentNO = this.StudentNO;
            p.Images = GetImages();
            p.Description = this.Description;
            p.Read = this.Read;
            p.Like = this.Like;
            p.CanLike = this.CanLike;
            p.Favorite = this.Favorite;
            p.CanFavorite = this.CanFavorite;

            return p;
        }

        public void SetObject(WTObject obj)
        {
            #region [Check argument]

            if (obj == null)
                throw new ArgumentNullException("obj");

            if (!(obj is WeTongji.Api.Domain.Person))
            {
                throw new ArgumentOutOfRangeException("obj");
            }

            #endregion

            var p = obj as WeTongji.Api.Domain.Person;

            #region [Save Images]

            if (p.Images.Count() > 0)
            {
                #region [Make ImageExt List]

                //...No image stored in the database.
                if (String.IsNullOrEmpty(ImageExtList))
                {
                    var imgList = new ImageExt[p.Images.Count()];
                    int i = 0;
                    StringBuilder sb = new StringBuilder();

                    foreach (var kvp in p.Images)
                    {
                        imgList[i] = new WeTongji.Api.Domain.ImageExt(kvp.Key, kvp.Value)
                        {
                            Id = Guid.NewGuid().ToString()
                        };
                        sb.AppendFormat("\"{0}\":\"{1}\";", imgList[i].Id, imgList[i].Url.GetImageFileExtension());
                        ++i;
                    }

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        db.Images.InsertAllOnSubmit(imgList);
                        db.SubmitChanges();
                    }

                    ImageExtList = sb.ToString().TrimEnd(';');
                }
                else
                {
                    //...Do nothing if images are stored in database.
                }

                #endregion
            }
            else
            {
                if (!String.IsNullOrEmpty(ImageExtList))
                {
                    //...Delete images in database and isolated storage folder.
                    var kvpairs = ImageExtList.Split(';');

                    var store = IsolatedStorageFile.GetUserStoreForApplication();

                    foreach (var pair in kvpairs)
                    {
                        var IdUrlpair = pair.Split(':');
                        var imgId = IdUrlpair[0].Trim('\"');
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            var imgInDB = db.Images.Where((img) => img.Id == imgId).SingleOrDefault();
                            if (imgInDB != null)
                            {
                                db.Images.DeleteOnSubmit(imgInDB);
                                db.SubmitChanges();
                            }
                        }

                        var fileName = String.Format("{0}.{1}", imgId, IdUrlpair[1].GetImageFileExtension());

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);
                    }
                }
            }

            #region [Save Avatar]

            if(String.IsNullOrEmpty(AvatarGuid))
            {
                var avatarImg = new ImageExt() { Id = Guid.NewGuid().ToString() };
                using (var db = WTShareDataContext.ShareDB)
                {
                    db.Images.InsertOnSubmit(avatarImg);
                    db.SubmitChanges();
                }
                AvatarGuid = avatarImg.Id;
            }

            #endregion

            #endregion

            #region [Save Basic Properties]

            this.Id = p.Id;
            this.Name = p.Name;
            this.JobTitle = p.JobTitle;
            this.Words = p.Words;
            this.NO = p.NO;
            this.Avatar = p.Avatar;
            this.StudentNO = p.StudentNO;
            this.Description = p.Description;
            this.Read = p.Read;
            this.Like = p.Like;
            this.CanLike = p.CanLike;
            this.Favorite = p.Favorite;
            this.CanFavorite = p.CanFavorite;

            #endregion
        }

        public Type ExpectedType() { return typeof(WeTongji.Api.Domain.Person); }

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

        #region [Constructor]

        public PersonExt() { }

        public PersonExt(Person p) { SetObject(p); }

        #endregion
    }
}
