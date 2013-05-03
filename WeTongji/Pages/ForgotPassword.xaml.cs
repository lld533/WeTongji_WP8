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

namespace WeTongji
{
    public partial class ForgotPassword : PhoneApplicationPage
    {
        public ForgotPassword()
        {
            InitializeComponent();

            var btn = new ApplicationBarIconButton(new Uri("/icons/appbar.check.rest.png", UriKind.RelativeOrAbsolute))
            {
                Text = StringLibrary.ForgotPassword_AppBarSendText,
                IsEnabled = false
            };
            btn.Click += Button_Send_Click;
            this.ApplicationBar.Buttons.Add(btn);

        }

        private void UpdateSendButton(Object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrEmpty(TextBox_Id.Text) && !String.IsNullOrEmpty(TextBox_Name.Text))
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

            var req = new UserResetPassword<WTResponse>();
            var client = new WTDefaultClient<WTResponse>();

            req.NO = TextBox_Id.Text;
            req.Name = TextBox_Name.Text;

            client.ExecuteCompleted += (obj, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var result = MessageBox.Show(StringLibrary.ForgotPassword_SucceededText, StringLibrary.ForgotPassword_SucceededCaption, MessageBoxButton.OKCancel);
                        if (result == MessageBoxResult.OK)
                        {
                            var task = new Microsoft.Phone.Tasks.WebBrowserTask();
                            task.Uri = new Uri("http://mail.tongji.edu.cn");
                            task.Show();
                        }
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
                                case Api.Util.Status.NoAccount:
                                    {
                                        MessageBox.Show(StringLibrary.ForgotPassword_NoAccountPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                        TextBox_Id.Focus();
                                        TextBox_Id.SelectAll();
                                    }
                                    break;
                                case Api.Util.Status.NotActivatedAccount:
                                    {
                                        MessageBox.Show(StringLibrary.ForgotPassword_NotActivatedAccountPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                        TextBox_Id.Focus();
                                        TextBox_Id.SelectAll();
                                    }
                                    break;
                                case Api.Util.Status.NotRegistered:
                                    {
                                        MessageBox.Show(StringLibrary.ForgotPassword_NotRegisteredPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                        TextBox_Id.Focus();
                                        TextBox_Id.SelectAll();
                                    }
                                    break;
                                case Api.Util.Status.IdNameDismatch:
                                    {
                                        MessageBox.Show(StringLibrary.ForgotPassword_IdNameDismatchPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                        TextBox_Name.SelectAll();
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

            client.Execute(req);
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