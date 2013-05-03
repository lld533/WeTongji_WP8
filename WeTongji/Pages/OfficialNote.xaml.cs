using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WeTongji.Business;
using WeTongji.Api.Domain;
using WeTongji.DataBase;
using WeTongji.Utility;
using WeTongji.Api;
using System.Diagnostics;
using WeTongji.Pages;
using System.Threading;
using System.Windows.Media;
using WeTongji.Api.Response;
using WeTongji.Api.Request;
using System.Windows.Media.Animation;

namespace WeTongji
{
    public partial class OfficialNote : PhoneApplicationPage
    {
        public OfficialNote()
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

            if (e.NavigationMode == NavigationMode.New)
            {
                var thread = new Thread(new ParameterizedThreadStart(LoadData));
                thread.Start(e.Uri.ToString());
            }

        }

        #endregion

        #region [Init]

        private void LoadData(object param)
        {
            ForStaffExt fs = null;

            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
            });

            #region [Get Official Note in Database]

            var uri = ((String)param).TrimStart("/Pages/OfficialNote.xaml".ToCharArray());

            if (String.IsNullOrEmpty(uri))
            {
                using (var db = WTShareDataContext.ShareDB)
                {
                    fs = db.ForStaffTable.LastOrDefault();
                }
            }
            else
            {
                if (uri.StartsWith("?q="))
                {
                    uri = uri.TrimStart("?q=".ToCharArray());

                    int id;
                    if (Int32.TryParse(uri, out id))
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            fs = db.ForStaffTable.Where((news) => news.Id == id).SingleOrDefault();
                        }
                    }
                }
            }

            #endregion

            if (fs != null)
            {
                #region [Binding]

                this.Dispatcher.BeginInvoke(() =>
                {
                    var tbs = fs.Context.GetInlineCollection();
                    this.DataContext = fs;

                    ReadCurrentNews();

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
                        var favObj = db.Favorites.Where((fo) => fo.Id == (int)FavoriteIndex.kOfficialNotes).SingleOrDefault();

                        if (favObj.Contains(fs.Id))
                        {
                            fs.CanFavorite = false;
                        }
                    }
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    InitAppBarButtons();
                });

                #endregion

                #region [Handle Images]

                #region [illustrated]
                if (fs.IsIllustrated)
                {
                    var images = fs.GetImageExts();
                    var otherImages = images.Skip(1);

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        NoIllustrationGrid.Visibility = Visibility.Collapsed;
                        IllustrationGrid.Visibility = Visibility.Visible;

                        ListBox_Pic.ItemsSource = otherImages;
                    });

                    int count = images.Count();

                    for (int i = 0; i < count; ++i)
                    {
                        int j = i;
                        var img = images.ElementAt(i);
                        if (!fs.ImageExists(i))
                        {
                            var client = new WTDownloadImageClient();

                            #region [Add event handlers]
                            client.DownloadImageCompleted += (obj, arg) =>
                            {
                                this.Dispatcher.BeginInvoke(() =>
                                {
                                    if (j == 0)
                                        fs.SendPropertyChanged("FirstImageBrush");
                                    else
                                    {
                                        try
                                        {
                                            DependencyObject dependencyObj = ListBox_Pic.ItemContainerGenerator.ContainerFromIndex((j - 1));

                                            while (!(dependencyObj is Grid))
                                            {
                                                dependencyObj = VisualTreeHelper.GetChild(dependencyObj, 0);
                                            }

                                            var g = dependencyObj as Grid;
                                            var txtBlk = VisualTreeHelper.GetChild(g, 0) as TextBlock;
                                            txtBlk.Visibility = Visibility.Collapsed;
                                            var btn = VisualTreeHelper.GetChild(g, 1) as Button;
                                            btn.Visibility = Visibility.Collapsed;
                                        }
                                        catch { }
                                    }
                                    img.SendPropertyChanged("ImageBrush");
                                });
                            };

                            client.DownloadImageFailed += (obj, arg) =>
                            {
                                if (j > 0)
                                {
                                    this.Dispatcher.BeginInvoke(() =>
                                    {
                                        try
                                        {
                                            DependencyObject dependencyObj = ListBox_Pic.ItemContainerGenerator.ContainerFromIndex((j - 1));

                                            while (!(dependencyObj is Grid))
                                            {
                                                dependencyObj = VisualTreeHelper.GetChild(dependencyObj, 0);
                                            }

                                            var btn = VisualTreeHelper.GetChild(dependencyObj, 1) as Button;
                                            btn.Visibility = Visibility.Visible;
                                        }
                                        catch { }
                                    });
                                }
                            };
                            #endregion

                            client.Execute(img.Url, img.Id + "." + img.Url.GetImageFileExtension());
                        }
                        //...Close "Loading" textblock & "Reload" button in ListBox_Pic
                        else if (i > 0)
                        {
                            this.Dispatcher.BeginInvoke(() =>
                            {
                                try
                                {
                                    DependencyObject dependencyObj = ListBox_Pic.ItemContainerGenerator.ContainerFromIndex((j - 1));

                                    if (dependencyObj != null)
                                    {
                                        while (!(dependencyObj is Grid))
                                        {
                                            dependencyObj = VisualTreeHelper.GetChild(dependencyObj, 0);
                                        }

                                        var g = dependencyObj as Grid;
                                        var txtBlk = VisualTreeHelper.GetChild(g, 0) as TextBlock;
                                        txtBlk.Visibility = Visibility.Collapsed;
                                        var btn = VisualTreeHelper.GetChild(g, 1) as Button;
                                        btn.Visibility = Visibility.Collapsed;
                                    }
                                }
                                catch { }
                            });
                        }
                    }
                }
                #endregion
                #region [not illustrated]

                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        IllustrationGrid.Visibility = Visibility.Collapsed;
                        NoIllustrationGrid.Visibility = Visibility.Visible;
                    });
                }

                #endregion

                #endregion
            }

            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Close();
            });
        }

        private void InitAppBarButtons()
        {
            var fs = this.DataContext as ForStaffExt;

            if (fs == null)
                return;

            if (String.IsNullOrEmpty(Global.Instance.CurrentUserID))
            {
                fs.CanLike = true;
                fs.CanFavorite = true;
            }

            if (fs.CanLike)
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

            if (fs.CanFavorite)
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

        #region [Refresh related functions]

        /// <summary>
        /// Get the latest data of current news from the server and refresh Like number on UI
        /// </summary>
        /// <param name="isLikeButtonClicked">
        /// True if the function is called by clicking the Like button. 
        /// False if the function is called by clicking the Unlike button.
        /// </param>
        /// <remarks>
        /// If failed, "Like" +1 when Like button clicked while "Like" -1 when Unlike button clicked.
        /// </remarks>
        private void RefreshCurrentNewsOnLikeButtonClicked(int id, Boolean isLikeButtonClicked)
        {
            var req = new ForStaffGetRequest<ForStaffGetResponse>();
            var client = new WTDefaultClient<ForStaffGetResponse>();

            req.Id = id;

            client.ExecuteCompleted += (obj, arg) =>
            {
                using (var db = WTShareDataContext.ShareDB)
                {
                    var news = db.ForStaffTable.Where((n) => n.Id == req.Id).SingleOrDefault();

                    if (news != null)
                    {
                        news.Like = arg.Result.ForStaff.Like;
                    }

                    db.SubmitChanges();
                }

                if (isLikeButtonClicked)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var fs = this.DataContext as ForStaffExt;

                        if (fs.Like != arg.Result.ForStaff.Like)
                        {
                            int previousLikeValue = fs.Like;
                            fs.Like = arg.Result.ForStaff.Like;
                            fs.SendPropertyChanged("Like");

                            //...Play animation
                            if (previousLikeValue < arg.Result.ForStaff.Like)
                                (this.Resources["IncreaseLikeNumberAnimation"] as Storyboard).Begin();
                        }
                    });
                }
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var fs = this.DataContext as ForStaffExt;
                        int previousLikeValue = fs.Like;
                        fs.Like = arg.Result.ForStaff.Like;

                        fs.SendPropertyChanged("Like");

                        //...Play animation
                        if (previousLikeValue > arg.Result.ForStaff.Like)
                            (this.Resources["DecreaseLikeNumberAnimation"] as Storyboard).Begin();
                    });
                }
            };
            client.ExecuteFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    var fs = this.DataContext as ForStaffExt;

                    if (isLikeButtonClicked)
                    {
                        ++fs.Like;
                        fs.SendPropertyChanged("Like");

                        //...Play animation
                        (this.Resources["IncreaseLikeNumberAnimation"] as Storyboard).Begin();
                    }
                    else
                    {
                        fs.Like = Math.Max(0, fs.Like - 1);
                        fs.SendPropertyChanged("Like");

                        (this.Resources["DecreaseLikeNumberAnimation"] as Storyboard).Begin();
                    }
                });
            };

            client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
        }

        /// <summary>
        /// Get the latest Favorite number of current news from the server when user click fav/unfav button
        /// </summary>
        /// <remarks>
        /// If failed, "Favorite" +1 when Like button clicked while "Favorite" -1 when UnFavorite button clicked.
        /// </remarks>
        private void RefreshCurrentNewsOnFavoriteButtonClicked(int id, Boolean isFavoriteButtonClicked)
        {
            var req = new ForStaffGetRequest<ForStaffGetResponse>();
            var client = new WTDefaultClient<ForStaffGetResponse>();

            req.Id = id;

            client.ExecuteCompleted += (obj, arg) =>
            {
                using (var db = WTShareDataContext.ShareDB)
                {
                    var news = db.ForStaffTable.Where((n) => n.Id == req.Id).SingleOrDefault();

                    if (news != null)
                    {
                        news.Favorite = arg.Result.ForStaff.Favorite;
                    }

                    db.SubmitChanges();
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    var fs = this.DataContext as ForStaffExt;

                    int previousValue = fs.Favorite;
                    fs.Favorite = arg.Result.ForStaff.Favorite;
                    fs.SendPropertyChanged("Favorite");

                    if (isFavoriteButtonClicked)
                    {
                        if (previousValue < fs.Favorite)
                        {
                            (this.Resources["IncreaseFavoriteNumberAnimation"] as Storyboard).Begin();
                        }
                    }
                    else
                    {
                        if (previousValue > fs.Favorite)
                        {
                            (this.Resources["DecreaseFavoriteNumberAnimation"] as Storyboard).Begin();
                        }
                    }
                });
            };

            client.ExecuteFailed += (obj, arg) =>
            {
                //...Refresh UI and do nothing with database
                this.Dispatcher.BeginInvoke(() =>
                {
                    var fs = this.DataContext as ForStaffExt;

                    if (isFavoriteButtonClicked)
                    {
                        fs.Favorite++;
                        fs.SendPropertyChanged("Favorite");
                        (this.Resources["IncreaseFavoriteNumberAnimation"] as Storyboard).Begin();
                    }
                    else
                    {
                        fs.Favorite = Math.Max(0, fs.Favorite - 1);
                        fs.SendPropertyChanged("Favorite");
                        (this.Resources["DecreaseFavoriteNumberAnimation"] as Storyboard).Begin();
                    }
                });
            };

            client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
        }

        /// <summary>
        /// Refresh "Read" component of current piece of Tongji News.
        /// </summary>
        /// <remarks>
        /// This function should be executed in UI thread.
        /// </remarks>
        private void RefreshCurrentNewsOnRead(int id)
        {
            var req = new ForStaffGetRequest<ForStaffGetResponse>();
            var client = new WTDefaultClient<ForStaffGetResponse>();

            req.Id = id;

            client.ExecuteCompleted += (obj, arg) =>
            {
                ForStaffExt itemInDB = null;
                using (var db = WTShareDataContext.ShareDB)
                {
                    itemInDB = db.ForStaffTable.Where((n) => n.Id == req.Id).SingleOrDefault();

                    if (itemInDB != null)
                    {
                        itemInDB.Read = arg.Result.ForStaff.Read;
                        itemInDB.Favorite = arg.Result.ForStaff.Favorite;
                        itemInDB.Like = arg.Result.ForStaff.Like;
                        //...Do nothing with CanLike or CanFavorite
                    }
                    else
                        return;

                    db.SubmitChanges();
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    var fs = this.DataContext as ForStaffExt;

                    if (fs.Favorite != itemInDB.Favorite)
                    {
                        fs.Favorite = itemInDB.Favorite;
                        fs.SendPropertyChanged("Favorite");
                    }
                    if (fs.Like != itemInDB.Like)
                    {
                        fs.Like = itemInDB.Like;
                        fs.SendPropertyChanged("Like");
                    }
                });
            };

            if (String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                client.Execute(req);
            else
                client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
        }

        /// <summary>
        /// Send Read request to server.
        /// </summary>
        /// <remarks>
        /// This function should be executed in UI thread.
        /// </remarks>
        private void ReadCurrentNews()
        {
            var req = new ForStaffReadRequest<WTResponse>();
            var client = new WTDefaultClient<WTResponse>();

            req.Id = (this.DataContext as ForStaffExt).Id;

            client.ExecuteCompleted += (o, e) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    RefreshCurrentNewsOnRead(req.Id);
                });
            };

            if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
            else
                client.Execute(req);
        }

        #endregion

        #region [Visual]

        private void Button_ReloadImage_Click(Object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var imgExt = btn.DataContext as ImageExt;
            String imgFileName = imgExt.Id + "." + imgExt.Url.GetImageFileExtension();

            btn.Visibility = Visibility.Collapsed;

            var client = new WTDownloadImageClient();

            client.DownloadImageCompleted += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    imgExt.SendPropertyChanged("ImageBrush");

                    try
                    {
                        var g = VisualTreeHelper.GetParent(btn);
                        var txtBlk = VisualTreeHelper.GetChild(g, 0) as TextBlock;
                        txtBlk.Visibility = Visibility.Collapsed;
                        btn.Visibility = Visibility.Collapsed;
                    }
                    catch { }
                });
            };

            client.DownloadImageFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    btn.Visibility = Visibility.Visible;
                });
            };

            client.Execute(imgExt.Url, imgExt.Id + "." + imgExt.Url.GetImageFileExtension());
        }

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

                var req = new ForStaffLikeRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();
                req.Id = (this.DataContext as ForStaffExt).Id;

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
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.OfficialNote)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif
                    #endregion

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        var itemInDB = db.ForStaffTable.Where((news) => news.Id == req.Id).SingleOrDefault();

                        if (itemInDB != null)
                        {
                            itemInDB.CanLike = false;
                            db.SubmitChanges();
                        }
                    }

                    RefreshCurrentNewsOnLikeButtonClicked(req.Id, true);

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var btn = ApplicationBar.Buttons[0] as ApplicationBarIconButton;

                        btn.IconUri = new Uri("/icons/appbar.unlike.rest.png", UriKind.RelativeOrAbsolute);
                        btn.Text = StringLibrary.Common_AppBarUnlikeText;
                        btn.IsEnabled = true;
                        btn.Click -= Button_Like_Clicked;
                        btn.Click += Button_Unlike_Clicked;

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

                var req = new ForStaffUnLikeRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();
                req.Id = (this.DataContext as ForStaffExt).Id;

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
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.OfficialNote)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif
                    #endregion

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        var itemInDB = db.ForStaffTable.Where((news) => news.Id == req.Id).SingleOrDefault();

                        if (itemInDB != null)
                        {
                            itemInDB.CanLike = true;
                            db.SubmitChanges();
                        }
                    }

                    RefreshCurrentNewsOnLikeButtonClicked(req.Id, false);

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var btn = ApplicationBar.Buttons[0] as ApplicationBarIconButton;

                        btn.IconUri = new Uri("/icons/appbar.like.rest.png", UriKind.RelativeOrAbsolute);
                        btn.Text = StringLibrary.Common_AppBarLikeText;
                        btn.IsEnabled = true;
                        btn.Click += Button_Like_Clicked;
                        btn.Click -= Button_Unlike_Clicked;

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
                var req = new ForStaffFavoriteRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();

                req.Id = (this.DataContext as ForStaffExt).Id;

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
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.OfficialNote)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif
                    #endregion

                    ForStaffExt itemInDB = null;

                    if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            itemInDB = db.ForStaffTable.Where((news) => news.Id == req.Id).SingleOrDefault();

                            if (itemInDB != null)
                            {
                                itemInDB.CanFavorite = false;
                            }

                            db.SubmitChanges();
                        }

                        using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                        {
                            var fav = db.Favorites.Where((fo) => fo.Id == (int)FavoriteIndex.kOfficialNotes).SingleOrDefault();

                            fav.Add(req.Id);

                            db.SubmitChanges();
                        }

                        //...Raise Global.FavoriteChanged
                        if (itemInDB != null)
                            Global.Instance.RaiseFavoriteChanged(itemInDB);

                        RefreshCurrentNewsOnFavoriteButtonClicked(req.Id, true);
                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var favBtn = this.ApplicationBar.Buttons[1] as ApplicationBarIconButton;
                        favBtn.IconUri = new Uri("/icons/appbar.unfavourite.rest.png", UriKind.RelativeOrAbsolute);
                        favBtn.Text = StringLibrary.Common_AppBarUnfavoriteText;
                        favBtn.Click -= Button_Favorite_Clicked;
                        favBtn.Click += Button_Unfavorite_Clicked;
                        favBtn.IsEnabled = true;

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
                var req = new ForStaffUnFavoriteRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();

                req.Id = (this.DataContext as ForStaffExt).Id;

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
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.OfficialNote)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif
                    #endregion

                    ForStaffExt itemInDB = null;

                    if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            itemInDB = db.ForStaffTable.Where((news) => news.Id == req.Id).SingleOrDefault();

                            if (itemInDB != null)
                            {
                                itemInDB.CanFavorite = true;
                            }

                            db.SubmitChanges();
                        }

                        using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                        {
                            var fav = db.Favorites.Where((fo) => fo.Id == (int)FavoriteIndex.kOfficialNotes).SingleOrDefault();

                            fav.Remove(req.Id);

                            db.SubmitChanges();
                        }

                        RefreshCurrentNewsOnFavoriteButtonClicked(req.Id, false);
                    }
                    
                    //...Raise Global.FavoriteChanged
                    if (itemInDB != null)
                        Global.Instance.RaiseFavoriteChanged(itemInDB);

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var favBtn = this.ApplicationBar.Buttons[1] as ApplicationBarIconButton;
                        favBtn.IconUri = new Uri("/icons/appbar.favs.rest.png", UriKind.RelativeOrAbsolute);
                        favBtn.Text = StringLibrary.Common_AppBarFavoriteText;
                        favBtn.Click += Button_Favorite_Clicked;
                        favBtn.Click -= Button_Unfavorite_Clicked;
                        favBtn.IsEnabled = true;

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

        #region [Tap and View image]
        private void ViewOriginalImage(Object sender, GestureEventArgs e)
        {
            var img = sender as Image;
            var imgExt = img.DataContext as ImageExt;
            ImageViewer.CoreImageName = imgExt.Id;
            ImageViewer.CoreImageSource = img.Source as System.Windows.Media.Imaging.BitmapSource;
            this.NavigationService.Navigate(new Uri("/Pages/ImageViewer.xaml", UriKind.RelativeOrAbsolute));
        }

        private void ViewFirstImage(Object sender, GestureEventArgs e)
        {
            var img = sender as Image;
            var sn = this.DataContext as ForStaffExt;
            ImageViewer.CoreImageName = sn.ImageExtList.GetImageFilesNames().First();
            ImageViewer.CoreImageSource = img.Source as System.Windows.Media.Imaging.BitmapSource;
            this.NavigationService.Navigate(new Uri("/Pages/ImageViewer.xaml", UriKind.RelativeOrAbsolute));
        }
        #endregion

        #endregion
    }
}