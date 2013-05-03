using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Navigation;
using WeTongji.Api.Request;
using WeTongji.Api.Response;
using WeTongji.Api;

namespace WeTongji
{
    public partial class SignUp : PhoneApplicationPage
    {
        #region [Constructor]

        public SignUp()
        {
            InitializeComponent();

            var btn = new ApplicationBarIconButton(new Uri("/icons/appbar.check.rest.png", UriKind.RelativeOrAbsolute))
            {
                Text = StringLibrary.SignUp_AppBarOkText,
                IsEnabled = false
            };
            btn.Click += Button_Done_Click;
            this.ApplicationBar.Buttons.Add(btn);
        }

        #endregion

        #region [Properties]

        private ApplicationBarIconButton Button_Send
        {
            get { return this.ApplicationBar.Buttons[0] as ApplicationBarIconButton; }
        }

        #endregion

        #region [Functions]

        #region [Api]

        private void Button_Done_Click(Object sender, EventArgs e)
        {
            var req = new UserActiveRequest<WTResponse>();
            var client = new WTDefaultClient<WTResponse>();

            req.NO = TextBox_Id.Text;
            req.Name = TextBox_Name.Text;
            req.Password = PasswordBox_Password.Password;

            client.ExecuteCompleted += (obj, args) =>
            {
                #region [Flurry]
#if Flurry
                FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickDoneToSubmitSignUpInfo).ToString());
#endif
                #endregion

                this.Dispatcher.BeginInvoke(() =>
                {
                    this.NavigationService.Navigate(new Uri("/Pages/SignUpSuccess.xaml", UriKind.RelativeOrAbsolute));
                });
            };

            client.ExecuteFailed += (obj, args) =>
                {
                    if (args.Error is WTException)
                    {
                        var err = args.Error as WTException;
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            switch (err.StatusCode.Id)
                            {
                                case Api.Util.Status.AlreadyRegistered:
                                    MessageBox.Show(StringLibrary.SignUp_AlreadyRegisteredPrompt, StringLibrary.Common_Prompt,MessageBoxButton.OK);
                                    this.NavigationService.RemoveBackEntry();
                                    this.NavigationService.GoBack();
                                    return;
                                case Api.Util.Status.NoAccount:
                                    MessageBox.Show(StringLibrary.SignUp_NoAccountPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                    return;
                                case Api.Util.Status.IdNameDismatch:
                                    MessageBox.Show(StringLibrary.SignUp_IdNameDismatchPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                    TextBox_Id.Focus();
                                    TextBox_Id.SelectAll();
                                    return;
                                case Api.Util.Status.InvalidParameter:
                                    MessageBox.Show(StringLibrary.SignUp_BadSignUpInfo, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                    TextBox_Id.Focus();
                                    TextBox_Id.SelectAll();
                                    return;
                                default:
                                    MessageBox.Show(StringLibrary.Common_FailurePrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                    return;
                            }
                        });

                    }
                    else if (args.Error is System.Net.WebException)
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            WTToast.Instance.Show(StringLibrary.Toast_NetworkErrorPrompt);
                        });
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            MessageBox.Show(StringLibrary.Common_FailurePrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                        });
                    }
                };


            client.Execute(req);
        }

        #endregion

        #region [Nav]

        private void Button_ViewAgreement_Click(Object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Pages/Agreement.xaml", UriKind.RelativeOrAbsolute));
        }

        #endregion

        #region [Visual]

        private void UpdateSendButton(Object sender, RoutedEventArgs e)
        {
            UpdateSendButtonCore(null, null);
        }

        private void IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateSendButtonCore(null, null);
        }

        private void UpdateSendButtonCore(Object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrEmpty(TextBox_Id.Text) &&
                !String.IsNullOrEmpty(TextBox_Name.Text) &&
                !String.IsNullOrEmpty(PasswordBox_Password.Password) &&
                !String.IsNullOrEmpty(PasswordBox_Confirm.Password) &&
                (Boolean)CheckBox_Agreement.IsChecked)
            {
                Button_Send.IsEnabled = true;
            }
            else
            {
                Button_Send.IsEnabled = false;
            }
        }

        private void CoreGotFocus(Object sender, RoutedEventArgs e)
        {
            var visualElment = sender as UIElement;
            var parentToScrollTo = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(visualElment)) as UIElement;
            var verticalOffset = parentToScrollTo.TransformToVisual(StackPanel_Root).Transform(new Point()).Y;

            this.Border_BottomPlaceHolder.Height = StackPanel_Root.RenderSize.Height - verticalOffset - parentToScrollTo.RenderSize.Height;

            this.Border_BottomPlaceHolder.Visibility = Visibility.Visible;

            ScrollViewer_Root.ScrollToVerticalOffset(Math.Min(verticalOffset, ScrollViewer_Root.ScrollableHeight));
        }

        private void CoreLostFocus(Object sender, RoutedEventArgs e)
        {
            this.Border_BottomPlaceHolder.Visibility = Visibility.Collapsed;

            ScrollViewer_Root.ScrollToVerticalOffset(0);
        }

        #endregion

        #endregion
    }
}