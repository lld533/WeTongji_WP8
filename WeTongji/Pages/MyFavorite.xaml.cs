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
using System.Text.RegularExpressions;
using System.Text;
using WeTongji.Api.Domain;
using WeTongji.DataBase;
using WeTongji.Business;
using System.Collections.ObjectModel;
using WeTongji.Pages;
using System.Threading;
using WeTongji.Api;
using WeTongji.Utility;

namespace WeTongji
{
    public partial class MyFavorite : PhoneApplicationPage
    {
        public MyFavorite()
        {
            InitializeComponent();
        }

        #region [Overridden]

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New || e.NavigationMode == NavigationMode.Back)
            {
                var thread = new Thread(new ThreadStart(LoadDataFromDatabase));

                ProgressBarPopup.Instance.Open();
                thread.Start();
            }
        }

        #endregion

        #region [Nav]

        private void ListBox_Activity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lb = sender as ListBox;

            if (lb.SelectedIndex == -1)
                return;

            var a = lb.SelectedItem as ActivityExt;
            lb.SelectedIndex = -1;
            this.NavigationService.Navigate(new Uri("/Pages/Activity.xaml?q=" + a.Id, UriKind.RelativeOrAbsolute));
        }

        private void ListBox_PeopleOfWeek_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            var lb = sender as ListBox;

            if (lb.SelectedIndex == -1)
                return;

            var p = lb.SelectedItem as PersonExt;
            lb.SelectedIndex = -1;
            this.NavigationService.Navigate(new Uri("/Pages/PeopleOfWeek.xaml?q=" + p.Id, UriKind.RelativeOrAbsolute));
        }

        private void Listbox_CampusInfo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lb = sender as ListBox;

            if (lb.SelectedIndex == -1)
                return;

            var ci = lb.SelectedItem as ICampusInfo;
            lb.SelectedIndex = -1;

            if (ci is SchoolNewsExt)
            {
                this.NavigationService.Navigate(new Uri("/Pages/TongjiNews.xaml?q=" + ci.Id, UriKind.RelativeOrAbsolute));
            }
            else if (ci is AroundExt)
            {
                this.NavigationService.Navigate(new Uri("/Pages/NearBy.xaml?q=" + ci.Id, UriKind.RelativeOrAbsolute));
            }
            else if (ci is ForStaffExt)
            {
                this.NavigationService.Navigate(new Uri("/Pages/OfficialNote.xaml?q=" + ci.Id, UriKind.RelativeOrAbsolute));
            }
            else if (ci is ClubNewsExt)
            {
                this.NavigationService.Navigate(new Uri("/Pages/SocietyNews.xaml?q=" + ci.Id, UriKind.RelativeOrAbsolute));
            }


        }

        #endregion

        #region [Load Data]

        private void LoadDataFromDatabase()
        {
            FavoriteObject[] foArr;
            FavoriteObject currentFavoriteObject;
            String[] strArr;
            int count, id;

            using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
            {
                foArr = db.Favorites.ToArray();
            }

            if (foArr != null)
            {
                #region [Campus Info]

                ICampusInfo ci;
                List<ICampusInfo> list = new List<ICampusInfo>();

                #region [School news]

                currentFavoriteObject = foArr.Where((fo) => fo.Id == (int)FavoriteIndex.kTongjiNews).SingleOrDefault();

                if (currentFavoriteObject != null)
                {
                    strArr = currentFavoriteObject.Value.Split("_".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    count = strArr.Count();
                    if (count > 0)
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            Int32.TryParse(strArr[i], out id);

                            using (var db = WTShareDataContext.ShareDB)
                            {
                                ci = db.SchoolNewsTable.Where((news) => news.Id == id).SingleOrDefault();
                                if (ci != null)
                                    list.Add(ci);
                            }
                        }
                    }
                }

                #endregion

                #region [Around news]

                currentFavoriteObject = foArr.Where((fo) => fo.Id == (int)FavoriteIndex.kAroundNews).SingleOrDefault();

                if (currentFavoriteObject != null)
                {
                    strArr = currentFavoriteObject.Value.Split("_".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    count = strArr.Count();
                    if (count > 0)
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            Int32.TryParse(strArr[i], out id);

                            using (var db = WTShareDataContext.ShareDB)
                            {
                                ci = db.AroundTable.Where((news) => news.Id == id).SingleOrDefault();
                                if (ci != null)
                                    list.Add(ci);
                            }
                        }
                    }
                }

                #endregion

                #region [Official notes]

                currentFavoriteObject = foArr.Where((fo) => fo.Id == (int)FavoriteIndex.kOfficialNotes).SingleOrDefault();

                if (currentFavoriteObject != null)
                {
                    strArr = currentFavoriteObject.Value.Split("_".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    count = strArr.Count();
                    if (count > 0)
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            Int32.TryParse(strArr[i], out id);

                            using (var db = WTShareDataContext.ShareDB)
                            {
                                ci = db.ForStaffTable.Where((news) => news.Id == id).SingleOrDefault();
                                if (ci != null)
                                    list.Add(ci);
                            }
                        }
                    }
                }

                #endregion

                #region [Club news]

                currentFavoriteObject = foArr.Where((fo) => fo.Id == (int)FavoriteIndex.kClubNews).SingleOrDefault();

                if (currentFavoriteObject != null)
                {
                    strArr = currentFavoriteObject.Value.Split("_".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    count = strArr.Count();
                    if (count > 0)
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            Int32.TryParse(strArr[i], out id);

                            using (var db = WTShareDataContext.ShareDB)
                            {
                                ci = db.ClubNewsTable.Where((news) => news.Id == id).SingleOrDefault();
                                if (ci != null)
                                    list.Add(ci);
                            }
                        }
                    }
                }

                #endregion

                if (list.Count > 0)
                {
                    var q = from ICampusInfo info in list
                            orderby info.CreatedAt descending
                            select info;

                    var collection = new ObservableCollection<ICampusInfo>(q);

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ListBox_CampusInfo.ItemsSource = collection;
                        ListBox_CampusInfo.Visibility = Visibility.Visible;
                    });
                }
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ListBox_CampusInfo.ItemsSource = null;
                        ListBox_CampusInfo.Visibility = Visibility.Collapsed;
                    });
                }
                #endregion

                #region [People Of week]

                currentFavoriteObject = foArr.Where((fo) => fo.Id == (int)FavoriteIndex.kPeopleOfWeek).SingleOrDefault();

                if (currentFavoriteObject != null && !String.IsNullOrEmpty(currentFavoriteObject.Value))
                {
                    List<PersonExt> people = new List<PersonExt>();

                    strArr = currentFavoriteObject.Value.Split("_".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    count = strArr.Count();

                    if (count > 0)
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            Int32.TryParse(strArr[i], out id);

                            using (var db = WTShareDataContext.ShareDB)
                            {
                                var p = db.People.Where((person) => person.Id == id).SingleOrDefault();
                                if (p != null)
                                    people.Add(p);
                            }
                        }

                        var q = from PersonExt p in people
                                orderby p.Id descending
                                select p;

                        var src = new ObservableCollection<PersonExt>(q);

                        this.Dispatcher.BeginInvoke(() =>
                        {
                            if (src.Count > 0)
                            {
                                ListBox_PeopleOfWeek.ItemsSource = src;
                                ListBox_PeopleOfWeek.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                ListBox_PeopleOfWeek.ItemsSource = null;
                                ListBox_PeopleOfWeek.Visibility = Visibility.Collapsed;
                            }
                        });
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            ListBox_PeopleOfWeek.ItemsSource = null;
                            ListBox_PeopleOfWeek.Visibility = Visibility.Collapsed;
                        });
                    }
                }
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ListBox_PeopleOfWeek.ItemsSource = null;
                        ListBox_PeopleOfWeek.Visibility = Visibility.Collapsed;
                    });
                }

                #endregion

                #region [Activities]

                currentFavoriteObject = foArr.Where((fo) => fo.Id == (int)FavoriteIndex.kActivity).SingleOrDefault();

                if (currentFavoriteObject != null && !String.IsNullOrEmpty(currentFavoriteObject.Value))
                {
                    List<ActivityExt> activities = new List<ActivityExt>();

                    strArr = currentFavoriteObject.Value.Split("_".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    count = strArr.Count();

                    if (count > 0)
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            Int32.TryParse(strArr[i], out id);

                            using (var db = WTShareDataContext.ShareDB)
                            {
                                var a = db.Activities.Where((person) => person.Id == id).SingleOrDefault();
                                if (a != null)
                                    activities.Add(a);
                            }
                        }

                        var q = from ActivityExt a in activities
                                orderby a.Id descending
                                select a;

                        var src = new ObservableCollection<ActivityExt>(q);
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            if (src.Count > 0)
                            {
                                ListBox_Activity.Visibility = Visibility.Visible;
                                ListBox_Activity.ItemsSource = src;
                            }
                            else
                            {
                                ListBox_Activity.ItemsSource = null;
                                ListBox_Activity.Visibility = Visibility.Collapsed;
                            }
                        });
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            ListBox_Activity.ItemsSource = null;
                            ListBox_Activity.Visibility = Visibility.Collapsed;
                        });
                    }
                }
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ListBox_Activity.ItemsSource = null;
                        ListBox_Activity.Visibility = Visibility.Collapsed;
                    });
                }

                #endregion
            }

            this.Dispatcher.BeginInvoke(() =>
            {
                DownloadImages();
                ProgressBarPopup.Instance.Close();
            });
        }

        private void DownloadImages()
        {
            ObservableCollection<PersonExt> people;
            ObservableCollection<ICampusInfo> campusInfo;

            if (ListBox_PeopleOfWeek.ItemsSource != null)
            {
                people = ListBox_PeopleOfWeek.ItemsSource as ObservableCollection<PersonExt>;

                for (int i = 0; i < people.Count; ++i)
                {
                    try
                    {
                        var p = people.ElementAt(i);
                        if (!p.AvatarExists())
                        {
                            var client = new WTDownloadImageClient();

                            client.DownloadImageCompleted += (o, e) =>
                                {
                                    this.Dispatcher.BeginInvoke(() =>
                                    {
                                        p.SendPropertyChanged("AvatarImageBrush");
                                    });
                                };

                            client.Execute(p.Avatar, p.AvatarGuid + "." + p.Avatar.GetImageFileExtension());
                        }
                    }
                    catch { }
                }
            }

            if (ListBox_CampusInfo.ItemsSource != null)
            {
                campusInfo = ListBox_CampusInfo.ItemsSource as ObservableCollection<ICampusInfo>;

                for (int i = 0; i < campusInfo.Count; ++i)
                {
                    try
                    {
                        var ci = campusInfo.ElementAt(i);
                        if (ci.IsIllustrated && !ci.CampusInfoImageExists)
                        {
                            var client = new WTDownloadImageClient();

                            client.DownloadImageCompleted += (o, e) =>
                            {
                                this.Dispatcher.BeginInvoke(() =>
                                {
                                    if (ci is SchoolNewsExt)
                                    {
                                        (ci as SchoolNewsExt).SendPropertyChanged("CampusInfoImageBrush");
                                    }
                                    else if (ci is AroundExt)
                                    {
                                        (ci as AroundExt).SendPropertyChanged("CampusInfoImageBrush");
                                    }
                                    else if (ci is ForStaffExt)
                                    {
                                        (ci as ForStaffExt).SendPropertyChanged("CampusInfoImageBrush");
                                    }
                                    else if (ci is ClubNewsExt)
                                    {
                                        (ci as ClubNewsExt).SendPropertyChanged("CampusInfoImageBrush");
                                    }
                                });
                            };

                            client.Execute(ci.CampusInfoImageUrl, ci.CampusInfoImageFileName);
                        }
                    }
                    catch { }
                }
            }

        }

        #endregion
    }
}