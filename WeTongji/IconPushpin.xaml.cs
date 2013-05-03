using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WeTongji
{
    public partial class IconPushpin : Grid
    {
        public IconPushpin()
        {
            InitializeComponent();
        }

        #region [Event handlers]

        public event EventHandler Opened
        {
            add
            {
                (this.Resources["Open"] as Storyboard).Completed += value;
            }
            remove
            {
                (this.Resources["Open"] as Storyboard).Completed -= value;
            }
        }

        public event EventHandler Closed
        {
            add
            {
                (this.Resources["Close"] as Storyboard).Completed += value;
            }
            remove
            {
                (this.Resources["Close"] as Storyboard).Completed -= value;
            }
        }

        /// <summary>
        /// Icon tapped event handler
        /// </summary>
        public event EventHandler<System.Windows.Input.GestureEventArgs> IconTapped
        {
            add
            {
                image.Tap += value;
            }
            remove
            {
                image.Tap -= value;
            }
        }

        #endregion

        #region [Properties]

        public Boolean IsOpened
        {
            get;
            set;
        }

        #endregion

        #region [Public functions]

        public void Open()
        {
            (this.Resources["Open"] as Storyboard).Begin();
        }

        public void Close()
        {
            (this.Resources["Close"] as Storyboard).Begin();
        }

        #endregion

        #region [Private functions]

        private void IconOpened(Object sender, EventArgs e)
        {
            IsOpened = true;
        }

        private void IconClosed(Object sender, EventArgs e)
        {
            IsOpened = false;
        }

        #endregion

    }
}