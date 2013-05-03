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
    [Table(Name = "Channel")]
    public class ChannelExt : IWTObjectExt, INotifyPropertyChanged
    {
        #region [Basic Properties]

        [Column(IsPrimaryKey = true)]
        public int Id { get; set; }

        [Column()]
        public String Title { get; set; }

        [Column()]
        public String Image { get; set; }

        [Column()]
        public int Follow { get; set; }

        [Column()]
        public String Description { get; set; }

        #endregion

        #region [Extended Property]

        [Column()]
        public String ImageGuid { get; set; }

        #endregion

        #region [Implementation]

        public WTObject GetObject()
        {
            var c = new WeTongji.Api.Domain.Channel();

            c.Id = this.Id;
            c.Title = this.Title;
            c.Image = this.Image;
            c.Follow = this.Follow;
            c.Description = this.Description;

            return c;
        }

        public void SetObject(WTObject obj)
        {
            #region [Check Argument]

            if (obj == null)
                throw new ArgumentNullException("obj");

            if (!(obj is WeTongji.Api.Domain.Channel))
                throw new ArgumentOutOfRangeException("obj");

            #endregion

            var c = obj as WeTongji.Api.Domain.Channel;

            #region [Save Basic Properties]

            this.Id = c.Id;
            this.Title = c.Title;
            this.Image = c.Image;
            this.Follow = c.Follow;
            this.Description = c.Description;

            #endregion

            #region [Save Extended Property]

            var img = new ImageExt() { Url = Image };
            using (var db = WTShareDataContext.ShareDB)
            {
                db.Images.InsertOnSubmit(img);
                db.SubmitChanges();
            }
            ImageGuid = img.Id;

            #endregion
        }

        public Type ExpectedType() { return typeof(WeTongji.Api.Domain.Channel); }

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
    }
}
