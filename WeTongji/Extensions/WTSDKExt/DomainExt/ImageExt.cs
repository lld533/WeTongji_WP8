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
    [Table(Name = "Image")]
    public class ImageExt : IWTObjectExt, INotifyPropertyChanged
    {
        #region [Properties]

        [Column(IsPrimaryKey = true)]
        public String Id { get; set; }

        [Column]
        public String Description { get; set; }

        [Column]
        public String Url { get; set; }

        #endregion

        #region [Implementation]

        /// <summary>
        /// Throw NotImplementedException
        /// </summary>
        /// <param name="obj"></param>
        public void SetObject(WTObject obj) { throw new NotImplementedException(); }

        /// <summary>
        /// Throw NotImplementedException
        /// </summary>
        /// <returns></returns>
        public WTObject GetObject() { throw new NotImplementedException(); }

        /// <summary>
        /// Throw NotImplementedException
        /// </summary>
        /// <returns></returns>
        public Type ExpectedType() { throw new NotImplementedException(); }

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

        public ImageSource ImageBrush
        {
            get
            {
                return String.Format("{0}.{1}", Id, Url.GetImageFileExtension()).GetImageSource();
            }
        }

        #endregion

        #region [Constructor]

        public ImageExt()
        {
        }

        public ImageExt(String url, String des = "")
        {
            Url = url;
            Description = des;
        }

        #endregion
    }
}
