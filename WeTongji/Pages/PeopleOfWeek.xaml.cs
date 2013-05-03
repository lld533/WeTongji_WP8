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
using WeTongji.Business;
using WeTongji.Api;
using System.Diagnostics;
using WeTongji.Pages;
using WeTongji.Utility;
using Microsoft.Phone.Shell;
using WeTongji.Api.Request;
using WeTongji.Api.Response;

namespace WeTongji
{
    public partial class PeopleOfWeek : PhoneApplicationPage
    {
        public PeopleOfWeek()
        {
            InitializeComponent();

            ApplicationBarIconButton button = null;
            button = new ApplicationBarIconButton(new Uri("/icons/appbar.like.rest.png", UriKind.RelativeOrAbsolute))
            {
                Text = StringLibrary.Common_AppBarLikeText,
                IsEnabled = false
            };
            this.ApplicationBar.Buttons.Add(button);

            button = new ApplicationBarIconButton(new Uri("/icons/appbar.favs.rest.png", UriKind.RelativeOrAbsolute))
            {
                Text = StringLibrary.Common_AppBarFavoriteText,
                IsEnabled = false
            };
            this.ApplicationBar.Buttons.Add(button);
        }

        #region [Overridden]

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var uri = e.Uri.ToString().TrimStart("/Pages/PeopleOfWeek.xaml?q=".ToCharArray());
            int idx;

            if (int.TryParse(uri, out idx))
            {
                var thread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(LoadData));
                thread.Start(idx);
            }
        }

        private void LoadData(Object param)
        {
            int idx = (int)param;
            PersonExt p = null;

            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
            });

            using (var db = WTShareDataContext.ShareDB)
            {
                p = db.People.Where((person) => person.Id == idx).SingleOrDefault();
            }

            if (p != null)
            {
                var images = p.GetImageExts();
                int count = images.Count();

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();

                    this.DataContext = p;

                    #region [Set description]

                    var tbs = p.Description.GetInlineCollection();

                    StackPanel_Description.Children.Clear();

                    var number = tbs.Count();
                    for (int i = 0; i < number; ++i)
                    {
                        var tb = tbs.ElementAt(i);
                        tb.Style = this.Resources["DescriptionTextBlockStyle"] as Style;
                        StackPanel_Description.Children.Add(tb);
                    }

                    #endregion

                    Pivot_Core.Visibility = Visibility.Visible;
                });

                #region [App bar buttons]

                if (!String.IsNullOrEmpty(Global.Instance.Settings.UID))
                {
                    using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                    {
                        var favObj = db.Favorites.Where((fo) => fo.Id == (int)FavoriteIndex.kPeopleOfWeek).SingleOrDefault();

                        if (favObj.Contains(p.Id))
                        {
                            p.CanFavorite = false;
                        }
                    }
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    InitAppBarButtons();
                });

                #endregion

                #region [Download Images]
                if (count > 0)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ListBox_Pic.ItemsSource = images;
                    });

                    for (int i = 0; i < count; ++i)
                    {
                        int j = i;
                        var img = images.ElementAt(i);
                        if (!img.Url.EndsWith("missing.png") && !p.ImageExists(i))
                        {
                            var client = new WTDownloadImageClient();

                            client.DownloadImageCompleted += (obj, arg) =>
                            {
                                this.Dispatcher.BeginInvoke(() =>
                                {
                                    if (j == 0)
                                        p.SendPropertyChanged("FirstImageBrush");

                                    img.SendPropertyChanged("ImageBrush");
                                });
                            };

                            client.Execute(img.Url, img.Id + "." + img.Url.GetImageFileExtension());
                        }
                    }
                }
                #endregion
            }
            else
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                });
            }
        }

        private void InitAppBarButtons()
        {
            var person = this.DataContext as PersonExt;

            if (person == null)
                return;

            if (person.CanLike)
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
                btn.Text = StringLibrary.Common_AppBarUnlikeText;
                btn.IsEnabled = true;
                btn.Click -= Button_Like_Clicked;
                btn.Click += Button_Unlike_Clicked;
            }

            if (person.CanFavorite)
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
                btn.Text = StringLibrary.Common_AppBarUnfavoriteText;
                btn.IsEnabled = true;
                btn.Click -= Button_Favorite_Clicked;
                btn.Click += Button_Unfavorite_Clicked;
            }
        }

        #endregion

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

                var req = new PersonLikeRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();
                req.Id = (this.DataContext as PersonExt).Id;

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
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.WeeklyStar)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif

                    #endregion

                    PersonExt itemInDB = null;
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        itemInDB = db.People.Where((person) => person.Id == req.Id).SingleOrDefault();

                        if (itemInDB != null)
                        {
                            ++itemInDB.Like;
                            itemInDB.CanLike = false;
                            db.SubmitChanges();
                        }
                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var btn = ApplicationBar.Buttons[0] as ApplicationBarIconButton;

                        btn.IconUri = new Uri("/icons/appbar.unlike.rest.png", UriKind.RelativeOrAbsolute);
                        btn.Text = StringLibrary.Common_AppBarUnlikeText;
                        btn.IsEnabled = true;
                        btn.Click -= Button_Like_Clicked;
                        btn.Click += Button_Unlike_Clicked;

                        var person = this.DataContext as PersonExt;

                        if (itemInDB != null)
                        {
                            person.Like = itemInDB.Like;
                            person.SendPropertyChanged("Like");

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

        private void Button_Unlike_Clicked(Object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Global.Instance.CurrentUserID))
            {
                MessageBox.Show(StringLibrary.Common_LogInFirstPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
            }
            else
            {
                (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false;

                var req = new PersonUnLikeRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();
                req.Id = (this.DataContext as PersonExt).Id;

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
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.WeeklyStar)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif
                    #endregion

                    PersonExt itemInDB = null;

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        itemInDB = db.People.Where((person) => person.Id == req.Id).SingleOrDefault();

                        if (itemInDB != null)
                        {
                            itemInDB.Like = Math.Max(0, itemInDB.Like - 1);
                            itemInDB.CanLike = true;
                            db.SubmitChanges();
                        }
                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var btn = ApplicationBar.Buttons[0] as ApplicationBarIconButton;

                        btn.IconUri = new Uri("/icons/appbar.like.rest.png", UriKind.RelativeOrAbsolute);
                        btn.Text = StringLibrary.Common_AppBarLikeText;
                        btn.IsEnabled = true;
                        btn.Click += Button_Like_Clicked;
                        btn.Click -= Button_Unlike_Clicked;

                        var person = this.DataContext as PersonExt;

                        if (itemInDB != null)
                        {
                            person.Like = itemInDB.Like;
                            person.SendPropertyChanged("Like");

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

        private void Button_Favorite_Clicked(Object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Global.Instance.CurrentUserID))
            {
                MessageBox.Show(StringLibrary.Common_LogInFirstPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                return;
            }
            else
            {
                var req = new PersonFavoriteRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();

                req.Id = (this.DataContext as PersonExt).Id;

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
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.WeeklyStar)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif
                    #endregion

                    PersonExt itemInDB = null;

                    if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            itemInDB = db.People.Where((person) => person.Id == req.Id).SingleOrDefault();

                            if (itemInDB != null)
                            {
                                ++itemInDB.Favorite;
                                itemInDB.CanFavorite = false;
                            }

                            db.SubmitChanges();
                        }

                        using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                        {
                            var fav = db.Favorites.Where((fo) => fo.Id == (int)FavoriteIndex.kPeopleOfWeek).SingleOrDefault();

                            fav.Add(req.Id);

                            db.SubmitChanges();
                        }

                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var favBtn = this.ApplicationBar.Buttons[1] as ApplicationBarIconButton;
                        favBtn.IconUri = new Uri("/icons/appbar.unfavourite.rest.png", UriKind.RelativeOrAbsolute);
                        favBtn.Text = StringLibrary.Common_AppBarUnfavoriteText;
                        favBtn.Click -= Button_Favorite_Clicked;
                        favBtn.Click += Button_Unfavorite_Clicked;
                        favBtn.IsEnabled = true;

                        var person = this.DataContext as PersonExt;

                        if (itemInDB != null)
                        {
                            person.Favorite = itemInDB.Favorite;
                            person.SendPropertyChanged("Favorite");

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
                var req = new PersonUnFavoriteRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();

                req.Id = (this.DataContext as PersonExt).Id;

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
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.WeeklyStar)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif

                    #endregion

                    PersonExt itemInDB = null;

                    if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            itemInDB = db.People.Where((news) => news.Id == req.Id).SingleOrDefault();

                            if (itemInDB != null)
                            {
                                itemInDB.Favorite = Math.Max(0, itemInDB.Favorite - 1);
                                itemInDB.CanFavorite = true;
                            }

                            db.SubmitChanges();
                        }

                        using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                        {
                            var fav = db.Favorites.Where((fo) => fo.Id == (int)FavoriteIndex.kPeopleOfWeek).SingleOrDefault();

                            fav.Remove(req.Id);

                            db.SubmitChanges();
                        }

                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var favBtn = this.ApplicationBar.Buttons[1] as ApplicationBarIconButton;
                        favBtn.IconUri = new Uri("/icons/appbar.favs.rest.png", UriKind.RelativeOrAbsolute);
                        favBtn.Text = StringLibrary.Common_AppBarFavoriteText;
                        favBtn.Click += Button_Favorite_Clicked;
                        favBtn.Click -= Button_Unfavorite_Clicked;
                        favBtn.IsEnabled = true;

                        var person = this.DataContext as PersonExt;

                        if (itemInDB != null)
                        {
                            person.Favorite = itemInDB.Favorite;
                            person.SendPropertyChanged("Favorite");

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

        #endregion

        #region [Refresh related functions]

        /// <summary>
        /// Send Read request to server.
        /// </summary>
        /// <remarks>
        /// This function should be executed in UI thread.
        /// </remarks>
        private void ReadCurrentPerson()
        {
            var req = new PersonReadRequest<WTResponse>();
            var client = new WTDefaultClient<WTResponse>();

            req.Id = (this.DataContext as PersonExt).Id;

            client.ExecuteCompleted += (o, e) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        var itemInDB = db.People.Where((person) => person.Id == req.Id).SingleOrDefault();
                        ++itemInDB.Read;
                        db.SubmitChanges();
                    }
                });
            };

            if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
            else
                client.Execute(req);
        }

        #endregion

        private void IgnoreSelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            var lb = sender as ListBox;

            if (-1 == lb.SelectedIndex)
                return;
            lb.SelectedIndex = -1;
        }

        private void TapToViewSourceImage(Object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            var img = sender as Image;
            var imgExt = img.DataContext as ImageExt;
            ImageViewer.CoreImageName = imgExt.Id;
            ImageViewer.CoreImageSource = img.Source as System.Windows.Media.Imaging.BitmapSource;
            this.NavigationService.Navigate(new Uri("/Pages/ImageViewer.xaml", UriKind.RelativeOrAbsolute));
        }

        private void ViewFirstImage(Object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            var img = sender as Image;
            var person = this.DataContext as PersonExt;

            if (img != null && person != null)
            {
                ImageViewer.CoreImageName = person.ImageExtList.Substring(0, person.ImageExtList.IndexOf(':')).Trim('\"');
                ImageViewer.CoreImageSource = img.Source as System.Windows.Media.Imaging.BitmapSource;
                this.NavigationService.Navigate(new Uri("/Pages/ImageViewer.xaml", UriKind.RelativeOrAbsolute));
            }
        }
    }
}