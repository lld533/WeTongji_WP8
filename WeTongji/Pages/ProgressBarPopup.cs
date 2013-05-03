using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WeTongji.Pages
{
    public class ProgressBarPopup
    {
        #region [Instance]

        private static Popup popup = null;

        private static ProgressBarPopup instance = null;

        public static ProgressBarPopup Instance
        {
            get
            {
                if (instance == null)
                    instance = new ProgressBarPopup();
                return instance;
            }
        }
        #endregion

        #region [Constructor]

        private ProgressBarPopup()
        {
            popup = new Popup();
            popup.Child = new ProgressBar()
            {
                Style = App.Current.Resources["WTProgressBarStyle"] as Style,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 64, 164, 227)),
                Width = App.Current.RootVisual.RenderSize.Width,
                Height = 90,
                Margin = new Thickness(-20, -10, -20, 0)
            };
        }

        #endregion

        #region [Functions]

        public void Open()
        {
            popup.IsOpen = true;
            (popup.Child as ProgressBar).IsIndeterminate = true;
        }

        public void Close()
        {
            popup.IsOpen = false;
            (popup.Child as ProgressBar).IsIndeterminate = false;
        }

        #endregion

        #region [Property]

        public Boolean IsOpen
        {
            get { return popup.IsOpen; }
        }

        #endregion
    }
}
