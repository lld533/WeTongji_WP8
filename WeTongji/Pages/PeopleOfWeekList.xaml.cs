using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WeTongji.DataBase;
using WeTongji.Api.Domain;
using System.Collections.ObjectModel;
using WeTongji.Business;
using WeTongji.Api;
using System.Diagnostics;
using WeTongji.Utility;
using WeTongji.Pages;
using WeTongji.Api.Request;
using WeTongji.Api.Response;
using System.Threading;

namespace WeTongji
{
    public partial class PeopleOfWeekList : PhoneApplicationPage
    {
        public PeopleOfWeekList()
        {
            InitializeComponent();

            var button = new ApplicationBarIconButton(new Uri("/icons/appbar.refresh.rest.png", UriKind.RelativeOrAbsolute))
            {
                Text = StringLibrary.PeopleOfWeekList_AppBarRefreshText
            };
            button.Click += Refresh_Button_Clicked;
            this.ApplicationBar.Buttons.Add(button);
        }

        private ObservableCollection<PersonExt> PeopleSource
        {
            get
            {
                if (ListBox_Core.ItemsSource == null)
                    return null;
                return ListBox_Core.ItemsSource as ObservableCollection<PersonExt>;
            }
            set
            {
                if (value == null || value.Count == 0)
                {
                    TextBlock_NoSource.Visibility = Visibility.Visible;
                }
                else
                {
                    ListBox_Core.ItemsSource = value;
                    TextBlock_NoSource.Visibility = Visibility.Collapsed;
                    ListBox_Core.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Load PeopleOfWeek Table from database when navigate to this page for the first time.
        /// Otherwise, check for not loaded person and reload data if it needs.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                var thread = new Thread(new ThreadStart(LoadData));
                thread.Start();
            }

            DownloadUnStoredAvatars();
        }

        private void LoadData()
        {
            PersonExt[] dbSource = null;
            using (var db = WTShareDataContext.ShareDB)
            {
                dbSource = db.People.ToArray();
            }

            var source = new ObservableCollection<PersonExt>(dbSource.OrderByDescending((person) => person.Id));

            this.Dispatcher.BeginInvoke(() =>
            {
                PeopleSource = source;
                ProgressBarPopup.Instance.Close();
                DownloadUnStoredAvatars();

                if (Global.Instance.CurrentPeopleOfWeekSourceState != SourceState.Done)
                {
                    GetAllPeopleFromServer(Global.Instance.PersonPageId);
                }
            });
        }

        private void DownloadUnStoredAvatars()
        {
            var people = PeopleSource;
            if (people != null)
            {
                int count = people.Count();
                for (int i = 0; i < count; ++i)
                {
                    var p = people[i];

                    if (!p.Avatar.EndsWith("missing.png") && !p.AvatarExists())
                    {
                        var client = new WTDownloadImageClient();

                        client.DownloadImageCompleted += (obj, arg) =>
                        {
                            this.Dispatcher.BeginInvoke(() =>
                            {
                                p.SendPropertyChanged("AvatarImageBrush");
                            });
                        };

                        client.Execute(p.Avatar, p.AvatarGuid + "." + p.Avatar.GetImageFileExtension());
                    }
                }
            }
        }

        private void ListBox_Core_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            var lb = sender as ListBox;

            if (lb.SelectedIndex == -1)
                return;

            var p = lb.SelectedItem as PersonExt;
            lb.SelectedIndex = -1;
            if (p != null)
                this.NavigationService.Navigate(new Uri(String.Format("/Pages/PeopleOfWeek.xaml?q={0}", p.Id), UriKind.RelativeOrAbsolute));
        }

        private void GetAllPeopleFromServer(int pageId = 0)
        {
            if (this.NavigationService.CurrentSource.ToString() != "/Pages/PeopleOfWeekList.xaml")
                return;

            var req = new PeopleGetRequest<PeopleGetResponse>();
            var client = new WTDefaultClient<PeopleGetResponse>();
            var uid = Global.Instance.CurrentUserID;

            if (pageId > 0)
                req.SetAdditionalParameter(WTDefaultClient<PeopleGetResponse>.PAGE, pageId);


            client.ExecuteCompleted += (obj, arg) =>
                {
                    if (Global.Instance.CurrentUserID != uid)
                    {
                        Global.Instance.CurrentPeopleOfWeekSourceState = SourceState.NotSet;
                        Global.Instance.PersonPageId = 0;
                        return;
                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();
                    });

                    int count = arg.Result.People.Count();
                    int newPersons = 0;

                    for (int i = 0; i < count; ++i)
                    {
                        PersonExt target = null;
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            var item = arg.Result.People[i];
                            target = db.People.Where((person) => person.Id == item.Id).SingleOrDefault();

                            if (target == null)
                            {
                                target = new PersonExt();
                                target.SetObject(item);

                                db.People.InsertOnSubmit(target);

                                ++newPersons;
                            }
                            else
                            {
                                target.SetObject(item);
                            }

                            db.SubmitChanges();
                        }

                        this.Dispatcher.BeginInvoke(() =>
                        {
                            InsertMorePeople(new PersonExt[] { target });
                        });
                    }

                    Global.Instance.PersonPageId = arg.Result.NextPager;

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        if (newPersons == 1)
                        {
                            WTToast.Instance.Show(StringLibrary.PeopleOfWeekList_ReceiveANewPersonTemplate);
                        }
                        else if (newPersons > 1)
                        {
                            WTToast.Instance.Show(String.Format(StringLibrary.PeopleOfWeekList_ReceiveNewPersonsTemplate, newPersons));
                        }


                        if (arg.Result.NextPager > 0)
                        {
                            GetAllPeopleFromServer(arg.Result.NextPager);
                        }
                        else
                        {
                            Global.Instance.CurrentPeopleOfWeekSourceState = SourceState.Done;
                        }
                    });
                };

            client.ExecuteFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                    Global.Instance.CurrentPeopleOfWeekSourceState = SourceState.NotSet;
                });
            };

            Global.Instance.CurrentPeopleOfWeekSourceState = SourceState.Setting;
            ProgressBarPopup.Instance.Open();
            if (String.IsNullOrEmpty(uid))
                client.Execute(req);
            else
                client.Execute(req, Global.Instance.Session, uid);
        }

        private void InsertMorePeople(IEnumerable<PersonExt> people)
        {
            if (this.NavigationService.CurrentSource.ToString() != "/Pages/PeopleOfWeekList.xaml")
                return;

            var src = PeopleSource;

            if (src == null)
            {
                PeopleSource = new ObservableCollection<PersonExt>(people.OrderByDescending((person) => person.Id));
                DownloadUnStoredAvatars();
            }
            else
            {
                foreach (var p in people)
                {
                    var target = src.Where((person) => person.Id == p.Id).SingleOrDefault();

                    if (target == null)
                    {
                        var idx = src.Where((person) => person.Id > p.Id).Count();
                        src.Insert(idx, p);

                        if (!p.Avatar.EndsWith("missing.png") && !p.AvatarExists())
                        {
                            var client = new WTDownloadImageClient();
                            client.DownloadImageCompleted += (obj, arg) =>
                                {
                                    this.Dispatcher.BeginInvoke(() =>
                                    {
                                        p.SendPropertyChanged("AvatarImageBrush");
                                    });
                                };

                            client.Execute(p.Avatar, p.AvatarGuid + "." + p.Avatar.GetImageFileExtension());
                        }
                    }
                    else
                    {
                        if (p.Name != target.Name)
                        {
                            target.Name = p.Name;
                            target.SendPropertyChanged("Name");
                        }
                        if (p.JobTitle != target.JobTitle)
                        {
                            target.JobTitle = p.JobTitle;
                            target.SendPropertyChanged("JobTitle");
                        }
                        if (p.NO != target.NO)
                        {
                            target.NO = p.NO;
                            target.SendPropertyChanged("NO");
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Try to get the latest Person from the server.
        /// </summary>
        private void RefreshPeopleOfWeek(int pageId = 0)
        {
            PeopleGetRequest<PeopleGetResponse> req = new PeopleGetRequest<PeopleGetResponse>();
            WTDefaultClient<PeopleGetResponse> client = new WTDefaultClient<PeopleGetResponse>();

            if (pageId > 0)
                req.SetAdditionalParameter(WTDefaultClient<PeopleGetResponse>.PAGE, pageId);

            #region [Add handlers]

            client.ExecuteCompleted += (o, arg) =>
            {
                int count = arg.Result.People.Count();
                PersonExt[] arr = new PersonExt[count];

                //...flag points out whether we should go on sending request
                bool flag = true;
                int newPersons = 0;

                #region [Update database]

                for (int i = 0; i < count; ++i)
                {
                    var item = arg.Result.People[i];
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        var itemInDB = db.People.Where((a) => a.Id == item.Id).FirstOrDefault();

                        //...New data
                        if (itemInDB == null)
                        {
                            itemInDB = new PersonExt();
                            itemInDB.SetObject(item);

                            db.People.InsertOnSubmit(itemInDB);

                            ++newPersons;
                        }
                        //...Already in DB
                        else
                        {
                            itemInDB.SetObject(item);
                            flag = false;
                        }

                        arr[i] = itemInDB;

                        db.SubmitChanges();
                    }
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                    InsertMorePeople(arr);

                    if (newPersons == 1)
                    {
                        WTToast.Instance.Show(StringLibrary.PeopleOfWeekList_ReceiveANewPersonTemplate);
                    }
                    else if (newPersons > 1)
                    {
                        WTToast.Instance.Show(String.Format(StringLibrary.PeopleOfWeekList_ReceiveNewPersonsTemplate, newPersons));
                    }

                    if (flag && arg.Result.NextPager > 0)
                        RefreshPeopleOfWeek(arg.Result.NextPager);
                });

                #endregion

                Global.Instance.PersonPageId = arg.Result.NextPager;
            };

            client.ExecuteFailed += (o, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                });
            };

            #endregion

            ProgressBarPopup.Instance.Open();
            client.Execute(req);
        }

        private void Refresh_Button_Clicked(Object sender, EventArgs e)
        {
            if (PeopleSource == null || PeopleSource.Count == 0)
            {
                RefreshPeopleOfWeek();
            }
            else
            {
                foreach (var p in PeopleSource)
                {
                    p.SendPropertyChanged("AvatarImageBrush");
                }

                DownloadUnStoredAvatars();
                RefreshPeopleOfWeek();
            }
        }
    }
}