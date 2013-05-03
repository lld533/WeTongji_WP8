using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using WeTongji.Business;
using WeTongji.Api.Domain;
using WeTongji.DataBase;
using WeTongji.Utility;
using WeTongji.Api;
using System.Diagnostics;
using WeTongji.Pages;
using System.Threading;
using WeTongji.Api.Request;
using WeTongji.Api.Response;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows.Media.Imaging;

namespace WeTongji
{
    public partial class NearBy : PhoneApplicationPage
    {
        public NearBy()
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
            AroundExt an = null;

            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
            });

            #region [Get Around News in Database]

            var uri = ((String)param).TrimStart("/Pages/NearBy.xaml".ToCharArray());

            if (String.IsNullOrEmpty(uri))
            {
                using (var db = WTShareDataContext.ShareDB)
                {
                    an = db.AroundTable.LastOrDefault();
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
                            an = db.AroundTable.Where((news) => news.Id == id).SingleOrDefault();
                        }
                    }
                }
            }

            #endregion

            if (an != null)
            {
                #region [Binding]

                this.Dispatcher.BeginInvoke(() =>
                {
                    var tbs = an.Context.GetInlineCollection();
                    this.DataContext = an;
                    TextBlock_Loading.Visibility = Visibility.Collapsed;

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
                        var favObj = db.Favorites.Where((fo) => fo.Id == (int)FavoriteIndex.kAroundNews).SingleOrDefault();

                        if (favObj.Contains(an.Id))
                        {
                            an.CanFavorite = false;
                        }
                    }
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    InitAppBarButtons();
                });

                #endregion

                #region [Handle Images]

                #region [Title Image]

                if (!an.IsTitleImageExists())
                {
                    var client = new WTDownloadImageClient();

                    client.DownloadImageCompleted += (obj, arg) =>
                        {
                            this.Dispatcher.BeginInvoke(() =>
                            {
                                an.SendPropertyChanged("TitleImageBrush");
                            });
                        };

                    client.Execute(an.TitleImage, an.TitleImageGuid + "." + an.TitleImage.GetImageFileExtension());
                }

                #endregion

                #region [Images]
                if (an.IsIllustrated)
                {
                    var images = an.GetImageExts();

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ListBox_Pic.ItemsSource = images;
                    });

                    int count = images.Count();

                    for (int i = 0; i < count; ++i)
                    {
                        var img = images.ElementAt(i);
                        if (!an.ImageExists(i))
                        {
                            var client = new WTDownloadImageClient();
                            int j = i;

                            #region [Add event handlers]
                            client.DownloadImageCompleted += (obj, arg) =>
                            {
                                this.Dispatcher.BeginInvoke(() =>
                                {
                                    try
                                    {
                                        DependencyObject dependencyObj = ListBox_Pic.ItemContainerGenerator.ContainerFromIndex(j);

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

                                    img.SendPropertyChanged("ImageBrush");
                                });
                            };

                            client.DownloadImageFailed += (obj, arg) =>
                            {
                                this.Dispatcher.BeginInvoke(() =>
                                {
                                    try
                                    {
                                        DependencyObject dependencyObj = ListBox_Pic.ItemContainerGenerator.ContainerFromIndex(j);

                                        while (!(dependencyObj is Grid))
                                        {
                                            dependencyObj = VisualTreeHelper.GetChild(dependencyObj, 0);
                                        }

                                        var btn = VisualTreeHelper.GetChild(dependencyObj, 1) as Button;
                                        btn.Visibility = Visibility.Visible;
                                    }
                                    catch { }
                                });
                            };
                            #endregion

                            client.Execute(img.Url, img.Id + "." + img.Url.GetImageFileExtension());
                        }
                        //...Close "Loading" textblock & "Reload" button in ListBox_Pic
                        else
                        {
                            this.Dispatcher.BeginInvoke(() =>
                            {
                                try
                                {
                                    DependencyObject dependencyObj = ListBox_Pic.ItemContainerGenerator.ContainerFromIndex(i);

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

                #endregion
            }

            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Close();
            });
        }

        private void InitAppBarButtons()
        {
            var an = this.DataContext as AroundExt;

            if (an == null)
                return;

            if (String.IsNullOrEmpty(Global.Instance.CurrentUserID))
            {
                an.CanLike = true;
                an.CanFavorite = true;
            }

            if (an.CanLike)
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

            if (an.CanFavorite)
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
            var req = new AroundGetRequest<AroundGetResponse>();
            var client = new WTDefaultClient<AroundGetResponse>();

            req.Id = id;

            client.ExecuteCompleted += (obj, arg) =>
            {
                using (var db = WTShareDataContext.ShareDB)
                {
                    var news = db.AroundTable.Where((n) => n.Id == req.Id).SingleOrDefault();

                    if (news != null)
                    {
                        news.Like = arg.Result.Around.Like;
                    }

                    db.SubmitChanges();
                }

                if (isLikeButtonClicked)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var an = this.DataContext as AroundExt;

                        if (an.Like != arg.Result.Around.Like)
                        {
                            int previousLikeValue = an.Like;
                            an.Like = arg.Result.Around.Like;
                            an.SendPropertyChanged("Like");

                            //...Play animation
                            if (previousLikeValue < arg.Result.Around.Like)
                                (this.Resources["IncreaseLikeNumberAnimation"] as Storyboard).Begin();
                        }
                    });
                }
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var an = this.DataContext as AroundExt;
                        int previousLikeValue = an.Like;
                        an.Like = arg.Result.Around.Like;

                        an.SendPropertyChanged("Like");

                        //...Play animation
                        if (previousLikeValue > arg.Result.Around.Like)
                            (this.Resources["DecreaseLikeNumberAnimation"] as Storyboard).Begin();
                    });
                }
            };
            client.ExecuteFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    var an = this.DataContext as AroundExt;

                    if (isLikeButtonClicked)
                    {
                        ++an.Like;
                        an.SendPropertyChanged("Like");

                        //...Play animation
                        (this.Resources["IncreaseLikeNumberAnimation"] as Storyboard).Begin();
                    }
                    else
                    {
                        an.Like = Math.Max(0, an.Like - 1);
                        an.SendPropertyChanged("Like");

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
            var req = new AroundGetRequest<AroundGetResponse>();
            var client = new WTDefaultClient<AroundGetResponse>();

            req.Id = id;

            client.ExecuteCompleted += (obj, arg) =>
            {
                using (var db = WTShareDataContext.ShareDB)
                {
                    var news = db.AroundTable.Where((n) => n.Id == req.Id).SingleOrDefault();

                    if (news != null)
                    {
                        news.Favorite = arg.Result.Around.Favorite;
                    }

                    db.SubmitChanges();
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    var an = this.DataContext as AroundExt;

                    int previousValue = an.Favorite;
                    an.Favorite = arg.Result.Around.Favorite;
                    an.SendPropertyChanged("Favorite");

                    if (isFavoriteButtonClicked)
                    {
                        if (previousValue < an.Favorite)
                        {
                            (this.Resources["IncreaseFavoriteNumberAnimation"] as Storyboard).Begin();
                        }
                    }
                    else
                    {
                        if (previousValue > an.Favorite)
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
                    var an = this.DataContext as AroundExt;

                    if (isFavoriteButtonClicked)
                    {
                        an.Favorite++;
                        an.SendPropertyChanged("Favorite");
                        (this.Resources["IncreaseFavoriteNumberAnimation"] as Storyboard).Begin();
                    }
                    else
                    {
                        an.Favorite = Math.Max(0, an.Favorite - 1);
                        an.SendPropertyChanged("Favorite");
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
            var req = new AroundGetRequest<AroundGetResponse>();
            var client = new WTDefaultClient<AroundGetResponse>();

            req.Id = id;

            client.ExecuteCompleted += (obj, arg) =>
            {
                AroundExt itemInDB = null;
                using (var db = WTShareDataContext.ShareDB)
                {
                    itemInDB = db.AroundTable.Where((n) => n.Id == req.Id).SingleOrDefault();

                    if (itemInDB != null)
                    {
                        itemInDB.Read = arg.Result.Around.Read;
                        itemInDB.Favorite = arg.Result.Around.Favorite;
                        itemInDB.Like = arg.Result.Around.Like;
                        //...Do nothing with CanLike or CanFavorite
                    }
                    else
                        return;

                    db.SubmitChanges();
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    var an = this.DataContext as AroundExt;

                    if (an.Favorite != itemInDB.Favorite)
                    {
                        an.Favorite = itemInDB.Favorite;
                        an.SendPropertyChanged("Favorite");
                    }
                    if (an.Like != itemInDB.Like)
                    {
                        an.Like = itemInDB.Like;
                        an.SendPropertyChanged("Like");
                    }
                });
            };

            client.Execute(req);
        }

        /// <summary>
        /// Send Read request to server.
        /// </summary>
        /// <remarks>
        /// This function should be executed in UI thread.
        /// </remarks>
        private void ReadCurrentNews()
        {
            var req = new AroundReadRequest<WTResponse>();
            var client = new WTDefaultClient<WTResponse>();

            req.Id = (this.DataContext as AroundExt).Id;

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

                var req = new AroundLikeRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();
                req.Id = (this.DataContext as AroundExt).Id;

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
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.Recommends)).ToString()
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
                        var itemInDB = db.AroundTable.Where((news) => news.Id == req.Id).SingleOrDefault();

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

                var req = new AroundUnLikeRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();
                req.Id = (this.DataContext as AroundExt).Id;

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
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.Recommends)).ToString()
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
                        var itemInDB = db.AroundTable.Where((news) => news.Id == req.Id).SingleOrDefault();

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
                var req = new AroundFavoriteRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();

                req.Id = (this.DataContext as AroundExt).Id;

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
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.Recommends)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif
                    #endregion

                    AroundExt itemInDB = null;

                    if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            itemInDB = db.AroundTable.Where((news) => news.Id == req.Id).SingleOrDefault();

                            if (itemInDB != null)
                            {
                                itemInDB.CanFavorite = false;
                            }

                            db.SubmitChanges();
                        }

                        using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                        {
                            var fav = db.Favorites.Where((fo) => fo.Id == (int)FavoriteIndex.kAroundNews).SingleOrDefault();

                            fav.Add(req.Id);

                            db.SubmitChanges();
                        }

                        RefreshCurrentNewsOnFavoriteButtonClicked(req.Id, true);
                    }

                    //...Raise Global.FavoriteChanged
                    if (itemInDB != null)
                        Global.Instance.RaiseFavoriteChanged(itemInDB);

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
                var req = new AroundUnFavoriteRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();

                req.Id = (this.DataContext as AroundExt).Id;

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
                                    ((int)(FlurryWP8SDK.Models.ParameterValue.Recommends)).ToString()
                                    ),
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.Id).ToString(), 
                                    req.Id.ToString()
                                    )
                            })
                            );
#endif
                    #endregion

                    AroundExt itemInDB = null;

                    if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                    {
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            itemInDB = db.AroundTable.Where((news) => news.Id == req.Id).SingleOrDefault();

                            if (itemInDB != null)
                            {
                                itemInDB.CanFavorite = true;
                            }

                            db.SubmitChanges();
                        }

                        using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                        {
                            var fav = db.Favorites.Where((fo) => fo.Id == (int)FavoriteIndex.kAroundNews).SingleOrDefault();

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

        private void ViewTitleImage(Object sender, GestureEventArgs e)
        {
            var img = sender as Image;
            var an = this.DataContext as AroundExt;
            ImageViewer.CoreImageName = an.TitleImageGuid + "." + an.TitleImage.GetImageFileExtension();
            ImageViewer.CoreImageSource = img.Source as System.Windows.Media.Imaging.BitmapSource;
            this.NavigationService.Navigate(new Uri("/Pages/ImageViewer.xaml", UriKind.RelativeOrAbsolute));
        }
        #endregion

        #endregion

        #region [Functions]

        private void ViewMapAddress(object sender, RoutedEventArgs e)
        {
            var news = this.DataContext as AroundExt;

            if (news != null && !String.IsNullOrEmpty(news.Location))
                this.NavigationService.Navigate(new Uri("/Pages/MapAddress.xaml?q=" + news.Location, UriKind.RelativeOrAbsolute));
        }

        private void MakePhoneCall(Object sender, RoutedEventArgs e)
        {
            var pct = new PhoneCallTask();
            pct.DisplayName = TextBlock_Title.Text;
            pct.PhoneNumber = (sender as Button).Content.ToString();
            pct.Show();
        }

        #endregion
    }
}