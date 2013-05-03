using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WeTongji.Api.Request;
using WeTongji.Api.Response;
using WeTongji.Api;
using WeTongji.Business;

namespace WeTongji
{
    public partial class UpdatePassword : PhoneApplicationPage
    {
        public UpdatePassword()
        {
            InitializeComponent();

            var btn = new ApplicationBarIconButton(new Uri("/icons/appbar.check.rest.png", UriKind.RelativeOrAbsolute))
                        {
                            IsEnabled = false,
                            Text = StringLibrary.UpdatePassword_AppBarSendText
                        };
            btn.Click += Button_Send_Click;
            this.ApplicationBar.Buttons.Add(btn);
        }

        private void UpdateSendButton(Object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(PasswordBox_Old.Password) && !String.IsNullOrEmpty(PasswordBox_New.Password) && !String.IsNullOrEmpty(PasswordBox_Repeat.Password))
            {
                var btn = this.ApplicationBar.Buttons[0] as ApplicationBarIconButton;
                btn.IsEnabled = true;
            }
            else
            {
                var btn = this.ApplicationBar.Buttons[0] as ApplicationBarIconButton;
                btn.IsEnabled = false;
            }
        }

        private void Button_Send_Click(Object sender, EventArgs e)
        {
            //...Hide Software Input Panel
            this.Focus();

            if (PasswordBox_New.Password != PasswordBox_Repeat.Password)
            {
                MessageBox.Show(StringLibrary.UpdatePassword_ConfirmNewPasswordErrorPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                PasswordBox_Repeat.Focus();
                PasswordBox_Repeat.SelectAll();
                return;
            }


            var req = new UserUpdatePasswordRequest<UserUpdatePasswordResponse>();
            var client = new WTDefaultClient<UserUpdatePasswordResponse>();

            req.OldPassword = PasswordBox_Old.Password;
            req.NewPassword = PasswordBox_New.Password;

            client.ExecuteCompleted += (obj, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        Global.Instance.UpdateSettings(Global.Instance.Settings.UID, req.NewPassword, arg.Result.Session);
                        var result = MessageBox.Show(StringLibrary.UpdatePassword_SucceededText, StringLibrary.UpdatePassword_SucceededCaption, MessageBoxButton.OK);
                        this.NavigationService.GoBack();
                    });
                };

            client.ExecuteFailed += (obj, arg) =>
                {
                    if (arg.Error is System.Net.WebException)
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            WTToast.Instance.Show(StringLibrary.Toast_NetworkErrorPrompt);
                            this.NavigationService.GoBack();
                        });
                    }
                    else if (arg.Error is WTException)
                    {
                        var ex = arg.Error as WTException;
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            switch (ex.StatusCode.Id)
                            {
                                case Api.Util.Status.InvalidPassword:
                                    {
                                        MessageBox.Show(StringLibrary.UpdatePassword_OldPasswordErrorPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                        PasswordBox_Old.Focus();
                                        PasswordBox_Old.SelectAll();
                                    }
                                    break;
                                case Api.Util.Status.NoAuth:
                                    {
                                        MessageBox.Show(StringLibrary.Common_SignInOnOtherPlatformPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                        this.NavigationService.RemoveBackEntry();
                                        this.NavigationService.GoBack();
                                    }
                                    break;
                                default:
                                    MessageBox.Show(StringLibrary.Common_FailurePrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                    break;
                            }
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

            client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
        }

        private void EnableScrolling(Object sender, RoutedEventArgs e)
        {
            CoreScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        }

        private void DisableScrolling(Object sender, RoutedEventArgs e)
        {
            CoreScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }
    }
}