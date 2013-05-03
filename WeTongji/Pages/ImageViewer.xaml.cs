using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Animation;
using System.Windows.Media;
using Microsoft.Xna.Framework.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.IO.IsolatedStorage;
using WeTongji.Pages;

namespace WeTongji
{
    public partial class ImageViewer : PhoneApplicationPage
    {
        #region [Fields]

        double initialScale = 1.0;

        DateTime pinchStarted = DateTime.MinValue;

        #endregion

        #region [Properties]

        /// <summary>
        /// The image name to save.
        /// </summary>
        public static String CoreImageName
        {
            private get;
            set;
        }

        /// <summary>
        /// The bitmap image source to show.
        /// </summary>
        public static BitmapSource CoreImageSource
        {
            private get;
            set;
        }

        #endregion

        #region [Construction]

        public ImageViewer()
        {
            InitializeComponent();

            var button = new ApplicationBarIconButton(new Uri("/icons/appbar.save.rest.png", UriKind.RelativeOrAbsolute))
            {
                Text = StringLibrary.ImageViewer_AppBarSaveText
            };
            button.Click += SaveImage_Button_Click;
            this.ApplicationBar.Buttons.Add(button);
        }
        #endregion

        #region [Event handlers]

        private void OnPinchStarted(object sender, PinchStartedGestureEventArgs e)
        {
            initialScale = transform.ScaleX;
            pinchStarted = DateTime.Now;
        }

        private void OnPinchDelta(object sender, PinchGestureEventArgs e)
        {
            transform.ScaleX = initialScale * e.DistanceRatio;
            transform.ScaleY = initialScale * e.DistanceRatio;
        }

        private void OnDragDelta(Object sender, DragDeltaGestureEventArgs e)
        {
            double targetX = transform.TranslateX + e.HorizontalChange;
            double targetY = transform.TranslateY + e.VerticalChange;

            var uielement = sender as UIElement;

            double minX = -uielement.RenderSize.Width / 2;
            double minY = -uielement.RenderSize.Height / 2;
            double maxX = minX + this.RenderSize.Width;
            double maxY = minY + this.RenderSize.Height;

            transform.TranslateX = Math.Max(minX, Math.Min(maxX, targetX));
            transform.TranslateY = Math.Max(minY, Math.Min(maxY, targetY));
        }

        private void OnDragCompleted(Object sender, DragCompletedGestureEventArgs e)
        {
            if (e.Direction == System.Windows.Controls.Orientation.Vertical && e.VerticalChange > 300 && e.VerticalVelocity > 4500)
            {
                StartDragDownToCloseAnimation();
            }
        }

        private void OnDoubleTap(Object sender, GestureEventArgs e)
        {
            ResetImageTransform();
        }

        private void OnPinchCompleted(Object sender, PinchGestureEventArgs e)
        {
            var ts = (DateTime.Now - pinchStarted);

            if (e.DistanceRatio < 0.52 && ts.TotalMilliseconds < 350)
            {
                StartPinchToCloseAnimation();
            }
        }

        private void SaveImage_Button_Click(Object sender, EventArgs e)
        {
            try
            {
                MediaLibrary ml = new MediaLibrary();
                var wb = new WriteableBitmap(CoreImageSource);

                using (var stream = new MemoryStream())
                {
                    wb.SaveJpeg(stream, wb.PixelWidth, wb.PixelHeight, 0, 100);
                    stream.Seek(0, SeekOrigin.Begin);
                    ml.SavePicture(CoreImageName, stream);

                    MessageBox.Show(StringLibrary.ImageViewer_SaveSucceeded, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                }
            }
            catch
            {
                MessageBox.Show(StringLibrary.ImageViewer_SaveFailed, StringLibrary.Common_Prompt, MessageBoxButton.OK);
            }
        }

        private void GoBack(Object sender, EventArgs e)
        {
            this.NavigationService.GoBack();
        }

        #endregion

        #region [Util functions]

        private void ResetImageTransform()
        {
            initialScale = transform.ScaleX = transform.ScaleY = 1;
            transform.TranslateX = transform.TranslateY = 0;
            pinchStarted = DateTime.MinValue;
        }

        #endregion

        #region [Animation]

        private void StartPinchToCloseAnimation()
        {
            this.IsHitTestVisible = false;
            var sb = this.Resources["PinchToClose"] as Storyboard;
            ((sb.Children[0] as DoubleAnimationUsingKeyFrames).KeyFrames[0] as EasingDoubleKeyFrame).Value = transform.ScaleX;
            ((sb.Children[1] as DoubleAnimationUsingKeyFrames).KeyFrames[0] as EasingDoubleKeyFrame).Value = transform.ScaleY;

            sb.Begin();
        }

        private void StartDragDownToCloseAnimation()
        {
            this.IsHitTestVisible = false;

            var sb = this.Resources["PinchToClose"] as Storyboard;
            ((sb.Children[0] as DoubleAnimationUsingKeyFrames).KeyFrames[0] as EasingDoubleKeyFrame).Value = transform.TranslateY;
            ((sb.Children[0] as DoubleAnimationUsingKeyFrames).KeyFrames[1] as EasingDoubleKeyFrame).Value = this.RenderSize.Height - image.TransformToVisual(null).Transform(new Point()).Y;

            sb.Begin();
        }

        #endregion

        #region [Overridden]

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            CoreImageSource = null;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                image.Source = CoreImageSource;
                ResetImageTransform();
            }
            else
            {
                CoreImageSource = image.Source as BitmapSource;
            }
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            base.OnOrientationChanged(e);

            ResetImageTransform();
        }

        #endregion
    }
}