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
using System.Windows.Navigation;
using WeTongji.Api.Domain;
using WeTongji.DataBase;
using WeTongji.Api;
using System.IO.IsolatedStorage;
using WeTongji.Pages;
using WeTongji.Business;
using WeTongji.Utility;
using Microsoft.Phone.Shell;
using WeTongji.Api.Request;
using WeTongji.Api.Response;
using System.Threading;

namespace WeTongji
{
    public partial class Activity : PhoneApplicationPage
    {
        public Activity()
        {
            InitializeComponent();

            ApplicationBarIconButton button = null;

            button = new ApplicationBarIconButton(new Uri("/icons/appbar.Like.rest.png", UriKind.RelativeOrAbsolute))
            {
                Text = StringLibrary.Activity_AppBarLikeText,
                IsEnabled = false
            };
            this.ApplicationBar.Buttons.Add(button);

            button = new ApplicationBarIconButton(new Uri("/icons/appbar.favs.rest.png", UriKind.RelativeOrAbsolute))
            {
                Text = StringLibrary.Activity_AppBarFavoriteText,
                IsEnabled = false
            };
            this.ApplicationBar.Buttons.Add(button);

            button = new ApplicationBarIconButton(new Uri("/icons/appbar.participate.rest.png", UriKind.RelativeOrAbsolute))
            {
                Text = StringLibrary.Activity_AppBarParticipateText,
                IsEnabled = false
            };
            this.ApplicationBar.Buttons.Add(button);
        }

        #region [Overridden]

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                var path = e.Uri.ToString();

                var thread = new Thread(new ParameterizedThreadStart(LoadData));

                thread.Start(e.Uri.ToString());
            }
        }

        #endregion

        #region [Init]

        private void LoadData(object param)
        {
            ActivityExt activity = null;

            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
            });

            #region [Get Official Note in Database]

            var uri = ((String)param).TrimStart("/Pages/Activity.xaml?q=".ToCharArray());

            if (String.IsNullOrEmpty(uri))
            {
                using (var db = WTShareDataContext.ShareDB)
                {
                    activity = db.Activities.LastOrDefault();
                }
            }
            else
            {
                int id;
                if (Int32.TryParse(uri, out id))
                {
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        activity = db.Activities.Where((a) => a.Id == id).SingleOrDefault();
                    }
                }
            }

            #endregion

            if (activity != null)
            {
                #region [Binding]

                this.Dispatcher.BeginInvoke(() =>
                {
                    var tbs = activity.Description.GetInlineCollection();
                    this.DataContext = activity;

                    #region [Set description]

                    StackPanel_Description.Children.Clear();

                    var number = tbs.Count();
                    for (int i = 0; i < number; ++i)
                    {
                        var tb = tbs.ElementAt(i);
                        tb.Style = this.Resources["DescriptionTextBlockStyle"] as Style;
                        StackPanel_Description.Children.Add(tb);
                    }

                    #endregion
                });

                #endregion

                #region [App bar buttons]

                if (!String.IsNullOrEmpty(Global.Instance.Settings.UID))
                {
                    using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                    {
                        var favObj = db.Favorites.Where((fo) => fo.Id == (int)FavoriteIndex.kActivity).SingleOrDefault();

                        if (favObj.Contains(activity.Id))
                        {
                            activity.CanFavorite = false;
                        }
                    }
                }
                else
                {
                    activity.CanLike = true;
                    activity.CanFavorite = true;
                    activity.CanSchedule = true;
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    InitAppBarButtons();
                });

                #endregion

                #region [Handle Images]

                //...Avatar
                if (!activity.OrganizerAvatar.EndsWith("missing.png") && !activity.AvatarExists())
                {
                    WTDownloadImageClient client = new WTDownloadImageClient();

                    client.DownloadImageCompleted += (obj, arg) =>
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            (this.DataContext as ActivityExt).SendPropertyChanged("OrganizerAvatarImageBrush");
                        });
                    };
                    client.Execute(activity.OrganizerAvatar, activity.OrganizerAvatarGuid + "." + activity.OrganizerAvatar.GetImageFileExtension());
                }

                //...Current activity is illustrated.
                if (!activity.Image.EndsWith("missing.png"))
                {
                    #region [Illustration is in isolated storage folder]

                    if (activity.ImageExists())
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            Illustration.Visibility = Visibility.Visible;
                        });
                    }

                    #endregion
                    #region [Illustration needs downloading from the server]

                    else
                    {
                        WTDownloadImageClient client = new WTDownloadImageClient();

                        client.DownloadImageCompleted += (obj, arg) =>
                        {
                            this.Dispatcher.BeginInvoke(() =>
                            {
                                (this.DataContext as ActivityExt).SendPropertyChanged("ActivityImageBrush");
                                Illustration.Visibility = Visibility.Visible;
                            });
                        };
                        client.Execute(activity.Image, activity.ImageGuid + "." + activity.Image.GetImageFileExtension());
                    }

                    #endregion
                }
                else
                {
                    //...Do nothing.
                }

                #endregion
            }

            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Close();
            });
        }

        private void InitAppBarButtons()
        {
            var activity = this.DataContext as ActivityExt;

            if (activity == null)
                return;

            if (String.IsNullOrEmpty(Global.Instance.CurrentUserID))
            {
                activity.CanLike = true;
                activity.CanFavorite = true;
                activity.CanSchedule = true;
            }
            else
            {
                activity.CanSchedule = !Global.Instance.ParticipatingActivitiesIdList.Contains(activity.Id);
            }

            if (activity.CanLike)
            {
                var btn = ApplicationBar.Buttons[0] as ApplicationBarIconButton;

                btn.IsEnabled = true;
                btn.Click += Button_Like_Clicked;
                btn.Click -= Button_Unlike_Clicked;
            }
            else
            {
                var btn = ApplicationBar.Buttons[0] as ApplicationBarIconButton;

                btn.IconUri = new Uri("/icons/appbar.unlike.rest.png", UriKind.RelativeOrAbsolute);
                btn.Text = StringLibrary.Activity_AppBarUnlikeText;
                btn.IsEnabled = true;
                btn.Click -= Button_Like_Clicked;
                btn.Click += Button_Unlike_Clicked;
            }

            if (activity.CanFavorite)
            {
                var btn = ApplicationBar.Buttons[1] as ApplicationBarIconButton;

                btn.IsEnabled = true;
                btn.Click += Button_Favorite_Clicked;
                btn.Click -= Button_Unfavorite_Clicked;
            }
            else
            {
                var btn = ApplicationBar.Buttons[1] as ApplicationBarIconButton;

                btn.IconUri = new Uri("/icons/appbar.unfavourite.rest.png", UriKind.RelativeOrAbsolute);
                btn.Text = StringLibrary.Activity_AppBarUnfavoriteText;
                btn.IsEnabled = true;
                btn.Click -= Button_Favorite_Clicked;
                btn.Click += Button_Unfavorite_Clicked;
            }

            if (activity.CanSchedule)
            {
                var btn = ApplicationBar.Buttons[2] as ApplicationBarIconButton;

                btn.IsEnabled = true;
                btn.Click += Button_Schedule_Clicked;
                btn.Click -= Button_UnSchedule_Clicked;
            }
            else
            {
                var btn = ApplicationBar.Buttons[2] as ApplicationBarIconButton;

                btn.IconUri = new Uri("/icons/appbar.unparticipate.rest.png", UriKind.RelativeOrAbsolute);
                btn.Text = StringLibrary.Activity_AppBarUnparticipateText;
                btn.IsEnabled = true;
                btn.Click -= Button_Schedule_Clicked;
                btn.Click += Button_UnSchedule_Clicked;
            }

        }

        #endregion

        #region [Visual]

        #region [App bar]

        private void Button_Like_Clicked(Object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Global.Instance.CurrentUserID))
            {
                MessageBox.Show(StringLibrary.Common_LogInFirstPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
            }
            else
            {
                (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false;

                var req = new ActivityLikeRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();
                req.Id = (this.DataContext as ActivityExt).Id;

                client.ExecuteCompleted += (obj, arg) =>
                {
                    #region [Flurry]

#if Flurry

                    FlurryWP8SDK.Api.LogEvent(
                        ((int)FlurryWP8SDK.Models.EventName.ClickAppBarLikeButton).ToString(),
                        new List<FlurryWP8SDK.Models.Parameter>(
                            new FlurryWP8SDK.Models.Parameter[]{
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.LikeableParameter).ToString(), 
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.Activity)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );

#endif

                    #endregion

                    ActivityExt itemInDB = null;

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        itemInDB = db.Activities.Where((a) => a.Id == req.Id).SingleOrDefault();

                        if (itemInDB != null)
                        {
                            itemInDB.CanLike = false;
                            ++itemInDB.Like;
                            db.SubmitChanges();
                        }
                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var btn = ApplicationBar.Buttons[0] as ApplicationBarIconButton;

                        btn.IconUri = new Uri("/icons/appbar.unlike.rest.png", UriKind.RelativeOrAbsolute);
                        btn.Text = StringLibrary.Activity_AppBarUnlikeText;
                        btn.IsEnabled = true;
                        btn.Click -= Button_Like_Clicked;
                        btn.Click += Button_Unlike_Clicked;

                        var activity = this.DataContext as ActivityExt;

                        if (itemInDB != null)
                        {
                            activity.CanLike = false;
                            activity.Like = itemInDB.Like;
                            activity.SendPropertyChanged("Like");

                            (this.Resources["IncreaseLikeNumberAnimation"] as Storyboard).Begin();
                        }

                        ProgressBarPopup.Instance.Close();
                        WTToast.Instance.Show(StringLibrary.Toast_Success);
                    });
                };

                client.ExecuteFailed += (obj, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;
                        ProgressBarPopup.Instance.Close();
                    });

                    if (arg.Error is System.Net.WebException)
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            WTToast.Instance.Show(StringLibrary.Toast_NetworkErrorPrompt);
                            (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;
                        });
                    }
                    else if (arg.Error is WTException)
                    {
                        var err = arg.Error as WTException;

                        this.Dispatcher.BeginInvoke(() =>
                        {
                            switch (err.StatusCode.Id)
                            {
                                case Api.Util.Status.NoAuth:
                                    {
                                        MessageBox.Show(StringLibrary.Common_SignInOnOtherPlatformPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                    }
                                    break;
                                //...Todo @_@ Check other status code.
                            }
                        });
                    }
                    else
                    {
                        MessageBox.Show(StringLibrary.Common_FailurePrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                    }
                };


                ProgressBarPopup.Instance.Open();
                client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
            }
        }

        private void Button_Unlike_Clicked(Object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Global.Instance.CurrentUserID))
            {
                MessageBox.Show(StringLibrary.Common_LogInFirstPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
            }
            else
            {
                (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false;

                var req = new ActivityUnLikeRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();
                req.Id = (this.DataContext as ActivityExt).Id;

                client.ExecuteCompleted += (obj, arg) =>
                {
                    #region [Flurry]
#if Flurry
                    FlurryWP8SDK.Api.LogEvent(
                        ((int)FlurryWP8SDK.Models.EventName.ClickAppBarUnlikeButton).ToString(),
                        new List<FlurryWP8SDK.Models.Parameter>(
                            new FlurryWP8SDK.Models.Parameter[]{
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.LikeableParameter).ToString(), 
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.Activity)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif
                    #endregion

                    ActivityExt itemInDB = null;

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        itemInDB = db.Activities.Where((a) => a.Id == req.Id).SingleOrDefault();

                        if (itemInDB != null)
                        {
                            itemInDB.CanLike = true;
                            --itemInDB.Like;
                            db.SubmitChanges();
                        }
                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var btn = ApplicationBar.Buttons[0] as ApplicationBarIconButton;

                        btn.IconUri = new Uri("/icons/appbar.like.rest.png", UriKind.RelativeOrAbsolute);
                        btn.Text = StringLibrary.Activity_AppBarLikeText;
                        btn.IsEnabled = true;
                        btn.Click += Button_Like_Clicked;
                        btn.Click -= Button_Unlike_Clicked;

                        var activity = this.DataContext as ActivityExt;

                        if (itemInDB != null)
                        {
                            activity.Like = itemInDB.Like;
                            activity.CanLike = true;

                            activity.SendPropertyChanged("Like");

                            (this.Resources["DecreaseLikeNumberAnimation"] as Storyboard).Begin();
                        }

                        ProgressBarPopup.Instance.Close();
                        WTToast.Instance.Show(StringLibrary.Toast_Success);
                    });
                };

                client.ExecuteFailed += (obj, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;
                        ProgressBarPopup.Instance.Close();
                    });

                    if (arg.Error is System.Net.WebException)
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;
                            WTToast.Instance.Show(StringLibrary.Toast_NetworkErrorPrompt);
                        });
                    }
                    else if (arg.Error is WTException)
                    {
                        var err = arg.Error as WTException;

                        this.Dispatcher.BeginInvoke(() =>
                        {
                            switch (err.StatusCode.Id)
                            {
                                case Api.Util.Status.NoAuth:
                                    {
                                        MessageBox.Show(StringLibrary.Common_SignInOnOtherPlatformPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                    }
                                    break;
                                //...Todo @_@ Check other status code.
                            }
                        });
                    }
                    else
                    {
                        MessageBox.Show(StringLibrary.Common_FailurePrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                    }
                };

                ProgressBarPopup.Instance.Open();
                client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
            }
        }

        private void Button_Favorite_Clicked(Object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Global.Instance.CurrentUserID))
            {
                MessageBox.Show(StringLibrary.Common_LogInFirstPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                return;
            }
            else
            {
                var req = new ActivityFavoriteRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();

                req.Id = (this.DataContext as ActivityExt).Id;

                client.ExecuteCompleted += (obj, arg) =>
                {
                    #region [Flurry]
#if Flurry
                    FlurryWP8SDK.Api.LogEvent(
                        ((int)FlurryWP8SDK.Models.EventName.ClickAppBarFavoriteButton).ToString(),
                        new List<FlurryWP8SDK.Models.Parameter>(
                            new FlurryWP8SDK.Models.Parameter[]{
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.FavorableParameter).ToString(), 
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.Activity)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif
                    #endregion

                    ActivityExt itemInDB = null;

                    if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            itemInDB = db.Activities.Where((a) => a.Id == req.Id).SingleOrDefault();

                            if (itemInDB != null)
                            {
                                itemInDB.CanFavorite = false;
                                ++itemInDB.Favorite;
                            }

                            db.SubmitChanges();
                        }

                        using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                        {
                            var fav = db.Favorites.Where((fo) => fo.Id == (int)FavoriteIndex.kActivity).SingleOrDefault();

                            fav.Add(req.Id);

                            db.SubmitChanges();
                        }
                    }

                    //...Raise favorite changed
                    if (itemInDB != null)
                        Global.Instance.RaiseFavoriteChanged(itemInDB);

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var favBtn = this.ApplicationBar.Buttons[1] as ApplicationBarIconButton;
                        favBtn.IconUri = new Uri("/icons/appbar.unfavourite.rest.png", UriKind.RelativeOrAbsolute);
                        favBtn.Text = StringLibrary.Activity_AppBarUnfavoriteText;
                        favBtn.Click -= Button_Favorite_Clicked;
                        favBtn.Click += Button_Unfavorite_Clicked;
                        favBtn.IsEnabled = true;

                        var activity = this.DataContext as ActivityExt;

                        if (itemInDB != null)
                        {
                            activity.Favorite = itemInDB.Favorite;
                            activity.CanFavorite = false;
                            activity.SendPropertyChanged("Favorite");

                            (this.Resources["IncreaseFavoriteNumberAnimation"] as Storyboard).Begin();
                        }

                        ProgressBarPopup.Instance.Close();
                        WTToast.Instance.Show(StringLibrary.Toast_Success);
                    });

                };

                client.ExecuteFailed += (obj, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();
                        (this.ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true;

                        if (arg.Error is System.Net.WebException)
                        {
                            WTToast.Instance.Show(StringLibrary.Toast_NetworkErrorPrompt);
                        }
                        else if (arg.Error is WTException)
                        {
                            switch ((arg.Error as WTException).StatusCode.Id)
                            {
                                case Api.Util.Status.NoAuth:
                                    {
                                        MessageBox.Show(StringLibrary.Common_SignInOnOtherPlatformPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                    }
                                    break;
                                //...Todo @_@ Check other status code.
                            }
                        }
                    });
                };

                ProgressBarPopup.Instance.Open();
                (this.ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false;
                client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
            }
        }

        private void Button_Unfavorite_Clicked(Object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Global.Instance.CurrentUserID))
            {
                MessageBox.Show(StringLibrary.Common_LogInFirstPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                return;
            }
            else
            {
                var req = new ActivityUnFavoriteRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();

                req.Id = (this.DataContext as ActivityExt).Id;

                client.ExecuteCompleted += (obj, arg) =>
                {
                    #region [Flurry]
#if Flurry
                    FlurryWP8SDK.Api.LogEvent(
                        ((int)FlurryWP8SDK.Models.EventName.ClickAppBarUnfavoriteButton).ToString(),
                        new List<FlurryWP8SDK.Models.Parameter>(
                            new FlurryWP8SDK.Models.Parameter[]{
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.FavorableParameter).ToString(), 
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.Activity)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif
                    #endregion

                    ActivityExt itemInDB = null;

                    if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            itemInDB = db.Activities.Where((a) => a.Id == req.Id).SingleOrDefault();

                            if (itemInDB != null)
                            {
                                itemInDB.Favorite = Math.Max(0, itemInDB.Favorite - 1);
                                itemInDB.CanFavorite = true;
                            }

                            db.SubmitChanges();
                        }

                        using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                        {
                            var fav = db.Favorites.Where((fo) => fo.Id == (int)FavoriteIndex.kActivity).SingleOrDefault();

                            fav.Remove(req.Id);

                            db.SubmitChanges();
                        }
                    }

                    //...Raise favorite changed
                    if (itemInDB != null)
                        Global.Instance.RaiseFavoriteChanged(itemInDB);

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var favBtn = this.ApplicationBar.Buttons[1] as ApplicationBarIconButton;
                        favBtn.IconUri = new Uri("/icons/appbar.favs.rest.png", UriKind.RelativeOrAbsolute);
                        favBtn.Text = StringLibrary.Activity_AppBarFavoriteText;
                        favBtn.Click += Button_Favorite_Clicked;
                        favBtn.Click -= Button_Unfavorite_Clicked;
                        favBtn.IsEnabled = true;

                        var activity = this.DataContext as ActivityExt;
                        if (itemInDB != null)
                        {
                            activity.Favorite = itemInDB.Favorite;
                            activity.CanFavorite = true;
                            activity.SendPropertyChanged("Favorite");

                            (this.Resources["DecreaseFavoriteNumberAnimation"] as Storyboard).Begin();
                        }

                        ProgressBarPopup.Instance.Close();
                        WTToast.Instance.Show(StringLibrary.Toast_Success);
                    });

                };

                client.ExecuteFailed += (obj, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();
                        (this.ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true;

                        if (arg.Error is System.Net.WebException)
                        {
                            WTToast.Instance.Show(StringLibrary.Toast_NetworkErrorPrompt);
                        }
                        else if (arg.Error is WTException)
                        {
                            switch ((arg.Error as WTException).StatusCode.Id)
                            {
                                case Api.Util.Status.NoAuth:
                                    {
                                        MessageBox.Show(StringLibrary.Common_SignInOnOtherPlatformPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                    }
                                    break;
                                //...Todo @_@ Check other status code.
                            }
                        }
                    });
                };

                ProgressBarPopup.Instance.Open();
                (this.ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false;
                client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
            }
        }

        private void Button_Schedule_Clicked(Object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Global.Instance.CurrentUserID))
            {
                MessageBox.Show(StringLibrary.Common_LogInFirstPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                return;
            }
            else
            {
                var req = new ActivityScheduleRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();

                req.Id = (this.DataContext as ActivityExt).Id;

                client.ExecuteCompleted += (obj, arg) =>
                {
                    #region [Flurry]
#if Flurry
                    FlurryWP8SDK.Api.LogEvent(
                        ((int)FlurryWP8SDK.Models.EventName.ClickAppBarScheduleButton).ToString(),
                        new List<FlurryWP8SDK.Models.Parameter>(
                            new FlurryWP8SDK.Models.Parameter[]{
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.ScheduleableParameter).ToString(), 
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.Activity)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif
                    #endregion

                    ActivityExt itemInDB = null;

                    if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            itemInDB = db.Activities.Where((a) => a.Id == req.Id).SingleOrDefault();

                            if (itemInDB != null)
                            {
                                ++itemInDB.Schedule;
                                itemInDB.CanSchedule = false;
                            }

                            db.SubmitChanges();
                        }

                        //...Update AgendaSource
                        if (itemInDB != null)
                        {
                            Global.Instance.AgendaSource.InsertCalendarNode(itemInDB.GetCalendarNode());
                            Global.Instance.RaiseAgendaSourceChanged();
                        }

                        if (!Global.Instance.ParticipatingActivitiesIdList.Contains(req.Id))
                        {
                            Global.Instance.ParticipatingActivitiesIdList.Add(req.Id);
                            Global.Instance.RaiseActivityScheduleChanged(itemInDB);
                        }
                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var favBtn = this.ApplicationBar.Buttons[2] as ApplicationBarIconButton;
                        favBtn.IconUri = new Uri("/icons/appbar.unparticipate.rest.png", UriKind.RelativeOrAbsolute);
                        favBtn.Text = StringLibrary.Activity_AppBarUnparticipateText;
                        favBtn.Click -= Button_Schedule_Clicked;
                        favBtn.Click += Button_UnSchedule_Clicked;
                        favBtn.IsEnabled = true;

                        var activity = this.DataContext as ActivityExt;
                        if (itemInDB != null)
                        {
                            activity.Schedule = itemInDB.Schedule;
                            activity.CanSchedule = false;
                        }

                        ProgressBarPopup.Instance.Close();
                        WTToast.Instance.Show(StringLibrary.Toast_Success);
                    });

                };

                client.ExecuteFailed += (obj, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();
                        (this.ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = true;

                        if (arg.Error is System.Net.WebException)
                        {
                            WTToast.Instance.Show(StringLibrary.Toast_NetworkErrorPrompt);
                        }
                        else if (arg.Error is WTException)
                        {
                            switch ((arg.Error as WTException).StatusCode.Id)
                            {
                                case Api.Util.Status.NoAuth:
                                    {
                                        MessageBox.Show(StringLibrary.Common_SignInOnOtherPlatformPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                    }
                                    break;
                                //...Todo @_@ Check other status code.
                            }
                        }
                    });
                };

                ProgressBarPopup.Instance.Open();
                (this.ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = false;
                client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
            }
        }

        private void Button_UnSchedule_Clicked(Object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Global.Instance.CurrentUserID))
            {
                MessageBox.Show(StringLibrary.Common_LogInFirstPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                return;
            }
            else
            {
                var req = new ActivityUnScheduleRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();

                req.Id = (this.DataContext as ActivityExt).Id;

                client.ExecuteCompleted += (obj, arg) =>
                {
                    #region [Flurry]
#if Flurry
                    FlurryWP8SDK.Api.LogEvent(
                        ((int)FlurryWP8SDK.Models.EventName.ClickAppBarUnscheduleButton).ToString(),
                        new List<FlurryWP8SDK.Models.Parameter>(
                            new FlurryWP8SDK.Models.Parameter[]{
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.ScheduleableParameter).ToString(), 
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.Activity)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif
                    #endregion

                    ActivityExt itemInDB = null;

                    if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            itemInDB = db.Activities.Where((a) => a.Id == req.Id).SingleOrDefault();

                            if (itemInDB != null)
                            {
                                itemInDB.Schedule = Math.Max(0, itemInDB.Schedule - 1);
                                itemInDB.CanSchedule = true;
                            }

                            db.SubmitChanges();
                        }

                        //...Update AgendaSource
                        if (itemInDB != null)
                        {
                            //...Remove in global AgendaSource
                            Global.Instance.AgendaSource.RemoveCalendarNode(itemInDB.GetCalendarNode());
                            Global.Instance.RaiseAgendaSourceChanged();

                            //...Participating List
                            if (Global.Instance.ParticipatingActivitiesIdList.Contains(req.Id))
                            {
                                Global.Instance.ParticipatingActivitiesIdList.Remove(req.Id);
                                Global.Instance.RaiseActivityScheduleChanged(itemInDB);
                            }
                        }
                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var favBtn = this.ApplicationBar.Buttons[2] as ApplicationBarIconButton;
                        favBtn.IconUri = new Uri("/icons/appbar.participate.rest.png", UriKind.RelativeOrAbsolute);
                        favBtn.Text = StringLibrary.Activity_AppBarParticipateText;
                        favBtn.Click += Button_Schedule_Clicked;
                        favBtn.Click -= Button_UnSchedule_Clicked;
                        favBtn.IsEnabled = true;

                        var activity = this.DataContext as ActivityExt;
                        if (itemInDB != null)
                        {
                            activity.Schedule = itemInDB.Schedule;
                            activity.CanSchedule = true;
                        }

                        ProgressBarPopup.Instance.Close();
                        WTToast.Instance.Show(StringLibrary.Toast_Success);
                    });

                };

                client.ExecuteFailed += (obj, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();
                        (this.ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = true;

                        if (arg.Error is System.Net.WebException)
                        {
                            WTToast.Instance.Show(StringLibrary.Toast_NetworkErrorPrompt);
                        }
                        else if (arg.Error is WTException)
                        {
                            switch ((arg.Error as WTException).StatusCode.Id)
                            {
                                case Api.Util.Status.NoAuth:
                                    {
                                        MessageBox.Show(StringLibrary.Common_SignInOnOtherPlatformPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                    }
                                    break;
                                //...Todo @_@ Check other status code.
                            }
                        }
                    });
                };

                ProgressBarPopup.Instance.Open();
                (this.ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = false;
                client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
            }
        }

        #endregion

        #region [Tap and View image]

        private void ViewActivityImage(Object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            var img = sender as Image;
            var activity = this.DataContext as ActivityExt;
            ImageViewer.CoreImageName = activity.ImageGuid + "." + activity.Image.GetImageFileExtension();
            ImageViewer.CoreImageSource = img.Source as System.Windows.Media.Imaging.BitmapSource;
            this.NavigationService.Navigate(new Uri("/Pages/ImageViewer.xaml", UriKind.RelativeOrAbsolute));
        }
        #endregion

        #endregion
    }
}