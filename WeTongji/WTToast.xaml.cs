using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Media;

namespace WeTongji
{
    public sealed partial class WTToast : Grid
    {
        #region [Fields]

        private DateTime beginTime = DateTime.MinValue;
        private DispatcherTimer dt;

        #endregion

        public static readonly TimeSpan OpeningInterval = TimeSpan.FromSeconds(2);

        private WTToast()
        {
            InitializeComponent();

            grid.Width = Application.Current.RootVisual.RenderSize.Width;
            (((this.Resources["FlyoutToast"] as Storyboard).Children[0] as DoubleAnimationUsingKeyFrames).KeyFrames[1].Value) = grid.Width;

            dt = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(50) };
            dt.Tick += TickToClose;
        }

        #region [Singleton]

        private static WTToast instance = null;

        public static WTToast Instance
        {
            get
            {
                return instance == null ? (instance = new WTToast()) : instance;
            }
        }

        #endregion

        public void Show(String txt)
        {
            Storyboard flyoutToast = this.Resources["FlyoutToast"] as Storyboard;

            if (flyoutToast.GetCurrentState() == ClockState.Active)
            {
                flyoutToast.SkipToFill();
                flyoutToast.Stop();
            }

            this.TextBlock_Core.Text = txt;
            (this.Resources["OpenToast"] as Storyboard).Begin();
        }

        #region [Event handlers]

        private void TickToClose(Object sender, EventArgs e)
        {
            if (DateTime.Now - beginTime >= OpeningInterval)
            {
                dt.Stop();
                grid.IsHitTestVisible = false;
                (this.Resources["CloseToast"] as Storyboard).Begin();
            }
        }

        private void OpenToastCompleted(Object sender, EventArgs e)
        {
            beginTime = DateTime.Now;
            grid.IsHitTestVisible = true;
            dt.Start();
        }

        private void Toast_MouseLeftButtonDown(Object sender, MouseButtonEventArgs e)
        {
            var pnt = e.GetPosition(grid);

            //...Tap on the right side
            if (pnt.X > grid.RenderSize.Width / 2)
            {
                (this.Resources["TapOnRight"] as Storyboard).Begin();
            }
            //...Tap on the left side
            else
            {
                (this.Resources["TapOnLeft"] as Storyboard).Begin();
            }
        }

        private void Toast_MouseLeftButtonUp(Object sender, MouseButtonEventArgs e)
        {
            (this.Resources["ResetTextblockProjection"] as Storyboard).Begin();
        }

        private void Toast_MouseLeave(Object sender, MouseEventArgs e)
        {
            (this.Resources["ResetTextblockProjection"] as Storyboard).Begin();
        }

        private void Toast_ManipulationCompleted(Object sender, ManipulationCompletedEventArgs e)
        {
            if (Math.Abs(e.TotalManipulation.Translation.X) > 200)
            {
                dt.Stop();
                e.Handled = true;

                //...Stop at the manipulation end point
                (((this.Resources["FlyoutToast"] as Storyboard).Children[0] as DoubleAnimationUsingKeyFrames).KeyFrames[1].Value) = e.TotalManipulation.Translation.X > 0 ? grid.Width : -grid.Width;

                if ((grid.RenderTransform as CompositeTransform).TranslateX == 0)
                {
                    grid.IsHitTestVisible = false;
                    (this.Resources["FlyoutToast"] as Storyboard).Begin();
                }
            }
            else
            {
                beginTime = DateTime.Now;
                dt.Start();
            }
        }

        private void FlyoutToastCompleted(Object sender, EventArgs e)
        {
            popup.IsOpen = false;
            (this.Resources["ResetTextblockProjection"] as Storyboard).Begin();
            (grid.RenderTransform as CompositeTransform).TranslateX = 0;
        }

        #endregion

    }
}