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
    public partial class BillboardPushpin : Grid
    {
        public BillboardPushpin()
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

        public event EventHandler<System.Windows.Input.GestureEventArgs> BillboardTapped
        {
            add
            {
                ShadowedBillboard.Tap += value;
            }
            remove
            {
                ShadowedBillboard.Tap -= value;
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

        private void ShadowedBillboardOpened(Object sender, EventArgs e)
        {
            IsOpened = true;
        }

        private void ShadowedBillboardClosed(Object sender, EventArgs e)
        {
            IsOpened = false;
        }

        #endregion
    }
}