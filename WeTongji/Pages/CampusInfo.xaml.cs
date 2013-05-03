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
using WeTongji.DataBase;
using WeTongji.Pages;
using WeTongji.Api.Domain;
using WeTongji.Business;
using WeTongji.Api;
using System.Diagnostics;
using WeTongji.Utility;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using WeTongji.Api.Request;
using WeTongji.Api.Response;
using Microsoft.Phone.Shell;

namespace WeTongji
{
    public partial class CampusInfo : PhoneApplicationPage
    {
        private Boolean isLoadingMoreTongjiNews = false;

        private Boolean isLoadingMoreAroundNews = false;

        private Boolean isLoadingMoreOfficialNotes = false;

        private Boolean isLoadingMoreClubNews = false;

        private SourceState tongjiNewsSourceState = SourceState.NotSet;
        private SourceState aroundNewsSourceState = SourceState.NotSet;
        private SourceState officialNotesSourceState = SourceState.NotSet;
        private SourceState clubNewsSourceState = SourceState.NotSet;

        #region [Properties]

        private ObservableCollection<SchoolNewsExt> SchoolNewsSource
        {
            get
            {
                return ListBox_TongjiNews.ItemsSource == null
                    ? null
                    : ListBox_TongjiNews.ItemsSource as ObservableCollection<SchoolNewsExt>;
            }
            set
            {
                if (value == null || value.Count == 0)
                {
                    var src = SchoolNewsSource;
                    if (src != null)
                        src.CollectionChanged -= SchoolNewsSourceChanged;

                    ListBox_TongjiNews.ItemsSource = null;
                    TextBlock_NoTongjiNews.Visibility = Visibility.Visible;
                }
                else
                {
                    if (SchoolNewsSource != null)
                        SchoolNewsSource.CollectionChanged -= SchoolNewsSourceChanged;

                    ObservableCollection<SchoolNewsExt> tmp = null;
                    ListBox_TongjiNews.ItemsSource = tmp = new ObservableCollection<SchoolNewsExt>();

                    tmp.CollectionChanged += SchoolNewsSourceChanged;
                    foreach (var item in value)
                        tmp.Add(item);

                    ListBox_TongjiNews.Visibility = Visibility.Visible;
                    TextBlock_NoTongjiNews.Visibility = Visibility.Collapsed;
                }
            }
        }

        private ObservableCollection<AroundExt> AroundNewsSource
        {
            get
            {
                return ListBox_NearBy.ItemsSource == null
                    ? null
                    : ListBox_NearBy.ItemsSource as ObservableCollection<AroundExt>;
            }
            set
            {
                if (value == null || value.Count == 0)
                {
                    var src = AroundNewsSource;
                    if (src != null)
                        src.CollectionChanged -= AroundNewsSourceChanged;

                    ListBox_NearBy.ItemsSource = null;
                    TextBlock_NoAroundNews.Visibility = Visibility.Visible;
                }
                else
                {
                    if (AroundNewsSource != null)
                        AroundNewsSource.CollectionChanged -= AroundNewsSourceChanged;

                    ObservableCollection<AroundExt> tmp = null;
                    ListBox_NearBy.ItemsSource = tmp = new ObservableCollection<AroundExt>();

                    tmp.CollectionChanged += AroundNewsSourceChanged;
                    foreach (var item in value)
                        tmp.Add(item);

                    ListBox_NearBy.Visibility = Visibility.Visible;
                    TextBlock_NoAroundNews.Visibility = Visibility.Collapsed;
                }
            }
        }

        private ObservableCollection<ForStaffExt> OfficialNotesSource
        {
            get
            {
                return ListBox_OfficialNotes.ItemsSource == null
                    ? null
                    : ListBox_OfficialNotes.ItemsSource as ObservableCollection<ForStaffExt>;
            }
            set
            {
                if (value == null || value.Count == 0)
                {
                    var src = OfficialNotesSource;
                    if (src != null)
                        src.CollectionChanged -= OfficalNoteSourceChanged;

                    ListBox_OfficialNotes.ItemsSource = null;
                    TextBlock_NoOfficialNote.Visibility = Visibility.Visible;
                }
                else
                {
                    if (OfficialNotesSource != null)
                        OfficialNotesSource.CollectionChanged -= OfficalNoteSourceChanged;

                    ObservableCollection<ForStaffExt> tmp = null;
                    ListBox_OfficialNotes.ItemsSource = tmp = new ObservableCollection<ForStaffExt>();

                    tmp.CollectionChanged += OfficalNoteSourceChanged;
                    foreach (var item in value)
                        tmp.Add(item);

                    ListBox_OfficialNotes.Visibility = Visibility.Visible;
                    TextBlock_NoOfficialNote.Visibility = Visibility.Collapsed;
                }
            }
        }

        private ObservableCollection<ClubNewsExt> ClubNewsSource
        {
            get
            {
                return ListBox_SocietyNews.ItemsSource == null
                    ? null
                    : ListBox_SocietyNews.ItemsSource as ObservableCollection<ClubNewsExt>;
            }
            set
            {
                if (value == null || value.Count == 0)
                {
                    var src = ClubNewsSource;
                    if (src != null)
                        src.CollectionChanged -= ClubNewsSourceChanged;

                    ListBox_SocietyNews.ItemsSource = null;
                    TextBlock_NoClubNews.Visibility = Visibility.Visible;
                }
                else
                {
                    if (ClubNewsSource != null)
                        ClubNewsSource.CollectionChanged -= ClubNewsSourceChanged;

                    ObservableCollection<ClubNewsExt> tmp = null;
                    ListBox_SocietyNews.ItemsSource = tmp = new ObservableCollection<ClubNewsExt>();

                    tmp.CollectionChanged += ClubNewsSourceChanged;
                    foreach (var item in value)
                        tmp.Add(item);


                    ListBox_SocietyNews.Visibility = Visibility.Visible;
                    TextBlock_NoClubNews.Visibility = Visibility.Collapsed;
                }
            }
        }

        #endregion

        public CampusInfo()
        {
            InitializeComponent();

            var button = new ApplicationBarIconButton(new Uri("/icons/appbar.refresh.rest.png", UriKind.RelativeOrAbsolute))
            {
                Text = StringLibrary.CampusInfo_AppBarRefreshText
            };
            button.Click += Refresh_Button_Clicked;
            this.ApplicationBar.Buttons.Add(button);
        }

        private void SchoolNewsSourceChanged(Object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
                return;

            foreach (var item in e.NewItems)
            {
                var sn = item as SchoolNewsExt;

                if (!sn.IsInvalidSchoolNews && !String.IsNullOrEmpty(sn.ImageExtList) && !sn.ImageExists())
                {
                    var client = new WTDownloadImageClient();

                    client.DownloadImageCompleted += (obj, arg) =>
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            sn.SendPropertyChanged("FirstImageBrush");
                        });
                    };

                    client.Execute(sn.GetImagesURL().First(), sn.ImageExtList.GetImageFilesNames().First());
                }
            }
        }
        private void AroundNewsSourceChanged(Object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
                return;

            foreach (var item in e.NewItems)
            {
                var an = item as AroundExt;

                if (!an.IsInvalidAround && !String.IsNullOrEmpty(an.TitleImage) && !an.IsTitleImageExists())
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
            }
        }
        private void OfficalNoteSourceChanged(Object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
                return;

            foreach (var item in e.NewItems)
            {
                var fs = item as ForStaffExt;

                if (!fs.IsInvalidForStaff && !String.IsNullOrEmpty(fs.ImageExtList) && !fs.ImageExists())
                {
                    var client = new WTDownloadImageClient();

                    client.DownloadImageCompleted += (obj, arg) =>
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            fs.SendPropertyChanged("FirstImageBrush");
                        });
                    };

                    client.Execute(fs.GetImagesURL().First(), fs.ImageExtList.GetImageFilesNames().First());
                }
            }
        }
        private void ClubNewsSourceChanged(Object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
                return;

            foreach (var item in e.NewItems)
            {
                var cn = item as ClubNewsExt;

                if (!cn.IsInvalidClubNews && !String.IsNullOrEmpty(cn.ImageExtList) && !cn.ImageExists())
                {
                    var client = new WTDownloadImageClient();

                    client.DownloadImageCompleted += (obj, arg) =>
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            cn.SendPropertyChanged("FirstImageBrush");
                        });
                    };

                    client.Execute(cn.GetImagesURL().First(), cn.ImageExtList.GetImageFilesNames().First());
                }
            }
        }

        #region [Overridden]

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>
        /// [Query] like /Pages/CampusInfo.xaml?q={Int32}
        /// where the query string is the selected index of the Pivot
        /// </remarks>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                var str = e.Uri.ToString();
                var q = str.Substring(str.IndexOf('?'));
                q = q.TrimStart("?q=".ToCharArray());

                Int32 idx = 0;
                Int32.TryParse(q, out idx);
                idx = Math.Max(idx, 0);
                idx = idx > Pivot_Core.Items.Count ? 0 : idx;

                Pivot_Core.SelectedIndex = idx;

                var thread = new System.Threading.Thread(new System.Threading.ThreadStart(LoadData));
                thread.Start();
            }
        }

        private void LoadData()
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
            });

            #region [School News]
            SchoolNewsExt[] snArr = null;
            using (var db = WTShareDataContext.ShareDB)
            {
                snArr = db.SchoolNewsTable.ToArray();
            }
            if (snArr != null && snArr.Count() > 0)
            {
                var src = snArr.OrderByDescending((news) => news.CreatedAt);

                this.Dispatcher.BeginInvoke(() =>
                {
                    if (src.Count() > 5)
                    {
                        SchoolNewsSource = new ObservableCollection<SchoolNewsExt>(src.Take(5));
                    }
                    else
                    {
                        SchoolNewsSource = new ObservableCollection<SchoolNewsExt>(src);
                    }
                });
            }
            else
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    SchoolNewsSource = null;
                });
            }
            #endregion

            #region [Around News]
            AroundExt[] anArr = null;
            using (var db = WTShareDataContext.ShareDB)
            {
                anArr = db.AroundTable.ToArray();
            }
            if (anArr != null && anArr.Count() > 0)
            {
                var src = anArr.OrderByDescending((news) => news.CreatedAt);

                this.Dispatcher.BeginInvoke(() =>
                {
                    if (anArr.Count() > 5)
                    {
                        AroundNewsSource = new ObservableCollection<AroundExt>(src.Take(5));
                    }
                    else
                    {
                        AroundNewsSource = new ObservableCollection<AroundExt>(src);
                    }
                });
            }
            else
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    AroundNewsSource = null;
                });
            }
            #endregion

            #region [Official Notes]
            ForStaffExt[] fsArr = null;
            using (var db = WTShareDataContext.ShareDB)
            {
                fsArr = db.ForStaffTable.ToArray();
            }
            if (fsArr != null && fsArr.Count() > 0)
            {
                var src = fsArr.OrderByDescending((news) => news.CreatedAt);

                this.Dispatcher.BeginInvoke(() =>
                {
                    if (fsArr.Count() > 5)
                    {
                        OfficialNotesSource = new ObservableCollection<ForStaffExt>(src.Take(5));
                    }
                    else
                    {
                        OfficialNotesSource = new ObservableCollection<ForStaffExt>(src);
                    }
                });
            }
            else
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    OfficialNotesSource = null;
                });
            }
            #endregion

            #region [Club News]
            ClubNewsExt[] cnArr = null;
            using (var db = WTShareDataContext.ShareDB)
            {
                cnArr = db.ClubNewsTable.ToArray();
            }
            if (cnArr != null && cnArr.Count() > 0)
            {
                var src = cnArr.OrderByDescending((news) => news.CreatedAt);

                this.Dispatcher.BeginInvoke(() =>
                {
                    if (cnArr.Count() > 5)
                    {
                        ClubNewsSource = new ObservableCollection<ClubNewsExt>(src.Take(5));
                    }
                    else
                    {
                        ClubNewsSource = new ObservableCollection<ClubNewsExt>(src);
                    }
                });
            }
            else
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ClubNewsSource = null;
                });
            }
            #endregion

            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Close();
            });
        }

        #endregion

        #region [Nav]

        private void Listbox_TongjiNews_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lb = sender as ListBox;

            if (lb.SelectedIndex == -1)
                return;

            var item = lb.SelectedItem as SchoolNewsExt;
            lb.SelectedIndex = -1;

            if (item != null && !item.IsInvalidSchoolNews)
                this.NavigationService.Navigate(new Uri("/Pages/TongjiNews.xaml?q=" + item.Id, UriKind.RelativeOrAbsolute));
        }

        private void ListBox_NearBy_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            var lb = sender as ListBox;

            if (lb.SelectedIndex == -1)
                return;

            var item = lb.SelectedItem as AroundExt;
            lb.SelectedIndex = -1;

            if (item != null && !item.IsInvalidAround)
                this.NavigationService.Navigate(new Uri("/Pages/NearBy.xaml?q=" + item.Id, UriKind.RelativeOrAbsolute));
        }

        private void Listbox_OfficialNotes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lb = sender as ListBox;

            if (lb.SelectedIndex == -1)
                return;

            var item = lb.SelectedItem as ForStaffExt;
            lb.SelectedIndex = -1;

            if (item != null && !item.IsInvalidForStaff)
                this.NavigationService.Navigate(new Uri("/Pages/OfficialNote.xaml?q=" + item.Id, UriKind.RelativeOrAbsolute));
        }

        private void ListBox_SocietyNews_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lb = sender as ListBox;

            if (lb.SelectedIndex == -1)
                return;

            var item = lb.SelectedItem as ClubNewsExt;
            lb.SelectedIndex = -1;

            if (item != null && !item.IsInvalidClubNews)
                this.NavigationService.Navigate(new Uri("/Pages/SocietyNews.xaml?q=" + item.Id, UriKind.RelativeOrAbsolute));
        }

        #endregion

        #region [MouseMove]

        #region [Tongji News]

        private void ListBox_TongjiNews_MouseMove(Object sender, MouseEventArgs e)
        {
            var lb = sender as ListBox;
            DependencyObject obj = lb;

            while (!(obj is ScrollViewer) && obj != null)
            {
                obj = VisualTreeHelper.GetChild(obj, 0);
            }

            if (obj != null)
            {
                var sv = obj as ScrollViewer;

                //...Scroll to the end
                if (!isLoadingMoreTongjiNews && Math.Abs(sv.ScrollableHeight - sv.VerticalOffset) <= 0.1)
                {
                    var src = SchoolNewsSource;
                    if (src != null && src.Count > 0)
                    {
                        if (!src.Last().IsInvalidSchoolNews)
                        {
                            var thread = new Thread(new ParameterizedThreadStart(LoadMoreTongjiNews));

                            thread.Start(src.Count);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load more Tongji News and insert to UI
        /// </summary>
        /// <param name="param">Tongji News count in ListBox_TongjiNews</param>
        private void LoadMoreTongjiNews(Object param)
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
                isLoadingMoreTongjiNews = true;
            });

            var count = (int)param;
            SchoolNewsExt[] arr = null;

            using (var db = WTShareDataContext.ShareDB)
            {
                arr = db.SchoolNewsTable.ToArray();
            }

            if (arr != null && arr.Count() > count)
            {
                var src = arr.OrderByDescending((news) => news.CreatedAt).Skip(count);

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                });

                if (src.Count() > 10)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        InsertMoreTongjiNews(src.Take(10));
                        isLoadingMoreTongjiNews = false;
                    });
                }
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        InsertMoreTongjiNews(src);
                        isLoadingMoreTongjiNews = false;
                    });
                }
            }
            else
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                    isLoadingMoreTongjiNews = false;
                });
            }
        }

        private void InsertMoreTongjiNews(IEnumerable<SchoolNewsExt> arr)
        {
            if (arr == null || !this.NavigationService.CurrentSource.ToString().StartsWith("/Pages/CampusInfo.xaml"))
                return;

            var src = SchoolNewsSource;

            if (src == null)
            {
                SchoolNewsSource = new ObservableCollection<SchoolNewsExt>(arr.OrderByDescending((news) => news.CreatedAt));
            }
            else
            {
                foreach (var sn in arr)
                {
                    var count = src.Where((news) => news.Id == sn.Id).Count();

                    if (count == 0)
                    {
                        var idx = src.Where((news) => news.CreatedAt > sn.CreatedAt).Count();
                        src.Insert(idx, sn);
                    }
                }
            }
        }

        #endregion

        #region [Around News]

        private void ListBox_AroundNews_MouseMove(Object sender, MouseEventArgs e)
        {
            var lb = sender as ListBox;
            DependencyObject obj = lb;

            while (!(obj is ScrollViewer) && obj != null)
            {
                obj = VisualTreeHelper.GetChild(obj, 0);
            }

            if (obj != null)
            {
                var sv = obj as ScrollViewer;

                //...Scroll to the end
                if (!isLoadingMoreAroundNews && Math.Abs(sv.ScrollableHeight - sv.VerticalOffset) <= 0.1)
                {
                    var src = AroundNewsSource;
                    if (src != null && src.Count > 0)
                    {
                        if (!src.Last().IsInvalidAround)
                        {
                            var thread = new Thread(new ParameterizedThreadStart(LoadMoreAroundNews));

                            thread.Start(src.Count);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load more Around News and insert to UI
        /// </summary>
        /// <param name="param">Tongji News count in ListBox_AroundNews</param>
        private void LoadMoreAroundNews(Object param)
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
                isLoadingMoreAroundNews = true;
            });

            var count = (int)param;
            AroundExt[] arr = null;

            using (var db = WTShareDataContext.ShareDB)
            {
                arr = db.AroundTable.ToArray();
            }

            if (arr != null && arr.Count() > count)
            {
                var src = arr.OrderByDescending((news) => news.CreatedAt).Skip(count);

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                });

                if (src.Count() > 10)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        InsertMoreAroundNews(src.Take(10));
                        isLoadingMoreAroundNews = false;
                    });
                }
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        InsertMoreAroundNews(src);
                        isLoadingMoreAroundNews = false;
                    });
                }
            }
            else
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                    isLoadingMoreAroundNews = false;
                });
            }
        }

        private void InsertMoreAroundNews(IEnumerable<AroundExt> arr)
        {
            if (arr == null || !this.NavigationService.CurrentSource.ToString().StartsWith("/Pages/CampusInfo.xaml"))
                return;

            var src = AroundNewsSource;

            if (src == null)
            {
                AroundNewsSource = new ObservableCollection<AroundExt>(arr.OrderByDescending((news) => news.CreatedAt));
            }
            else
            {
                foreach (var sn in arr)
                {
                    var count = src.Where((news) => news.Id == sn.Id).Count();

                    if (count == 0)
                    {
                        var idx = src.Where((news) => news.CreatedAt > sn.CreatedAt).Count();
                        src.Insert(idx, sn);
                    }
                }
            }
        }

        #endregion

        #region [Official Notes]

        private void ListBox_OfficialNotes_MouseMove(Object sender, MouseEventArgs e)
        {
            var lb = sender as ListBox;
            DependencyObject obj = lb;

            while (!(obj is ScrollViewer) && obj != null)
            {
                obj = VisualTreeHelper.GetChild(obj, 0);
            }

            if (obj != null)
            {
                var sv = obj as ScrollViewer;

                //...Scroll to the end
                if (!isLoadingMoreOfficialNotes && Math.Abs(sv.ScrollableHeight - sv.VerticalOffset) <= 0.1)
                {
                    var src = OfficialNotesSource;
                    if (src != null && src.Count > 0)
                    {
                        if (!src.Last().IsInvalidForStaff)
                        {
                            var thread = new Thread(new ParameterizedThreadStart(LoadMoreOfficialNotes));

                            thread.Start(src.Count);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load more Tongji News and insert to UI
        /// </summary>
        /// <param name="param">Tongji News count in ListBox_TongjiNews</param>
        private void LoadMoreOfficialNotes(Object param)
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
                isLoadingMoreOfficialNotes = true;
            });

            var count = (int)param;
            ForStaffExt[] arr = null;

            using (var db = WTShareDataContext.ShareDB)
            {
                arr = db.ForStaffTable.ToArray();
            }

            if (arr != null && arr.Count() > count)
            {
                var src = arr.OrderByDescending((news) => news.CreatedAt).Skip(count);

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                });

                if (src.Count() > 10)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        InsertMoreOfficialNotes(src.Take(10));
                        isLoadingMoreOfficialNotes = false;
                    });
                }
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        InsertMoreOfficialNotes(src);
                        isLoadingMoreOfficialNotes = false;
                    });
                }
            }
            else
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                    isLoadingMoreOfficialNotes = false;
                });
            }
        }

        private void InsertMoreOfficialNotes(IEnumerable<ForStaffExt> arr)
        {
            if (arr == null || !this.NavigationService.CurrentSource.ToString().StartsWith("/Pages/CampusInfo.xaml"))
                return;

            var src = OfficialNotesSource;

            if (src == null)
            {
                OfficialNotesSource = new ObservableCollection<ForStaffExt>(arr.OrderByDescending((news) => news.CreatedAt));
            }
            else
            {
                foreach (var sn in arr)
                {
                    var count = src.Where((news) => news.Id == sn.Id).Count();

                    if (count == 0)
                    {
                        var idx = src.Where((news) => news.CreatedAt > sn.CreatedAt).Count();
                        src.Insert(idx, sn);
                    }
                }
            }
        }

        #endregion

        #region [Club News]

        private void ListBox_ClubNews_MouseMove(Object sender, MouseEventArgs e)
        {
            var lb = sender as ListBox;
            DependencyObject obj = lb;

            while (!(obj is ScrollViewer) && obj != null)
            {
                obj = VisualTreeHelper.GetChild(obj, 0);
            }

            if (obj != null)
            {
                var sv = obj as ScrollViewer;

                //...Scroll to the end
                if (!isLoadingMoreClubNews && Math.Abs(sv.ScrollableHeight - sv.VerticalOffset) <= 0.1)
                {
                    var src = ClubNewsSource;
                    if (src != null && src.Count > 0)
                    {
                        if (!src.Last().IsInvalidClubNews)
                        {
                            var thread = new Thread(new ParameterizedThreadStart(LoadMoreClubNews));

                            thread.Start(src.Count);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load more Tongji News and insert to UI
        /// </summary>
        /// <param name="param">Tongji News count in ListBox_TongjiNews</param>
        private void LoadMoreClubNews(Object param)
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
                isLoadingMoreClubNews = true;
            });

            var count = (int)param;
            ClubNewsExt[] arr = null;

            using (var db = WTShareDataContext.ShareDB)
            {
                arr = db.ClubNewsTable.ToArray();
            }

            if (arr != null && arr.Count() > count)
            {
                var src = arr.OrderByDescending((news) => news.CreatedAt).Skip(count);

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                });

                if (src.Count() > 10)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        InsertMoreClubNews(src.Take(10));
                        isLoadingMoreClubNews = false;
                    });
                }
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        InsertMoreClubNews(src);
                        isLoadingMoreClubNews = false;
                    });
                }
            }
            else
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                    isLoadingMoreClubNews = false;
                });
            }
        }

        private void InsertMoreClubNews(IEnumerable<ClubNewsExt> arr)
        {
            if (arr == null || !this.NavigationService.CurrentSource.ToString().StartsWith("/Pages/CampusInfo.xaml"))
                return;

            var src = ClubNewsSource;

            if (src == null)
            {
                ClubNewsSource = new ObservableCollection<ClubNewsExt>(arr.OrderByDescending((news) => news.CreatedAt));
            }
            else
            {
                foreach (var sn in arr)
                {
                    var count = src.Where((news) => news.Id == sn.Id).Count();

                    if (count == 0)
                    {
                        var idx = src.Where((news) => news.CreatedAt > sn.CreatedAt).Count();
                        src.Insert(idx, sn);
                    }
                }
            }
        }
        #endregion

        #endregion

        #region [Refresh]

        private void Refresh_Button_Clicked(Object sender, EventArgs e)
        {
            switch (Pivot_Core.SelectedIndex)
            {
                case 0:
                    {
                        //...Refresh display creation time
                        var src = SchoolNewsSource;

                        if (src != null)
                        {
                            foreach (var news in src)
                            {
                                news.SendPropertyChanged("DisplayCreationTime");
                            }
                        }

                        //...Get latest school news
                        GetLatestTongjiNews();
                    }
                    break;
                case 1:
                    {
                        //...Get latest around news
                        GetLatestAroundNews();
                    }
                    break;
                case 2:
                    {
                        //...Refresh display creation time
                        var src = OfficialNotesSource;

                        if (src != null)
                        {
                            foreach (var news in src)
                            {
                                news.SendPropertyChanged("DisplayCreationTime");
                            }
                        }

                        //...Get latest official notes
                        GetLatestOfficialNotes();
                    }
                    break;
                case 3:
                    {
                        //...Refresh display creation time
                        var src = ClubNewsSource;

                        if (src != null)
                        {
                            foreach (var news in src)
                            {
                                news.SendPropertyChanged("DisplayCreationTime");
                            }
                        }

                        //...Get latest club news
                        GetLatestClubNews();
                    }
                    break;
                default:
                    break;
            }
        }

        private void GetLatestTongjiNews(int page = 0)
        {
            if (!this.NavigationService.CurrentSource.ToString().StartsWith("/Pages/CampusInfo.xaml") || Pivot_Core.SelectedIndex != 0)
                return;

            var req = new SchoolNewsGetListRequest<SchoolNewsGetListResponse>();
            var client = new WTDefaultClient<SchoolNewsGetListResponse>();
            String uid = Global.Instance.CurrentUserID;

            if (page > 0)
                req.SetAdditionalParameter(WTDefaultClient<SchoolNewsGetListResponse>.PAGE, page);

            client.ExecuteCompleted += (obj, arg) =>
                {
                    if (Global.Instance.CurrentUserID != uid)
                        return;

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();
                    });

                    int newNews = 0;

                    foreach (var schoolnews in arg.Result.SchoolNews)
                    {
                        SchoolNewsExt target = null;

                        using (var db = WTShareDataContext.ShareDB)
                        {
                            target = db.SchoolNewsTable.Where((news) => news.Id == schoolnews.Id).SingleOrDefault();

                            if (target == null)
                            {
                                target = new SchoolNewsExt();
                                target.SetObject(schoolnews);

                                db.SchoolNewsTable.InsertOnSubmit(target);
                                ++newNews;
                            }
                            else
                            {
                                target.SetObject(schoolnews);
                            }

                            db.SubmitChanges();
                        }

                        if (target != null)
                        {
                            this.Dispatcher.BeginInvoke(() =>
                            {
                                InsertMoreTongjiNews(new SchoolNewsExt[] { target });
                            });
                        }
                    }

                    this.Dispatcher.BeginInvoke(() => 
                    {
                        if (newNews == 1)
                        {
                            WTToast.Instance.Show(StringLibrary.CampusInfo_ReceiveANewTongjiNews);
                        }
                        else if (newNews > 1)
                        {
                            WTToast.Instance.Show(String.Format(StringLibrary.CampusInfo_ReceiveNewTongjiNews, newNews));
                        }
                    });

                    GetLatestTongjiNews(arg.Result.NextPager);
                };

            client.ExecuteFailed += (obj, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();
                    });
                };

            ProgressBarPopup.Instance.Open();

            if (String.IsNullOrEmpty(uid))
                client.Execute(req);
            else
                client.Execute(req, Global.Instance.Session, uid);
        }

        private void GetLatestAroundNews(int page = 0)
        {
            if (!this.NavigationService.CurrentSource.ToString().StartsWith("/Pages/CampusInfo.xaml") || Pivot_Core.SelectedIndex != 1)
                return;

            var req = new AroundsGetRequest<AroundsGetResponse>();
            var client = new WTDefaultClient<AroundsGetResponse>();
            String uid = Global.Instance.CurrentUserID;

            if (page > 0)
                req.SetAdditionalParameter(WTDefaultClient<AroundsGetResponse>.PAGE, page);

            client.ExecuteCompleted += (obj, arg) =>
            {
                if (Global.Instance.CurrentUserID != uid)
                    return;

                List<AroundExt> insertList = new List<AroundExt>();

                int newNews = 0;

                foreach (var aroundnews in arg.Result.Arounds)
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        var target = db.AroundTable.Where((news) => news.Id == aroundnews.Id).SingleOrDefault();

                        if (target == null)
                        {
                            target = new AroundExt();
                            target.SetObject(aroundnews);

                            insertList.Add(target);
                            db.AroundTable.InsertOnSubmit(target);

                            ++newNews;
                        }
                        else
                        {
                            target.SetObject(aroundnews);
                        }

                        db.SubmitChanges();
                    }


                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();

                    if (newNews == 1)
                    {
                        WTToast.Instance.Show(StringLibrary.CampusInfo_ReceiveANewRecommend);
                    }
                    else if (newNews > 1)
                    {
                        WTToast.Instance.Show(String.Format(StringLibrary.CampusInfo_ReceiveANewRecommend, newNews));
                    }
                });

                if (insertList.Count > 0)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        InsertMoreAroundNews(insertList);

                        GetLatestAroundNews(arg.Result.NextPager);
                    });
                }
            };

            client.ExecuteFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                });
            };

            ProgressBarPopup.Instance.Open();

            if (String.IsNullOrEmpty(uid))
                client.Execute(req);
            else
                client.Execute(req, Global.Instance.Session, uid);
        }

        private void GetLatestOfficialNotes(int page = 0)
        {
            if (!this.NavigationService.CurrentSource.ToString().StartsWith("/Pages/CampusInfo.xaml") || Pivot_Core.SelectedIndex != 2)
                return;

            var req = new ForStaffsGetRequest<ForStaffsGetResponse>();
            var client = new WTDefaultClient<ForStaffsGetResponse>();
            String uid = Global.Instance.CurrentUserID;

            if (page > 0)
                req.SetAdditionalParameter(WTDefaultClient<ForStaffsGetResponse>.PAGE, page);

            client.ExecuteCompleted += (obj, arg) =>
            {
                if (Global.Instance.CurrentUserID != uid)
                    return;

                List<ForStaffExt> insertList = new List<ForStaffExt>();

                foreach (var officialnote in arg.Result.ForStaffs)
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        var target = db.ForStaffTable.Where((news) => news.Id == officialnote.Id).SingleOrDefault();

                        if (target == null)
                        {
                            target = new ForStaffExt();
                            target.SetObject(officialnote);

                            insertList.Add(target);
                            db.ForStaffTable.InsertOnSubmit(target);
                        }
                        else
                        {
                            target.SetObject(officialnote);
                        }

                        db.SubmitChanges();
                    }


                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();

                    if (insertList.Count == 1)
                    {
                        WTToast.Instance.Show(StringLibrary.CampusInfo_ReceiveANewOfficialNote);
                    }
                    else if (insertList.Count > 1)
                    {
                        WTToast.Instance.Show(String.Format(StringLibrary.CampusInfo_ReceiveNewOfficialNotes, insertList.Count));
                    }
                });

                if (insertList.Count > 0)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        InsertMoreOfficialNotes(insertList);

                        GetLatestOfficialNotes(arg.Result.NextPager);
                    });
                }
            };

            client.ExecuteFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                });
            };

            ProgressBarPopup.Instance.Open();

            if (String.IsNullOrEmpty(uid))
                client.Execute(req);
            else
                client.Execute(req, Global.Instance.Session, uid);
        }

        private void GetLatestClubNews(int page = 0)
        {
            if (!this.NavigationService.CurrentSource.ToString().StartsWith("/Pages/CampusInfo.xaml") || Pivot_Core.SelectedIndex != 3)
                return;

            var req = new ClubNewsGetListRequest<ClubNewsGetListResponse>();
            var client = new WTDefaultClient<ClubNewsGetListResponse>();
            String uid = Global.Instance.CurrentUserID;

            if (page > 0)
                req.SetAdditionalParameter(WTDefaultClient<ClubNewsGetListResponse>.PAGE, page);

            client.ExecuteCompleted += (obj, arg) =>
            {
                if (Global.Instance.CurrentUserID != uid)
                    return;

                List<ClubNewsExt> insertList = new List<ClubNewsExt>();

                foreach (var clubnews in arg.Result.ClubNews)
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        var target = db.ClubNewsTable.Where((news) => news.Id == clubnews.Id).SingleOrDefault();

                        if (target == null)
                        {
                            target = new ClubNewsExt();
                            target.SetObject(clubnews);

                            insertList.Add(target);
                            db.ClubNewsTable.InsertOnSubmit(target);
                        }
                        else
                        {
                            target.SetObject(clubnews);
                        }

                        db.SubmitChanges();
                    }

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();

                    if (insertList.Count == 1)
                    {
                        WTToast.Instance.Show(StringLibrary.CampusInfo_ReceiveANewRecommend);
                    }
                    else if (insertList.Count > 1)
                    {
                        WTToast.Instance.Show(String.Format(StringLibrary.CampusInfo_ReceiveNewRecommends, insertList.Count));
                    }
                });

                if (insertList.Count > 0)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        InsertMoreClubNews(insertList);

                        GetLatestClubNews(arg.Result.NextPager);
                    });
                }
            };

            client.ExecuteFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                });
            };

            ProgressBarPopup.Instance.Open();

            if (String.IsNullOrEmpty(uid))
                client.Execute(req);
            else
                client.Execute(req, Global.Instance.Session, uid);
        }

        private void RefreshTongjiNews(int page = 0)
        {
            if (!this.NavigationService.CurrentSource.ToString().StartsWith("/Pages/CampusInfo.xaml") || Pivot_Core.SelectedIndex != 0)
            {
                tongjiNewsSourceState = SourceState.NotSet;
                return;
            }

            var req = new SchoolNewsGetListRequest<SchoolNewsGetListResponse>();
            var client = new WTDefaultClient<SchoolNewsGetListResponse>();
            String uid = Global.Instance.CurrentUserID;

            if (page > 0)
                req.SetAdditionalParameter(WTDefaultClient<SchoolNewsGetListResponse>.PAGE, page);

            client.ExecuteCompleted += (obj, arg) =>
            {
                if (Global.Instance.CurrentUserID != uid)
                {
                    Global.Instance.TongjiNewsPageId = 0;

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        RefreshTongjiNews();
                    });
                    return;
                }

                int count = arg.Result.SchoolNews.Count();
                SchoolNewsExt[] arr = new SchoolNewsExt[count];

                //...flag points out whether we should send another request for the next page.
                bool flag = false;
                int newNews = 0;

                for (int i = 0; i < count; ++i)
                {
                    var schoolnews = arg.Result.SchoolNews[i];
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        var target = db.SchoolNewsTable.Where((news) => news.Id == schoolnews.Id).SingleOrDefault();

                        if (target == null)
                        {
                            target = new SchoolNewsExt();
                            target.SetObject(schoolnews);

                            db.SchoolNewsTable.InsertOnSubmit(target);

                            ++newNews;
                        }
                        else
                        {
                            target.SetObject(schoolnews);

                            flag = true;
                        }
                        arr[i] = target;
                        db.SubmitChanges();
                    }
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();

                    if (newNews == 1)
                    {
                        WTToast.Instance.Show(StringLibrary.CampusInfo_ReceiveANewTongjiNews);
                    }
                    else if (newNews > 1)
                    {
                        WTToast.Instance.Show(String.Format(StringLibrary.CampusInfo_ReceiveNewTongjiNews, newNews));
                    }
                });

                Global.Instance.TongjiNewsPageId = arg.Result.NextPager;

                //...Continue sending request
                if (flag)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        //...Remove add more button
                        if (SchoolNewsSource != null && SchoolNewsSource.Last().IsInvalidSchoolNews)
                        {
                            SchoolNewsSource.RemoveAt(SchoolNewsSource.Count - 1);
                        }

                        InsertMoreTongjiNews(arr);
                        if (arg.Result.NextPager > 0)
                        {
                            RefreshTongjiNews(arg.Result.NextPager);
                        }
                    });
                }
                //...Stop sending request
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        //...Remove add more button
                        if (SchoolNewsSource != null && SchoolNewsSource.Last().IsInvalidSchoolNews)
                        {
                            SchoolNewsSource.RemoveAt(SchoolNewsSource.Count - 1);
                        }
                        tongjiNewsSourceState = SourceState.Done;
                        InsertMoreTongjiNews(arr);
                        if (arg.Result.NextPager > 0)
                        {
                            SchoolNewsSource.Add(SchoolNewsExt.InvalidSchoolNews());
                        }
                    });
                }

            };

            client.ExecuteFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                    tongjiNewsSourceState = SourceState.NotSet;
                });
            };

            ProgressBarPopup.Instance.Open();
            tongjiNewsSourceState = SourceState.Setting;

            if (String.IsNullOrEmpty(uid))
                client.Execute(req);
            else
                client.Execute(req, Global.Instance.Session, uid);
        }

        private void RefreshAroundNews(int page = 0)
        {
            if (!this.NavigationService.CurrentSource.ToString().StartsWith("/Pages/CampusInfo.xaml") || Pivot_Core.SelectedIndex != 1)
            {
                aroundNewsSourceState = SourceState.NotSet;
                return;
            }

            var req = new AroundsGetRequest<AroundsGetResponse>();
            var client = new WTDefaultClient<AroundsGetResponse>();
            String uid = Global.Instance.CurrentUserID;

            if (page > 0)
                req.SetAdditionalParameter(WTDefaultClient<AroundsGetResponse>.PAGE, page);

            client.ExecuteCompleted += (obj, arg) =>
            {
                if (Global.Instance.CurrentUserID != uid)
                {
                    Global.Instance.AroundNewsPageId = 0;

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        RefreshAroundNews();
                    });
                    return;
                }

                int count = arg.Result.Arounds.Count();
                AroundExt[] arr = new AroundExt[count];
                int newNews = 0;

                //...flag points out whether we should send another request for the next page.
                bool flag = false;

                for (int i = 0; i < count; ++i)
                {
                    var aroundnews = arg.Result.Arounds[i];
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        var target = db.AroundTable.Where((news) => news.Id == aroundnews.Id).SingleOrDefault();

                        if (target == null)
                        {
                            target = new AroundExt();
                            target.SetObject(aroundnews);

                            db.AroundTable.InsertOnSubmit(target);

                            ++newNews;
                        }
                        else
                        {
                            target.SetObject(aroundnews);

                            flag = true;
                        }
                        arr[i] = target;
                        db.SubmitChanges();
                    }
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();

                    if (newNews == 1)
                    {
                        WTToast.Instance.Show(StringLibrary.CampusInfo_ReceiveANewRecommend);
                    }
                    else if (newNews > 1)
                    {
                        WTToast.Instance.Show(String.Format(StringLibrary.CampusInfo_ReceiveNewRecommends, newNews));
                    }
                });

                Global.Instance.AroundNewsPageId = arg.Result.NextPager;

                //...Continue sending request
                if (flag)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        //...Remove add more button
                        if (AroundNewsSource != null && AroundNewsSource.Last().IsInvalidAround)
                        {
                            AroundNewsSource.RemoveAt(AroundNewsSource.Count - 1);
                        }

                        InsertMoreAroundNews(arr);
                        if (arg.Result.NextPager > 0)
                        {
                            RefreshAroundNews(arg.Result.NextPager);
                        }
                    });
                }
                //...Stop sending request
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        //...Remove add more button
                        if (AroundNewsSource != null && AroundNewsSource.Last().IsInvalidAround)
                        {
                            AroundNewsSource.RemoveAt(AroundNewsSource.Count - 1);
                        }
                        tongjiNewsSourceState = SourceState.Done;
                        InsertMoreAroundNews(arr);
                        if (arg.Result.NextPager > 0)
                        {
                            AroundNewsSource.Add(AroundExt.InvalidAround());
                        }
                    });
                }

            };

            client.ExecuteFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                    aroundNewsSourceState = SourceState.NotSet;
                });
            };

            ProgressBarPopup.Instance.Open();
            aroundNewsSourceState = SourceState.Setting;

            if (String.IsNullOrEmpty(uid))
                client.Execute(req);
            else
                client.Execute(req, Global.Instance.Session, uid);
        }

        private void RefreshOfficialNotes(int page = 0)
        {
            if (!this.NavigationService.CurrentSource.ToString().StartsWith("/Pages/CampusInfo.xaml") || Pivot_Core.SelectedIndex != 2)
            {
                officialNotesSourceState = SourceState.NotSet;
                return;
            }

            var req = new ForStaffsGetRequest<ForStaffsGetResponse>();
            var client = new WTDefaultClient<ForStaffsGetResponse>();
            String uid = Global.Instance.CurrentUserID;

            if (page > 0)
                req.SetAdditionalParameter(WTDefaultClient<ForStaffsGetResponse>.PAGE, page);

            client.ExecuteCompleted += (obj, arg) =>
            {
                if (Global.Instance.CurrentUserID != uid)
                {
                    Global.Instance.OfficialNotePageId = 0;

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        RefreshOfficialNotes();
                    });
                    return;
                }

                int count = arg.Result.ForStaffs.Count();
                ForStaffExt[] arr = new ForStaffExt[count];
                int newNews = 0;

                //...flag points out whether we should send another request for the next page.
                bool flag = false;

                for (int i = 0; i < count; ++i)
                {
                    var officialnote = arg.Result.ForStaffs[i];
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        var target = db.ForStaffTable.Where((news) => news.Id == officialnote.Id).SingleOrDefault();

                        if (target == null)
                        {
                            target = new ForStaffExt();
                            target.SetObject(officialnote);

                            db.ForStaffTable.InsertOnSubmit(target);

                            ++newNews;
                        }
                        else
                        {
                            target.SetObject(officialnote);

                            flag = true;
                        }
                        arr[i] = target;
                        db.SubmitChanges();
                    }
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();

                    if (newNews == 1)
                    {
                        WTToast.Instance.Show(StringLibrary.CampusInfo_ReceiveANewOfficialNote);
                    }
                    else if (newNews > 1)
                    {
                        WTToast.Instance.Show(String.Format(StringLibrary.CampusInfo_ReceiveNewOfficialNotes, newNews));
                    }
                });

                Global.Instance.OfficialNotePageId = arg.Result.NextPager;

                //...Continue sending request
                if (flag)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        //...Remove add more button
                        if (OfficialNotesSource != null && OfficialNotesSource.Last().IsInvalidForStaff)
                        {
                            OfficialNotesSource.RemoveAt(OfficialNotesSource.Count - 1);
                        }

                        InsertMoreOfficialNotes(arr);
                        if (arg.Result.NextPager > 0)
                        {
                            RefreshOfficialNotes(arg.Result.NextPager);
                        }
                    });
                }
                //...Stop sending request
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        //...Remove add more button
                        if (OfficialNotesSource != null && OfficialNotesSource.Last().IsInvalidForStaff)
                        {
                            OfficialNotesSource.RemoveAt(OfficialNotesSource.Count - 1);
                        }
                        officialNotesSourceState = SourceState.Done;
                        InsertMoreOfficialNotes(arr);
                        if (arg.Result.NextPager > 0)
                        {
                            OfficialNotesSource.Add(ForStaffExt.InvalidForStaff());
                        }
                    });
                }

            };

            client.ExecuteFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                    officialNotesSourceState = SourceState.NotSet;
                });
            };

            ProgressBarPopup.Instance.Open();
            officialNotesSourceState = SourceState.Setting;

            if (String.IsNullOrEmpty(uid))
                client.Execute(req);
            else
                client.Execute(req, Global.Instance.Session, uid);
        }

        private void RefreshClubNews(int page = 0)
        {
            if (!this.NavigationService.CurrentSource.ToString().StartsWith("/Pages/CampusInfo.xaml") || Pivot_Core.SelectedIndex != 3)
            {
                clubNewsSourceState = SourceState.NotSet;
                return;
            }

            var req = new ClubNewsGetListRequest<ClubNewsGetListResponse>();
            var client = new WTDefaultClient<ClubNewsGetListResponse>();
            String uid = Global.Instance.CurrentUserID;

            if (page > 0)
                req.SetAdditionalParameter(WTDefaultClient<ClubNewsGetListResponse>.PAGE, page);

            client.ExecuteCompleted += (obj, arg) =>
            {
                if (Global.Instance.CurrentUserID != uid)
                {
                    Global.Instance.OfficialNotePageId = 0;

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        RefreshClubNews();
                    });
                    return;
                }

                int count = arg.Result.ClubNews.Count();
                ClubNewsExt[] arr = new ClubNewsExt[count];
                int newNews = 0;

                //...flag points out whether we should send another request for the next page.
                bool flag = false;

                for (int i = 0; i < count; ++i)
                {
                    var clubnews = arg.Result.ClubNews[i];
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        var target = db.ClubNewsTable.Where((news) => news.Id == clubnews.Id).SingleOrDefault();

                        if (target == null)
                        {
                            target = new ClubNewsExt();
                            target.SetObject(clubnews);

                            db.ClubNewsTable.InsertOnSubmit(target);

                            ++newNews;
                        }
                        else
                        {
                            target.SetObject(clubnews);

                            flag = true;
                        }
                        arr[i] = target;
                        db.SubmitChanges();
                    }
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();

                    if (newNews == 1)
                    {
                        WTToast.Instance.Show(StringLibrary.CampusInfo_ReceiveNewGroupNotices);
                    }
                    else if (newNews > 1)
                    {
                        WTToast.Instance.Show(String.Format(StringLibrary.CampusInfo_ReceiveANewGroupNotice, newNews));
                    }
                });

                Global.Instance.ClubNewsPageId = arg.Result.NextPager;

                //...Continue sending request
                if (flag)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        //...Remove add more button
                        if (ClubNewsSource != null && ClubNewsSource.Last().IsInvalidClubNews)
                        {
                            ClubNewsSource.RemoveAt(ClubNewsSource.Count - 1);
                        }

                        InsertMoreClubNews(arr);
                        if (arg.Result.NextPager > 0)
                        {
                            RefreshClubNews(arg.Result.NextPager);
                        }
                    });
                }
                //...Stop sending request
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        //...Remove add more button
                        if (ClubNewsSource != null && ClubNewsSource.Last().IsInvalidClubNews)
                        {
                            ClubNewsSource.RemoveAt(ClubNewsSource.Count - 1);
                        }
                        clubNewsSourceState = SourceState.Done;
                        InsertMoreClubNews(arr);
                        if (arg.Result.NextPager > 0)
                        {
                            ClubNewsSource.Add(ClubNewsExt.InvalidClubNews());
                        }
                    });
                }

            };

            client.ExecuteFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                    clubNewsSourceState = SourceState.NotSet;
                });
            };

            ProgressBarPopup.Instance.Open();
            clubNewsSourceState = SourceState.Setting;

            if (String.IsNullOrEmpty(uid))
                client.Execute(req);
            else
                client.Execute(req, Global.Instance.Session, uid);
        }

        #endregion

        #region [Load More]

        private void Button_LoadMoreTongjiNews_Click(Object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            btn.IsHitTestVisible = false;
            btn.Content = StringLibrary.CampusInfo_LoadingMoreTongjiNews;

            String uid = Global.Instance.CurrentUserID;
            var req = new SchoolNewsGetListRequest<SchoolNewsGetListResponse>();
            var client = new WTDefaultClient<SchoolNewsGetListResponse>();

            if (Global.Instance.TongjiNewsPageId > 0)
                req.SetAdditionalParameter(WTDefaultClient<SchoolNewsGetListResponse>.PAGE, Global.Instance.TongjiNewsPageId);

            client.ExecuteCompleted += (obj, arg) =>
            {
                //...Return if current user info has been changed.
                if (uid != Global.Instance.CurrentUserID)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();
                        btn.Content = StringLibrary.CampusInfo_LoadMoreTongjiNews;
                        btn.IsHitTestVisible = true;
                    });
                    return;
                }

                int count = arg.Result.SchoolNews.Count();

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();

                    var src = SchoolNewsSource;

                    if (src != null && src.Last().IsInvalidSchoolNews)
                    {
                        src.RemoveAt(src.Count - 1);
                    }
                });

                for (int i = 0; i < count; ++i)
                {
                    var schoolnews = arg.Result.SchoolNews[i];
                    SchoolNewsExt target = null;

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        target = db.SchoolNewsTable.Where((news) => news.Id == schoolnews.Id).SingleOrDefault();

                        if (target == null)
                        {
                            target = new SchoolNewsExt();
                            target.SetObject(schoolnews);
                            db.SchoolNewsTable.InsertOnSubmit(target);
                        }
                        else
                        {
                            target.SetObject(schoolnews);
                        }

                        db.SubmitChanges();
                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        if (target != null)
                            InsertMoreTongjiNews(new SchoolNewsExt[] { target });

                        if (i == count - 1 && arg.Result.NextPager > 0 && SchoolNewsSource != null)
                        {
                            SchoolNewsSource.Add(SchoolNewsExt.InvalidSchoolNews());
                        }
                    });
                }

                Global.Instance.TongjiNewsPageId = arg.Result.NextPager;

                this.Dispatcher.BeginInvoke(() =>
                {
                    tongjiNewsSourceState = SourceState.Done;
                });
            };

            client.ExecuteFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();

                    tongjiNewsSourceState = SourceState.Done;
                    btn.Content = StringLibrary.CampusInfo_LoadMoreTongjiNews;
                    btn.IsHitTestVisible = true;
                });
            };

            ProgressBarPopup.Instance.Open();
            tongjiNewsSourceState = SourceState.Setting;
            if (String.IsNullOrEmpty(uid))
                client.Execute(req);
            else
                client.Execute(req, Global.Instance.Session, uid);
        }

        private void Button_LoadMoreAroundNews_Click(Object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            btn.IsHitTestVisible = false;
            btn.Content = StringLibrary.CampusInfo_LoadingMoreAroundNews;

            String uid = Global.Instance.CurrentUserID;
            var req = new AroundsGetRequest<AroundsGetResponse>();
            var client = new WTDefaultClient<AroundsGetResponse>();

            if (Global.Instance.AroundNewsPageId > 0)
                req.SetAdditionalParameter(WTDefaultClient<AroundsGetResponse>.PAGE, Global.Instance.AroundNewsPageId);

            client.ExecuteCompleted += (obj, arg) =>
            {
                //...Return if current user info has been changed.
                if (uid != Global.Instance.CurrentUserID)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();
                        btn.Content = StringLibrary.CampusInfo_LoadMoreAroundNews;
                        btn.IsHitTestVisible = true;
                    });
                    return;
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();

                    var src = AroundNewsSource;

                    if (src != null && src.Last().IsInvalidAround)
                    {
                        src.RemoveAt(src.Count - 1);
                    }
                });

                int count = arg.Result.Arounds.Count();

                for (int i = 0; i < count; ++i)
                {
                    var aroundnews = arg.Result.Arounds[i];
                    AroundExt target = null;

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        target = db.AroundTable.Where((news) => news.Id == aroundnews.Id).SingleOrDefault();

                        if (target == null)
                        {
                            target = new AroundExt();
                            target.SetObject(aroundnews);
                            db.AroundTable.InsertOnSubmit(target);
                        }
                        else
                        {
                            target.SetObject(aroundnews);
                        }

                        db.SubmitChanges();
                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        if (target != null)
                        {
                            InsertMoreAroundNews(new AroundExt[] { target });
                        }

                        if (i == count - 1 && arg.Result.NextPager > 0 && AroundNewsSource != null)
                        {
                            AroundNewsSource.Add(AroundExt.InvalidAround());
                        }
                    });
                }

                Global.Instance.AroundNewsPageId = arg.Result.NextPager;

                this.Dispatcher.BeginInvoke(() =>
                {
                    aroundNewsSourceState = SourceState.Done;
                });
            };

            client.ExecuteFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();

                    aroundNewsSourceState = SourceState.Done;
                    btn.Content = StringLibrary.CampusInfo_LoadMoreAroundNews;
                    btn.IsHitTestVisible = true;
                });
            };

            ProgressBarPopup.Instance.Open();
            aroundNewsSourceState = SourceState.Setting;
            if (String.IsNullOrEmpty(uid))
                client.Execute(req);
            else
                client.Execute(req, Global.Instance.Session, uid);
        }

        private void Button_LoadMoreOfficialNotes_Click(Object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            btn.IsHitTestVisible = false;
            btn.Content = StringLibrary.CampusInfo_LoadingMoreOfficialNotes;

            String uid = Global.Instance.CurrentUserID;
            var req = new ForStaffsGetRequest<ForStaffsGetResponse>();
            var client = new WTDefaultClient<ForStaffsGetResponse>();

            if (Global.Instance.OfficialNotePageId > 0)
                req.SetAdditionalParameter(WTDefaultClient<ForStaffsGetResponse>.PAGE, Global.Instance.OfficialNotePageId);

            client.ExecuteCompleted += (obj, arg) =>
                {
                    //...Return if current user info has been changed.
                    if (uid != Global.Instance.CurrentUserID)
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            ProgressBarPopup.Instance.Close();
                            btn.Content = StringLibrary.CampusInfo_LoadMoreOfficialNotes;
                            btn.IsHitTestVisible = true;
                        });
                        return;
                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();
                        officialNotesSourceState = SourceState.Done;

                        var src = OfficialNotesSource;

                        if (src != null && src.Last().IsInvalidForStaff)
                        {
                            src.RemoveAt(src.Count - 1);
                        }
                    });

                    int count = arg.Result.ForStaffs.Count();

                    for (int i = 0; i < count; ++i)
                    {
                        var officialnote = arg.Result.ForStaffs[i];
                        ForStaffExt target = null;

                        using (var db = WTShareDataContext.ShareDB)
                        {
                            target = db.ForStaffTable.Where((news) => news.Id == officialnote.Id).SingleOrDefault();

                            if (target == null)
                            {
                                target = new ForStaffExt();
                                target.SetObject(officialnote);
                                db.ForStaffTable.InsertOnSubmit(target);
                            }
                            else
                            {
                                target.SetObject(officialnote);
                            }

                            db.SubmitChanges();
                        }

                        this.Dispatcher.BeginInvoke(() =>
                        {
                            if (target != null)
                                InsertMoreOfficialNotes(new ForStaffExt[] { target });

                            if (i == count - 1 && arg.Result.NextPager > 0 && OfficialNotesSource != null)
                            {
                                OfficialNotesSource.Add(ForStaffExt.InvalidForStaff());
                            }
                        });
                    }

                    Global.Instance.OfficialNotePageId = arg.Result.NextPager;
                };

            client.ExecuteFailed += (obj, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();

                        officialNotesSourceState = SourceState.Done;
                        btn.Content = StringLibrary.CampusInfo_LoadMoreOfficialNotes;
                        btn.IsHitTestVisible = true;
                    });
                };

            ProgressBarPopup.Instance.Open();
            officialNotesSourceState = SourceState.Setting;
            if (String.IsNullOrEmpty(uid))
                client.Execute(req);
            else
                client.Execute(req, Global.Instance.Session, uid);
        }

        private void Button_LoadMoreClubNews_Click(Object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            btn.IsHitTestVisible = false;
            btn.Content = StringLibrary.CampusInfo_LoadingMoreClubNews;

            String uid = Global.Instance.CurrentUserID;
            var req = new ClubNewsGetListRequest<ClubNewsGetListResponse>();
            var client = new WTDefaultClient<ClubNewsGetListResponse>();

            if (Global.Instance.ClubNewsPageId > 0)
                req.SetAdditionalParameter(WTDefaultClient<ClubNewsGetListResponse>.PAGE, Global.Instance.ClubNewsPageId);

            client.ExecuteCompleted += (obj, arg) =>
            {
                //...Return if current user info has been changed.
                if (uid != Global.Instance.CurrentUserID)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();
                        btn.Content = StringLibrary.CampusInfo_LoadMoreClubNews;
                        btn.IsHitTestVisible = true;
                    });
                    return;
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                    clubNewsSourceState = SourceState.Done;

                    var src = ClubNewsSource;

                    if (src != null && src.Last().IsInvalidClubNews)
                    {
                        src.RemoveAt(src.Count - 1);
                    }
                });

                int count = arg.Result.ClubNews.Count();

                for (int i = 0; i < count; ++i)
                {
                    var clubnews = arg.Result.ClubNews[i];
                    ClubNewsExt target = null;

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        target = db.ClubNewsTable.Where((news) => news.Id == clubnews.Id).SingleOrDefault();

                        if (target == null)
                        {
                            target = new ClubNewsExt();
                            target.SetObject(clubnews);
                            db.ClubNewsTable.InsertOnSubmit(target);
                        }
                        else
                        {
                            target.SetObject(clubnews);
                        }

                        db.SubmitChanges();
                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        if (target != null)
                            InsertMoreClubNews(new ClubNewsExt[] { target });

                        if (i == count - 1 && arg.Result.NextPager > 0 && ClubNewsSource != null)
                        {
                            ClubNewsSource.Add(ClubNewsExt.InvalidClubNews());
                        }
                    });
                }

                Global.Instance.ClubNewsPageId = arg.Result.NextPager;
            };

            client.ExecuteFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();

                    clubNewsSourceState = SourceState.Done;
                    btn.Content = StringLibrary.CampusInfo_LoadMoreClubNews;
                    btn.IsHitTestVisible = true;
                });
            };

            ProgressBarPopup.Instance.Open();
            clubNewsSourceState = SourceState.Setting;
            if (String.IsNullOrEmpty(uid))
                client.Execute(req);
            else
                client.Execute(req, Global.Instance.Session, uid);
        }

        #endregion

        private void Pivot_Core_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            var pivot = sender as Pivot;

            if (pivot.SelectedIndex == 0)
            {
                if (tongjiNewsSourceState == SourceState.NotSet && Global.Instance.TongjiNewsPageId < 0)
                {
                    RefreshTongjiNews(Global.Instance.TongjiNewsPageId);
                }
            }
            else if (pivot.SelectedIndex == 1)
            {
                if (aroundNewsSourceState == SourceState.NotSet && Global.Instance.AroundNewsPageId < 0)
                {
                    RefreshAroundNews(Global.Instance.AroundNewsPageId);
                }
            }
            else if (pivot.SelectedIndex == 2)
            {
                if (officialNotesSourceState == SourceState.NotSet && Global.Instance.OfficialNotePageId < 0)
                {
                    RefreshOfficialNotes(Global.Instance.OfficialNotePageId);
                }
            }
            else if (pivot.SelectedIndex == 3)
            {
                if (clubNewsSourceState == SourceState.NotSet && Global.Instance.ClubNewsPageId < 0)
                {
                    RefreshClubNews(Global.Instance.ClubNewsPageId);
                }
            }
        }
    }
}