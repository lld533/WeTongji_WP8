using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WeTongji.Api.Domain;
using WeTongji.DataBase;
using WeTongji.Business;
using WeTongji.Pages;
using System.Threading;
using System.Reflection;
using WeTongji.Api.Request;
using WeTongji.Api;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.IO;

namespace WeTongji
{
    public partial class EditPersonalProfile : PhoneApplicationPage
    {
        private UserExt copy = null;
        private Boolean isAvatarChanged = false;

        public EditPersonalProfile()
        {
            InitializeComponent();

            var btn = new ApplicationBarIconButton(new Uri("/icons/appbar.save.rest.png", UriKind.RelativeOrAbsolute))
            {
                Text = StringLibrary.EditPersonalProfile_AppBarSaveText
            };
            btn.Click += SaveButtonClicked;
            this.ApplicationBar.Buttons.Add(btn);

            this.Loaded += (o, e) =>
                {
                    var thread = new Thread(new ThreadStart(LoadData))
                    {
                        IsBackground = true,
                        Name = "LoadData"
                    };

                    ProgressBarPopup.Instance.Open();
                    thread.Start();
                };
        }

        /// <summary>
        /// Select all text if the text of is not empty
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxGotFocus(Object sender, RoutedEventArgs e)
        {
            #region [Check argument]

            if (sender == null)
                throw new ArgumentNullException("sender");

            var txtbx = sender as TextBox;

            if (txtbx == null)
                throw new ArgumentOutOfRangeException("sender");

            #endregion

            #region [Flurry]

            if (txtbx == TextBox_Phone)
            {
#if Flurry
                FlurryWP8SDK.Api.LogEvent(
                    ((int)FlurryWP8SDK.Models.EventName.EditPersonalProfile).ToString(),
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
                    ((int)FlurryWP8SDK.Models.EventName.EditPersonalProfile).ToString(),
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
                    ((int)FlurryWP8SDK.Models.EventName.EditPersonalProfile).ToString(),
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
            else if (txtbx == TextBox_SinaMicroBlog)
            {
#if Flurry
                FlurryWP8SDK.Api.LogEvent(
                    ((int)FlurryWP8SDK.Models.EventName.EditPersonalProfile).ToString(),
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

            if (!String.IsNullOrEmpty(txtbx.Text))
            {
                txtbx.SelectAll();
            }
        }

        private void TextBoxTextChanged(Object sender, RoutedEventArgs e)
        {
            var txtbx = sender as TextBox;

            if (txtbx == null)
                return;

            var user = this.DataContext as UserExt;
            var bindingExpression = txtbx.GetBindingExpression(TextBox.TextProperty);
            var propertyInfo = typeof(UserExt).GetProperty(bindingExpression.ParentBinding.Path.Path);

            propertyInfo.SetValue(user, txtbx.Text, null);
        }

        private void SaveButtonClicked(object sender, EventArgs e)
        {
            this.Focus();

            UserExt current = this.DataContext as UserExt;

            //...if nothing changed, just go back
            if (!isAvatarChanged && current.Phone == copy.Phone && current.Email == copy.Email
                && current.QQ == copy.QQ && current.SinaWeibo == copy.SinaWeibo)
            {
                this.NavigationService.GoBack();
            }
            else
            {
                #region [Flurry]

#if Flurry

                FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.SavePersonalProfile).ToString());

#endif

                #endregion


                var btn = sender as ApplicationBarIconButton;
                btn.IsEnabled = false;
                ProgressBarPopup.Instance.Open();

                #region [Avatar changed]
                if (isAvatarChanged)
                {
                    var req = new UserUpdateAvatarRequest<WeTongji.Api.Response.UserGetResponse>();
                    var client = new WTDefaultClient<WeTongji.Api.Response.UserGetResponse>();

                    var wb = new WriteableBitmap(this.Image_Avatar.Source as BitmapSource);

                    MemoryStream stream = new MemoryStream();
                    wb.SaveJpeg(stream, wb.PixelWidth, wb.PixelHeight, 0, 100);
                    stream.Seek(0, SeekOrigin.Begin);
                    req.JpegPhotoStream = stream;

                    client.ExecuteCompleted += (obj, arg) =>
                    {
                        //...Get current user
                        using (var db = new WTUserDataContext(current.UID))
                        {
                            var previousUserInfo = db.UserInfo.SingleOrDefault();

                            if (previousUserInfo != null)
                            {
                                previousUserInfo.SetObject(arg.Result.User);
                            }
                        }

                        req.JpegPhotoStream.Seek(0, SeekOrigin.Begin);
                        current.SaveAvatarImage(req.JpegPhotoStream);

                        Global.Instance.RaiseUserAvatarChanged();

                        this.Dispatcher.BeginInvoke(() =>
                        {
                            ProgressBarPopup.Instance.Close();

                            if (current.Phone == copy.Phone && current.Email == copy.Email
                                && current.QQ == copy.QQ && current.SinaWeibo == copy.SinaWeibo)
                            {
                                WTToast.Instance.Show(StringLibrary.Toast_Success);
                                this.NavigationService.GoBack();
                            }
                            else
                            {
                                var thread = new System.Threading.Thread(new ParameterizedThreadStart(SaveData));
                                thread.Start(current);
                            }
                        });
                    };

                    client.ExecuteFailed += (obj, arg) =>
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            ProgressBarPopup.Instance.Close();
                        });

                        if (arg.Error is System.Net.WebException)
                        {
                            this.Dispatcher.BeginInvoke(() =>
                            {
                                WTToast.Instance.Show(StringLibrary.Toast_NetworkErrorPrompt);
                            });
                        }
                        else if (arg.Error is WTException)
                        {
                            if ((arg.Error as WTException).StatusCode.Id == WeTongji.Api.Util.Status.NoAuth)
                            {
                                this.Dispatcher.BeginInvoke(() =>
                                {
                                    MessageBox.Show(StringLibrary.Common_SignInOnOtherPlatformPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                });
                            }
                            //...Todo @_@ check other status code
                            else
                            {
                                this.Dispatcher.BeginInvoke(() =>
                                {
                                    MessageBox.Show(StringLibrary.EditPersonalProfile_UpdateAvatarFailedPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                });
                            }
                        }
                        else
                        {
                            this.Dispatcher.BeginInvoke(() =>
                            {
                                MessageBox.Show(StringLibrary.EditPersonalProfile_UpdateAvatarFailedPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                            });
                        }

                        //...Update UI
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            btn.IsEnabled = true;
                        });
                    };

                    client.Post(req, Global.Instance.Session, Global.Instance.Settings.UID);
                }
                #endregion
                #region [User profile changed]
                else
                {
                    var thread = new System.Threading.Thread(new ParameterizedThreadStart(SaveData));
                    thread.Start(current);
                }
                #endregion
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            var current = this.DataContext as UserExt;

            if (current != null && (isAvatarChanged || current.Phone != copy.Phone || current.Email != copy.Email
               || current.QQ != copy.QQ || current.SinaWeibo != copy.SinaWeibo))
            {
                var result = MessageBox.Show(StringLibrary.EditPersonalProfile_DiscardAndReturnPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                    e.Cancel = true;
            }
        }

        private void LoadData()
        {
            UserExt user = null;

            using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
            {
                user = db.UserInfo.SingleOrDefault();
            }

            this.Dispatcher.BeginInvoke(() =>
            {
                this.copy = user.Clone();
                this.DataContext = user;

                ProgressBarPopup.Instance.Close();
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="param">the user's profile to save, typeof(UserExt)</param>
        private void SaveData(Object param)
        {
            var data = param as UserExt;

            #region [Basic info]

            if (data != null)
            {
                var req = new UserUpdateRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Open();
                });

                req.User = data.GetObject() as User;

                client.ExecuteFailed += (o, e) =>
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            ProgressBarPopup.Instance.Close();
                            (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;

                            if (e.Error is System.Net.WebException)
                                WTToast.Instance.Show(StringLibrary.Toast_NetworkErrorPrompt);
                            else
                                MessageBox.Show(StringLibrary.EditPersonalProfile_SavePersonalProfileFailedPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                        });
                    };

                client.ExecuteCompleted += (o, e) =>
                    {
                        using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                        {
                            var userExt = db.UserInfo.SingleOrDefault();
                            var user = req.User;

                            userExt.Email = user.Email;
                            userExt.Phone = user.Phone;
                            userExt.QQ = user.QQ;
                            userExt.SinaWeibo = user.SinaWeibo;

                            db.SubmitChanges();
                        }

                        Global.Instance.RaiseUserProfileChanged();

                        this.Dispatcher.BeginInvoke(() =>
                        {
                            ProgressBarPopup.Instance.Close();
                            (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;

                            this.NavigationService.GoBack();
                        });
                    };

                client.Post(req, Global.Instance.Session, Global.Instance.Settings.UID);
            }
            #endregion
        }

        private void UpdateAvatar(Object sender, RoutedEventArgs e)
        {
            var task = new Microsoft.Phone.Tasks.PhotoChooserTask();
            task.PixelHeight = 200;
            task.PixelWidth = 200;
            task.ShowCamera = true;

            task.Completed += (obj, arg) =>
            {
                switch (arg.TaskResult)
                {
                    case Microsoft.Phone.Tasks.TaskResult.OK:
                        {
                            #region [Flurry]
#if Flurry
                            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ChangeAvatar).ToString());
#endif
                            #endregion

                            var img = new BitmapImage();
                            img.SetSource(arg.ChosenPhoto);
                            Image_Avatar.Source = img;
                            isAvatarChanged = true;
                        }
                        break;
                    case Microsoft.Phone.Tasks.TaskResult.None:
                    case Microsoft.Phone.Tasks.TaskResult.Cancel:
                        break;
                }
            };

            task.Show();
        }
    }
}