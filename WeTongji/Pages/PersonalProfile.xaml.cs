using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Threading;
using WeTongji.DataBase;
using WeTongji.Business;
using WeTongji.Api.Domain;
using System.Diagnostics;
using WeTongji.Api;
using WeTongji.Pages;
using System.Windows.Media;
using System.Windows.Data;
using System.Reflection;
using WeTongji.Api.Request;
using WeTongji.Api.Response;
using WeTongji.Utility;

namespace WeTongji
{
    public partial class PersonalProfile : PhoneApplicationPage
    {
        #region [Private parameter]

        /// <summary>
        /// This class is used to construct a background thread, which is used
        /// to update new data to the server and local database.
        /// </summary>
        private class UpdateDataParameter
        {
            public UserExt OldValue { get; private set; }
            public UserExt NewValue { get; private set; }
            public PropertyInfo Info { get; private set; }
            public TextBox Item { get; private set; }

            public UpdateDataParameter(UserExt old_value, UserExt new_value, PropertyInfo info, TextBox item)
            {
                OldValue = old_value;
                NewValue = new_value;
                Info = info;
                Item = item;
            }
        }

        #endregion

        #region [Field]

        private TextBox lastVisibleTextBox = null;

        #endregion

        #region [Constructor]

        public PersonalProfile()
        {
            InitializeComponent();

            Global.Instance.UserAvatarChanged += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    var UserSource = this.DataContext as UserExt;
                    if (UserSource != null)
                    {
                        UserSource.SendPropertyChanged("AvatarImageBrush");
                    }
                });
            };

            Global.Instance.UserProfileChanged += (obj, arg) =>
                {
                    LoadPersonalProfile();
                };

            var button = new ApplicationBarIconButton(new Uri("/icons/appbar.edit.rest.png", UriKind.RelativeOrAbsolute))
                {
                    Text = StringLibrary.PersonalProfile_AppBarEditText
                };
            button.Click += EditPersonalProfile;
            this.ApplicationBar.Buttons.Add(button);

            var mi = new ApplicationBarMenuItem()
            {
                Text = StringLibrary.PersonalProfile_AppBarUpdatePasswordText
            };
            mi.Click += NavToUpdatePassword;
            this.ApplicationBar.MenuItems.Add(mi);
        }

        #endregion

        #region [Functions]

        #region [Load]

        private void LoadPersonalProfile()
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
            });

            if (String.IsNullOrEmpty(Global.Instance.Settings.UID) || !WTUserDataContext.UserDataContextExists(Global.Instance.Settings.UID))
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    TextBlock_LoadFailed.Visibility = Visibility.Visible;
                    ProgressBarPopup.Instance.Close();
                });
            }
            else
            {
                UserExt user = null;

                using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                {
                    user = db.UserInfo.SingleOrDefault();
                }

                if (user == null)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        TextBlock_LoadFailed.Visibility = Visibility.Visible;
                        ProgressBarPopup.Instance.Close();
                    });
                }
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        //...Update UI
                        TextBlock_LoadFailed.Visibility = Visibility.Collapsed;
                        this.DataContext = user;
                        ScrollViewer_Core.Visibility = Visibility.Visible;

                        //...Download image if it needs.
                        Debug.WriteLine(user.Avatar);
                        Debug.WriteLine(user.AvatarGuid);
                        if (!user.Avatar.EndsWith("missing.png") && !user.AvatarImageExists())
                        {
                            var thread = new Thread(new ParameterizedThreadStart(DownloadAvatarImage));

                            thread.Start(user);
                        }
                        else
                        {
                            ProgressBarPopup.Instance.Close();
                        }
                    });
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="param">UserExt</param>
        private void DownloadAvatarImage(object param)
        {
            #region [Check argument]

            if (param == null)
                throw new ArgumentNullException();

            var user = param as UserExt;
            if (user == null)
                throw new NotSupportedException("UserExt is expected");

            #endregion

            var client = new WTDownloadImageClient();

            #region [Register download event handlers]

            client.DownloadImageCompleted += (o, e) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    user.SendPropertyChanged("AvatarImageBrush");
                });
            };

            #endregion

            client.Execute(user.Avatar, user.AvatarGuid + "." + user.Avatar.GetImageFileExtension());
        }

        #endregion

        #region [Nav]

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            this.ScrollViewer_Core.ScrollToVerticalOffset(0);

            if (e.NavigationMode == NavigationMode.New)
            {
                Thread thread = new Thread(new ThreadStart(LoadPersonalProfile));

                ProgressBarPopup.Instance.Open();
                thread.Start();
            }
        }

        private void EditPersonalProfile(Object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Pages/EditPersonalProfile.xaml", UriKind.RelativeOrAbsolute));
        }

        private void NavToUpdatePassword(Object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Pages/UpdatePassword.xaml", UriKind.RelativeOrAbsolute));
        }

        #endregion

        #region [Visual]

        /// <summary>
        /// Click to turn over the corresponding editing text box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// This function relies on VisualTree
        /// </remarks>
        private void ClickToTurnOverTextBoxVisibility(Object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var parent = VisualTreeHelper.GetParent(btn) as Panel;
            var txtbx = VisualTreeHelper.GetChild(parent, 1) as TextBox;
            //txtbx.Visibility = Visibility.Visible;
            if (txtbx.Visibility == Visibility.Visible)
            {
                txtbx.Visibility = Visibility.Collapsed;
                lastVisibleTextBox = null;
            }
            else
            {
                if (lastVisibleTextBox != null && lastVisibleTextBox != txtbx && lastVisibleTextBox.Visibility == Visibility.Visible)
                {
                    lastVisibleTextBox.Visibility = Visibility.Collapsed;
                }

                txtbx.Visibility = Visibility.Visible;
                txtbx.Focus();
                lastVisibleTextBox = txtbx;
            }
        }

        private void EditingTextBox_LostFocus(Object sender, RoutedEventArgs e)
        {
            var txtbx = sender as TextBox;
            var user = this.DataContext as UserExt;

            if (txtbx == null || user == null)
                return;

            #region [Flurry]

            if (txtbx == TextBox_Phone)
            {

#if Flurry

                FlurryWP8SDK.Api.LogEvent(
                    ((int)FlurryWP8SDK.Models.EventName.AddNewPersonalProfile).ToString(),
                    new List<FlurryWP8SDK.Models.Parameter>(
                            new FlurryWP8SDK.Models.Parameter[] 
                            {
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.PersonalProfileParameter).ToString(),
                                    ((int)FlurryWP8SDK.Models.ParameterValue.Phone).ToString()
                                    )
                            }
                        )
                    );

#endif
            }
            else if (txtbx == TextBox_Email)
            {
#if Flurry
                FlurryWP8SDK.Api.LogEvent(
                    ((int)FlurryWP8SDK.Models.EventName.AddNewPersonalProfile).ToString(),
                    new List<FlurryWP8SDK.Models.Parameter>(
                            new FlurryWP8SDK.Models.Parameter[] 
                            {
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.PersonalProfileParameter).ToString(),
                                    ((int)FlurryWP8SDK.Models.ParameterValue.Email).ToString()
                                    )
                            }
                        )
                    );
#endif
            }
            else if (txtbx == TextBox_QQ)
            {
#if Flurry
                FlurryWP8SDK.Api.LogEvent(
                    ((int)FlurryWP8SDK.Models.EventName.AddNewPersonalProfile).ToString(),
                    new List<FlurryWP8SDK.Models.Parameter>(
                            new FlurryWP8SDK.Models.Parameter[] 
                            {
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.PersonalProfileParameter).ToString(),
                                    ((int)FlurryWP8SDK.Models.ParameterValue.QQ).ToString()
                                    )
                            }
                        )
                    );
#endif
            }
            else if (txtbx == TextBox_SinaWeibo)
            {
#if Flurry
                FlurryWP8SDK.Api.LogEvent(
                    ((int)FlurryWP8SDK.Models.EventName.AddNewPersonalProfile).ToString(),
                    new List<FlurryWP8SDK.Models.Parameter>(
                            new FlurryWP8SDK.Models.Parameter[] 
                            {
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.PersonalProfileParameter).ToString(),
                                    ((int)FlurryWP8SDK.Models.ParameterValue.SinaWeibo).ToString()
                                    )
                            }
                        )
                    );
#endif
            }

            #endregion

            //...Get property info by data binding by VisualTree
            var parent = VisualTreeHelper.GetParent(txtbx) as StackPanel;
            var bindingExpr = parent.GetBindingExpression(StackPanel.VisibilityProperty);
            var propertyInfo = typeof(UserExt).GetProperty(bindingExpr.ParentBinding.Path.Path);

            string previousValue = (String)propertyInfo.GetValue(user, null);

            if (previousValue != txtbx.Text)
            {
                ProgressBarPopup.Instance.Open();

                var thread = new Thread(new ParameterizedThreadStart(UpdateData))
                {
                    IsBackground = true,
                    Name = "UpdateData"
                };

                var oldvalue = (this.DataContext as UserExt).Clone();
                var newvalue = (this.DataContext as UserExt).Clone();
                propertyInfo.SetValue(newvalue, txtbx.Text, null);

                var param = new UpdateDataParameter(oldvalue, newvalue, propertyInfo, txtbx);
                thread.Start(param);
            }
        }

        #endregion

        #region [Update data]

        private void UpdateData(Object obj)
        {
            if (obj == null)
                throw new ArgumentNullException();

            var param = obj as UpdateDataParameter;
            if (param == null)
                throw new NotSupportedException("UpdateDataParameter is expected");

            var req = new UserUpdateRequest<WTResponse>();
            var client = new WTDefaultClient<WTResponse>();

            req.User = param.NewValue.GetObject() as User;

            client.ExecuteFailed += (o, e) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();

                        if (e.Error is System.Net.WebException)
                        {
                            WTToast.Instance.Show(StringLibrary.Toast_NetworkErrorPrompt);
                        }
                        else if (e.Error is WeTongji.Api.WTException)
                        {
                            var ex = e.Error as WeTongji.Api.WTException;
                            if (ex.StatusCode.Id == WeTongji.Api.Util.Status.NoAuth)
                            {
                                var result = MessageBox.Show(StringLibrary.Common_SignInOnOtherPlatformPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OKCancel);
                                if (result == MessageBoxResult.OK)
                                {
                                    this.NavigationService.GoBack();
                                }
                            }
                        }
                        else
                            MessageBox.Show(StringLibrary.Common_FailurePrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                    });
                };

            client.ExecuteCompleted += (o, e) =>
                {
                    Debug.WriteLine("Update [{0}] completed.", param.Info.Name);

                    using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                    {
                        var userInDB = db.UserInfo.SingleOrDefault();

                        userInDB.Phone = req.User.Phone;
                        userInDB.QQ = req.User.QQ;
                        userInDB.Email = req.User.Email;
                        userInDB.SinaWeibo = req.User.SinaWeibo;

                        db.SubmitChanges();
                    }

                    //...Update UI
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var user = this.DataContext as UserExt;

                        object v = param.Info.GetValue(param.NewValue, null);
                        param.Info.SetValue(user, v, null);
                        user.SendPropertyChanged(param.Info.Name);
                        param.Item.Text = String.Empty;
                        ProgressBarPopup.Instance.Close();
                    });
                };


            client.Post(req, Global.Instance.Session, Global.Instance.Settings.UID);
        }

        #endregion


        #endregion
    }
}