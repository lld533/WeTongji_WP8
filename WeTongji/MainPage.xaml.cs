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
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.Phone.Tasks;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls.Primitives;
using Microsoft.Phone.Shell;
using System.Windows.Navigation;
using WeTongji.DataBase;
using System.IO.IsolatedStorage;
using WeTongji.Api.Domain;
using System.Collections.ObjectModel;
using WeTongji.Business;
using WeTongji.Api.Request;
using WeTongji.Api.Response;
using WeTongji.Api;
using WeTongji.Pages;
using System.Threading;
using WeTongji.Utility;
using System.Windows.Threading;
using Microsoft.Devices;
using System.Device.Location;


namespace WeTongji
{
    public partial class MainPage : PhoneApplicationPage
    {
        #region [Fields]
        private static Boolean isResourceLoaded = false;

        private ActivitySortMethod activitySortMethod = ActivitySortMethod.kByCreationTime;

        private Boolean hasLoadMoreActivities = false;

        private SourceState PersonSourceState = SourceState.NotSet;

        private SourceState TongjiNewsSourceState = SourceState.NotSet;

        private SourceState AroundNewsSourceState = SourceState.NotSet;

        private SourceState ClubNewsSourceState = SourceState.NotSet;

        private SourceState OfficialNoteSourceState = SourceState.NotSet;

        private Boolean isLoadingMoreActivities = false;

        private Boolean isLikePersonButtonEnabled = false;
        private Boolean isUnlikePersonButtonEnabled = false;
        private Boolean isFavoritePersonButtonEnabled = false;
        private Boolean isUnfavoritePersonButtonEnabled = false;

        private Boolean isCurrentCalendarNodeAlarmed = false;

        private GeoCoordinate CurrentLocation = new GeoCoordinate();
        private GeoCoordinateWatcher GCW = new GeoCoordinateWatcher();

        #endregion

        #region [Alarm DispatcherTimer]

        private DispatcherTimer alarmDispatcherTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromSeconds(1),
        };

        #endregion

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            this.TextBlock_Today.Text = DateTime.Now.Day.ToString();

            GCW.PositionChanged += (o, e) =>
                {
                    CurrentLocation = e.Position.Location;
                };

            GCW.Start();

            #region [Add localized buttons, and menu items]

            {
                ApplicationBarIconButton button;
                ApplicationBarMenuItem mi;

                button = new ApplicationBarIconButton(new Uri("/icons/appbar.check.rest.png", UriKind.RelativeOrAbsolute))
                {
                    IsEnabled = false,
                    Text = StringLibrary.MainPage_AppBarLoginText
                };
                button.Click += LogOn_Click;
                this.ApplicationBar.Buttons.Add(button);

                button = new ApplicationBarIconButton(new Uri("/icons/appbar.register.rest.png", UriKind.RelativeOrAbsolute))
                {
                    Text = StringLibrary.MainPage_AppBarRegisterText
                };
                button.Click += NavigateToSignUp;
                this.ApplicationBar.Buttons.Add(button);


                mi = new ApplicationBarMenuItem() { Text = StringLibrary.MainPage_AppBarSettingsText };
                mi.Click += NavigateToSettings;
                this.ApplicationBar.MenuItems.Add(mi);

                mi = new ApplicationBarMenuItem() { Text = StringLibrary.MainPage_AppBarAboutText };
                mi.Click += NavigateToAbout;
                this.ApplicationBar.MenuItems.Add(mi);

                mi = new ApplicationBarMenuItem() { Text = StringLibrary.MainPage_AppBarFeedbackText };
                mi.Click += SendFeedback;
                this.ApplicationBar.MenuItems.Add(mi);

                mi = new ApplicationBarMenuItem() { Text = StringLibrary.MainPage_AppBarShareToFriendsByMail };
                mi.Click += ShareToFriends;
                this.ApplicationBar.MenuItems.Add(mi);
            };

            #endregion

            Global.Instance.UserAvatarChanged += (obj, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        if (UserSource != null)
                        {
                            UserSource.SendPropertyChanged("AvatarImageBrush");
                        }
                    });
                };

            Global.Instance.ActivityScheduleChanged += (obj, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var src = ActivityListSource;
                        if (src != null)
                        {
                            var target = src.Where((a) => a.Id == arg.NewValue.Id).SingleOrDefault();
                            if (target != null)
                            {
                                target.Schedule = arg.NewValue.Schedule;
                                target.CanSchedule = arg.NewValue.CanSchedule;

                                target.SendPropertyChanged("Schedule");
                            }
                        }
                    });
                };

            Global.Instance.FavoriteChanged += (obj, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            UpdateFavoriteNumber(false);
                        }
                        catch { }
                    });
                };

            Global.Instance.AgendaSourceChanged += (obj, arg) =>
                {
                    var node = Global.Instance.AgendaSource.GetNextCalendarNode();

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        if (AlarmClockSource != null && AlarmClockSource.CompareTo(node) == 0)
                            return;
                        else
                            AlarmClockSource = node;
                    });
                };

            alarmDispatcherTimer.Tick += (o, e) =>
                {
                    if (DateTime.Now.Day.ToString() != TextBlock_Today.Text)
                    {
                        PlayTriggerTodayAnimation();
                    }

                    if (String.IsNullOrEmpty(Global.Instance.CurrentUserID) || AlarmClockSource == null || AlarmClockSource.IsNoArrangementNode)
                        return;

                    var src = AlarmClockSource;

                    if (isCurrentCalendarNodeAlarmed)
                    {
                        if (DateTime.Now > src.BeginTime)
                        {
                            (this.Resources["AlarmAnimation"] as Storyboard).Stop();
                            AlarmClockSource = Global.Instance.AgendaSource.GetNextCalendarNode();
                        }
                    }
                    else
                    {
                        switch (src.NodeType)
                        {
                            //...30 min in advance
                            case CalendarNodeType.kActivity:
                                {
                                    if (!isCurrentCalendarNodeAlarmed)
                                    {
                                        //...the phone is locked before alarm should have started and is unlocked after the course starts.
                                        if (DateTime.Now > src.BeginTime)
                                        {
                                            (this.Resources["AlarmAnimation"] as Storyboard).Stop();
                                            AlarmClockSource = Global.Instance.AgendaSource.GetNextCalendarNode();
                                        }
                                        //...it should alarm now
                                        else if (DateTime.Now > src.BeginTime - TimeSpan.FromMinutes(30))
                                        {
                                            isCurrentCalendarNodeAlarmed = true;
                                            (this.Resources["AlarmAnimation"] as Storyboard).Begin();

                                            //...Vibrate
                                            VibrateController.Default.Start(TimeSpan.FromSeconds(1));

                                            //...MessageBox
                                            MessageBox.Show(String.Format(StringLibrary.MainPage_ActivityExpiredPromptContentTemplate, DateTime.Now.ToString("HH:mm"),
                                                                                                                                      src.Title,
                                                                                                                                      (int)(src.BeginTime - DateTime.Now).TotalMinutes + 1),
                                                           StringLibrary.Common_Prompt, MessageBoxButton.OK);

                                        }
                                        //...it is too early to alarm
                                        else
                                        {
                                            //...do nothing
                                        }
                                    }
                                }
                                break;
                            //...1 hour in advance
                            case CalendarNodeType.kExam:
                                {
                                    if (!isCurrentCalendarNodeAlarmed)
                                    {
                                        //...the phone is locked before alarm should have started and is unlocked after the course starts.
                                        if (DateTime.Now > src.BeginTime)
                                        {
                                            (this.Resources["AlarmAnimation"] as Storyboard).Stop();
                                            AlarmClockSource = Global.Instance.AgendaSource.GetNextCalendarNode();
                                        }
                                        //...it should alarm now
                                        else if (DateTime.Now > src.BeginTime - TimeSpan.FromHours(1))
                                        {
                                            isCurrentCalendarNodeAlarmed = true;
                                            (this.Resources["AlarmAnimation"] as Storyboard).Begin();

                                            //...Vibrate
                                            VibrateController.Default.Start(TimeSpan.FromSeconds(1));

                                            //...MessageBox
                                            MessageBox.Show(String.Format(StringLibrary.MainPage_ExamExpiredPromptContentTemplate, DateTime.Now.ToString("HH:mm"),
                                                                                                                                     src.Title,
                                                                                                                                     (int)(src.BeginTime - DateTime.Now).TotalMinutes + 1),
                                                            StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                        }
                                        //...it is too early to alarm
                                        else
                                        {
                                            //...do nothing
                                        }
                                    }
                                }
                                break;
                            //...10 min in advance
                            case CalendarNodeType.kObligedCourse:
                            case CalendarNodeType.kOptionalCourse:
                                {
                                    if (!isCurrentCalendarNodeAlarmed)
                                    {
                                        //...the phone is locked before alarm should have started and is unlocked after the course starts.
                                        if (DateTime.Now > src.BeginTime)
                                        {
                                            (this.Resources["AlarmAnimation"] as Storyboard).Stop();
                                            AlarmClockSource = Global.Instance.AgendaSource.GetNextCalendarNode();
                                        }
                                        //...it should alarm now
                                        else if (DateTime.Now > src.BeginTime - TimeSpan.FromMinutes(10))
                                        {
                                            isCurrentCalendarNodeAlarmed = true;
                                            (this.Resources["AlarmAnimation"] as Storyboard).Begin();

                                            //...Vibrate
                                            VibrateController.Default.Start(TimeSpan.FromSeconds(1));

                                            //...MessageBox
                                            MessageBox.Show(String.Format(StringLibrary.MainPage_CourseExpiredPromptContentTemplate, DateTime.Now.ToString("HH:mm"),
                                                                                                                                   src.Title,
                                                                                                                                   (int)(src.BeginTime - DateTime.Now).TotalMinutes + 1),
                                                        StringLibrary.Common_Prompt, MessageBoxButton.OK);

                                        }
                                        //...it is too early to alarm
                                        else
                                        {
                                            //...do nothing
                                        }
                                    }
                                }
                                break;
                        }
                    }

                };
        }

        #region [Overridden]

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (Global.Instance.Settings.HintOnExit)
            {
                var result = MessageBox.Show(StringLibrary.Common_ExitAppPromptContent, StringLibrary.Common_Prompt, MessageBoxButton.OKCancel);
                if (MessageBoxResult.Cancel == result)
                {
                    e.Cancel = true;
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                using (var db = WTShareDataContext.ShareDB)
                {
                    if (!db.DatabaseExists())
                    {
                        db.CreateDatabase();
                    }
                }

                if (!isResourceLoaded)
                {
                    var thread = new System.Threading.Thread(new ThreadStart(LoadDataFromDatabase))
                    {
                        IsBackground = true,
                        Name = "LoadDataFromDatabase"
                    };

                    thread.Start();
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                    alarmDispatcherTimer.Start();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ProgressBarPopup.Instance.Close();
            alarmDispatcherTimer.Stop();
        }

        #endregion

        #region [Properties]

        private ApplicationBarIconButton Button_Login
        {
            get
            {
                if (Panorama_Core.SelectedIndex == 0 && Border_SignedOut.Visibility == Visibility.Visible)
                    return this.ApplicationBar.Buttons[0] as ApplicationBarIconButton;
                return null;
            }
        }

        private UserExt UserSource
        {
            get
            {
                return Border_SignedIn.DataContext == null ? null : Border_SignedIn.DataContext as UserExt;
            }
            set
            {
                var previousValue = UserSource;
                Border_SignedIn.DataContext = value;

                if (value != null)
                {
                    if (!value.Avatar.EndsWith("missing.png") && !value.AvatarImageExists())
                    {
                        var client = new WTDownloadImageClient();

                        client.DownloadImageCompleted += (obj, arg) =>
                        {
                            this.Dispatcher.BeginInvoke(() =>
                            {
                                value.SendPropertyChanged("AvatarImageBrush");
                            });
                        };

                        client.Execute(value.Avatar, value.AvatarGuid + "." + value.Avatar.GetImageFileExtension());
                    }

                    var sb = this.Resources["AvatarAnimation"] as Storyboard;
                    if (sb.GetCurrentState() == ClockState.Stopped)
                        sb.Begin();
                }
                else
                {
                    var sb = this.Resources["AvatarAnimation"] as Storyboard;
                    if (sb.GetCurrentState() == ClockState.Active)
                        sb.Stop();
                }

                UpdateFavoriteNumber(previousValue == null);
            }
        }

        private ObservableCollection<ActivityExt> ActivityListSource
        {
            get
            {
                if (null == ListBox_Activity.ItemsSource)
                    return null;
                return ListBox_Activity.ItemsSource as ObservableCollection<ActivityExt>;
            }
            set
            {
                ListBox_Activity.ItemsSource = value;

                if (value != null && value.Count > 0)
                {
                    ListBox_Activity.Visibility = Visibility.Visible;
                }
                else
                {
                    ListBox_Activity.Visibility = Visibility.Collapsed;
                }
            }
        }

        private ForStaffExt OfficialNoteSource
        {
            get
            {
                return Grid_OfficialNote.DataContext == null ? null : Grid_OfficialNote.DataContext as ForStaffExt;
            }
            set
            {
                Grid_OfficialNote.DataContext = value;

                if (value != null)
                {
                    var imgUrls = value.GetImagesURL();
                    if (imgUrls != null && imgUrls.Count() > 0)
                    {
                        if (!value.ImageExists())
                        {
                            var url = imgUrls.First();
                            var client = new WTDownloadImageClient();
                            client.DownloadImageCompleted += (o, e) =>
                            {
                                this.Dispatcher.BeginInvoke(() =>
                                {
                                    if (OfficialNoteSource != null)
                                    {
                                        OfficialNoteSource.SendPropertyChanged("FirstImageBrush");

                                        var sb = this.Resources["OfficialNoteImageAnimation"] as Storyboard;
                                        if (sb.GetCurrentState() != ClockState.Active)
                                            sb.Begin();
                                    }
                                });
                            };
                            client.Execute(url, value.ImageExtList.GetImageFilesNames().First());
                        }
                        else
                            (this.Resources["OfficialNoteImageAnimation"] as Storyboard).Begin();
                    }
                }
            }
        }

        private ClubNewsExt ClubNewsSource
        {
            get
            {
                return Grid_ClubNews.DataContext == null ? null : Grid_ClubNews.DataContext as ClubNewsExt;
            }
            set
            {
                Grid_ClubNews.DataContext = value;

                if (value != null)
                {
                    var imgUrls = value.GetImagesURL();
                    if (imgUrls != null && imgUrls.Count() > 0)
                    {
                        if (!value.ImageExists())
                        {
                            var url = imgUrls.First();
                            var client = new WTDownloadImageClient();
                            client.DownloadImageCompleted += (o, e) =>
                            {
                                this.Dispatcher.BeginInvoke(() =>
                                    {
                                        if (ClubNewsSource != null)
                                        {
                                            ClubNewsSource.SendPropertyChanged("FirstImageBrush");

                                            var sb = this.Resources["ClubNewsImageAnimation"] as Storyboard;
                                            if (sb.GetCurrentState() != ClockState.Active)
                                                sb.Begin();
                                        }
                                    });
                            };
                            client.Execute(url, value.ImageExtList.GetImageFilesNames().First());
                        }
                        else
                            (this.Resources["ClubNewsImageAnimation"] as Storyboard).Begin();
                    }
                }
            }
        }

        private SchoolNewsExt TongjiNewsSource
        {
            get
            {
                return Grid_TongjiNews.DataContext == null ? null : Grid_TongjiNews.DataContext as SchoolNewsExt;
            }
            set
            {
                Grid_TongjiNews.DataContext = value;

                if (value != null)
                {
                    var imgUrls = value.GetImagesURL();
                    if (imgUrls != null && imgUrls.Count() > 0)
                    {
                        if (!value.ImageExists())
                        {
                            var url = imgUrls.First();
                            var client = new WTDownloadImageClient();

                            client.DownloadImageCompleted += (o, e) =>
                                {
                                    this.Dispatcher.BeginInvoke(() =>
                                        {
                                            if (TongjiNewsSource != null)
                                            {
                                                TongjiNewsSource.SendPropertyChanged("FirstImageBrush");

                                                var sb = this.Resources["TongjiNewsImageAnimation"] as Storyboard;
                                                if (sb.GetCurrentState() != ClockState.Active)
                                                    sb.Begin();
                                            }
                                        });
                                };

                            client.Execute(url, value.ImageExtList.GetImageFilesNames().First());
                        }
                        else
                        {
                            (this.Resources["TongjiNewsImageAnimation"] as Storyboard).Begin();
                        }
                    }
                }
            }
        }

        private AroundExt AroundNewsSource
        {
            get
            {
                return Grid_AroundNews.DataContext == null ? null : Grid_AroundNews.DataContext as AroundExt;
            }
            set
            {
                Grid_AroundNews.DataContext = value;

                if (value != null)
                {
                    if (!value.IsTitleImageExists())
                    {
                        var client = new WTDownloadImageClient();
                        client.DownloadImageCompleted += (o, e) =>
                        {
                            this.Dispatcher.BeginInvoke(() =>
                                {
                                    if (AroundNewsSource != null)
                                    {
                                        AroundNewsSource.SendPropertyChanged("TitleImageBrush");

                                        var sb = this.Resources["AroundNewsImageAnimation"] as Storyboard;
                                        if (sb.GetCurrentState() != ClockState.Active)
                                            sb.Begin();
                                    }
                                });
                        };
                        client.Execute(value.TitleImage, value.TitleImageGuid + "." + value.TitleImage.GetImageFileExtension());
                    }
                    else
                        (this.Resources["AroundNewsImageAnimation"] as Storyboard).Begin();
                }
            }
        }

        private PersonExt PersonSource
        {
            get
            {
                return ScrollViewer_PeopleOfWeek.DataContext == null ? null : ScrollViewer_PeopleOfWeek.DataContext as PersonExt;
            }
            set
            {
                ScrollViewer_PeopleOfWeek.DataContext = value;

                isLikePersonButtonEnabled = isUnlikePersonButtonEnabled = isFavoritePersonButtonEnabled = isUnfavoritePersonButtonEnabled = true;

                if (value != null)
                {
                    ScrollViewer_PeopleOfWeek.Visibility = Visibility.Visible;

                    #region [Avatar]

                    var avatarClient = new WTDownloadImageClient();
                    avatarClient.DownloadImageCompleted += (o, e) =>
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            if (PersonSource != null)
                            {
                                PersonSource.SendPropertyChanged("AvatarImageBrush");
                            }
                        });

                    };
                    avatarClient.Execute(value.Avatar, value.AvatarGuid + "." + value.Avatar.GetImageFileExtension());

                    #endregion

                    #region [First Image]

                    var filesName = value.ImageExtList.GetImageFilesNames();
                    var images = value.GetImages();
                    if (images.Count > 0 && !value.ImageExists())
                    {
                        var client = new WTDownloadImageClient();
                        client.DownloadImageCompleted += (o, e) =>
                        {
                            this.Dispatcher.BeginInvoke(() =>
                            {
                                PersonSource.SendPropertyChanged("FirstImageBrush");
                            });

                        };
                        client.Execute(images.First().Key, filesName.First());
                    }
                    #endregion
                }
                else
                {
                    ScrollViewer_PeopleOfWeek.Visibility = Visibility.Collapsed;
                }
            }
        }

        private CalendarNode AlarmClockSource
        {
            get
            {
                return Button_Alarm.DataContext == null ? null : Button_Alarm.DataContext as CalendarNode;
            }
            set
            {
                Button_Alarm.DataContext = value;

                #region [Clock]
                if (value == null)
                {
                    (this.Resources["ResetAlarmClockPointersAnimation"] as Storyboard).Begin();
                    alarmDispatcherTimer.Stop();
                }
                else
                {
                    TextBlock_NoArrangement.Visibility = value.IsNoArrangementNode ? Visibility.Visible : Visibility.Collapsed;

                    var visibleAnimation = this.Resources["VisibleAlarmClockPointersAnimation"] as Storyboard;
                    var setAnimation = this.Resources["SetAlarmClockAnimation"] as Storyboard;

                    setAnimation.Stop();

                    if (HourPointer.Opacity == 0)
                    {
                        //...Hour pointer
                        double hptr = value.IsNoArrangementNode ? DateTime.Now.GetHourPointerRotationDegree() : value.BeginTime.GetHourPointerRotationDegree();
                        hptr = hptr > 0 ? hptr : hptr + 360;

                        //...Minute pointer
                        double mptr = value.IsNoArrangementNode ? DateTime.Now.GetMinutePointerRotationDegree() : value.BeginTime.GetMinutePointerRotationDegree();
                        mptr = mptr > 0 ? mptr : mptr + 360;

                        (setAnimation.Children[0] as DoubleAnimationUsingKeyFrames).KeyFrames[0].Value = 0;
                        (setAnimation.Children[0] as DoubleAnimationUsingKeyFrames).KeyFrames[1].Value = hptr;
                        (setAnimation.Children[1] as DoubleAnimationUsingKeyFrames).KeyFrames[0].Value = 0;
                        (setAnimation.Children[1] as DoubleAnimationUsingKeyFrames).KeyFrames[1].Value = mptr;
                    }
                    else
                    {
                        //...Round start rotation degree
                        double hptr_start = (setAnimation.Children[0] as DoubleAnimationUsingKeyFrames).KeyFrames[0].Value;
                        double mptr_start = (setAnimation.Children[1] as DoubleAnimationUsingKeyFrames).KeyFrames[0].Value;
                        (setAnimation.Children[0] as DoubleAnimationUsingKeyFrames).KeyFrames[0].Value = hptr_start > 180 ? (hptr_start - 360) : ((hptr_start < -180) ? (hptr_start + 360) : hptr_start);
                        (setAnimation.Children[1] as DoubleAnimationUsingKeyFrames).KeyFrames[0].Value = mptr_start > 180 ? (mptr_start - 360) : ((mptr_start < -180) ? (mptr_start + 360) : mptr_start);

                        //...Hour pointer
                        double hptr = value.IsNoArrangementNode ? DateTime.Now.GetHourPointerRotationDegree() : value.BeginTime.GetHourPointerRotationDegree();
                        hptr = hptr > 0 ? hptr : hptr + 360;
                        hptr = hptr >= hptr_start ? hptr : hptr + 360;

                        //...Minute pointer
                        double mptr = value.IsNoArrangementNode ? DateTime.Now.GetMinutePointerRotationDegree() : value.BeginTime.GetMinutePointerRotationDegree();
                        mptr = mptr > 0 ? mptr : mptr + 360;
                        mptr = mptr >= mptr_start ? mptr : mptr + 360;

                        (setAnimation.Children[0] as DoubleAnimationUsingKeyFrames).KeyFrames[0].Value = hptr_start;
                        (setAnimation.Children[0] as DoubleAnimationUsingKeyFrames).KeyFrames[1].Value = hptr;
                        (setAnimation.Children[1] as DoubleAnimationUsingKeyFrames).KeyFrames[0].Value = mptr_start;
                        (setAnimation.Children[1] as DoubleAnimationUsingKeyFrames).KeyFrames[1].Value = mptr;
                    }

                    visibleAnimation.Begin();
                    setAnimation.Begin();
                }
                #endregion

                #region [DispatcherTimer]

                isCurrentCalendarNodeAlarmed = false;

                if (value == null || value.IsNoArrangementNode)
                {
                    alarmDispatcherTimer.Stop();
                    (this.Resources["AlarmAnimation"] as Storyboard).Stop();
                }
                else if (!alarmDispatcherTimer.IsEnabled)
                    alarmDispatcherTimer.Start();

                #endregion
            }
        }

        #endregion

        #region [Functions]

        #region [Nav]

        private void MyButtonClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var str = button.Content as String;
            this.NavigationService.Navigate(new Uri("/Pages/" + str + ".xaml", UriKind.RelativeOrAbsolute));
        }

        void NavToPeopleOfWeek(object sender, RoutedEventArgs e)
        {
            var src = PersonSource;

            if (src != null)
            {
                #region [Flurry]
#if Flurry
                FlurryWP8SDK.Api.LogEvent(
                    ((int)FlurryWP8SDK.Models.EventName.ViewWeeklyStar).ToString(),
                    new List<FlurryWP8SDK.Models.Parameter>(new FlurryWP8SDK.Models.Parameter[]
                            {
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.TapToViewWeeklyStarParameter).ToString(), 
                                    ((int)FlurryWP8SDK.Models.ParameterValue.SeeMore).ToString())
                            })
                    );
#endif
                #endregion

                this.NavigationService.Navigate(new Uri(String.Format("/Pages/PeopleOfWeek.xaml?q={0}", src.Id), UriKind.RelativeOrAbsolute));
            }
        }

        void NavToForgotPassword(Object sender, RoutedEventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickForgetPassword).ToString());
#endif
            #endregion

            this.NavigationService.Navigate(new Uri("/Pages/ForgotPassword.xaml", UriKind.RelativeOrAbsolute));
        }

        private void NavigateToSignUp(object sender, EventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickAppBarSignUpButton).ToString());
#endif
            #endregion

            this.NavigationService.Navigate(new Uri("/Pages/TongjiMail.xaml", UriKind.RelativeOrAbsolute));
        }

        private void ListBox_Activity_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            var lb = sender as ListBox;

            if (lb.SelectedIndex == -1)
                return;

            var a = lb.SelectedItem as ActivityExt;
            lb.SelectedIndex = -1;
            if (a.IsValid)
                this.NavigationService.Navigate(new Uri("/Pages/Activity.xaml?q=" + a.Id, UriKind.RelativeOrAbsolute));
        }

        private void NavToOfficialNotesList(Object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(
                ((int)FlurryWP8SDK.Models.EventName.ViewCampusNews).ToString(),
                new List<FlurryWP8SDK.Models.Parameter>(new FlurryWP8SDK.Models.Parameter[]
                            {
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.CampusNewsParameter).ToString(), 
                                    ((int)FlurryWP8SDK.Models.ParameterValue.OfficialNote).ToString())
                            })
                );
#endif
            #endregion

            this.NavigationService.Navigate(new Uri("/Pages/CampusInfo.xaml?q=2", UriKind.RelativeOrAbsolute));
        }

        private void NavToClubNewsList(Object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(
                ((int)FlurryWP8SDK.Models.EventName.ViewCampusNews).ToString(),
                new List<FlurryWP8SDK.Models.Parameter>(new FlurryWP8SDK.Models.Parameter[]
                            {
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.CampusNewsParameter).ToString(), 
                                    ((int)FlurryWP8SDK.Models.ParameterValue.GroupNotices).ToString())
                            })
                );
#endif
            #endregion

            this.NavigationService.Navigate(new Uri("/Pages/CampusInfo.xaml?q=3", UriKind.RelativeOrAbsolute));
        }

        private void NavToTongjiNewsList(Object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(
                ((int)FlurryWP8SDK.Models.EventName.ViewCampusNews).ToString(),
                new List<FlurryWP8SDK.Models.Parameter>(new FlurryWP8SDK.Models.Parameter[]
                            {
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.CampusNewsParameter).ToString(), 
                                    ((int)FlurryWP8SDK.Models.ParameterValue.TongjiNews).ToString())
                            })
                );
#endif
            #endregion

            this.NavigationService.Navigate(new Uri("/Pages/CampusInfo.xaml?q=0", UriKind.RelativeOrAbsolute));
        }

        private void NavToAroundNewsList(Object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(
                ((int)FlurryWP8SDK.Models.EventName.ViewCampusNews).ToString(),
                new List<FlurryWP8SDK.Models.Parameter>(new FlurryWP8SDK.Models.Parameter[]
                            {
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.CampusNewsParameter).ToString(), 
                                    ((int)FlurryWP8SDK.Models.ParameterValue.Recommends).ToString())
                            })
                );
#endif
            #endregion

            this.NavigationService.Navigate(new Uri("/Pages/CampusInfo.xaml?q=1", UriKind.RelativeOrAbsolute));
        }

        private void ViewPersonalProfile(Object sender, RoutedEventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ViewPersonalProfile).ToString());
#endif
            #endregion

            this.NavigationService.Navigate(new Uri("/Pages/PersonalProfile.xaml", UriKind.RelativeOrAbsolute));
        }

        private void ViewMyFavorite(Object sender, RoutedEventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ViewMyFavorites).ToString());
#endif
            #endregion

            this.NavigationService.Navigate(new Uri("/Pages/MyFavorite.xaml", UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// View the alarm item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewAlarmItem(Object sender, RoutedEventArgs e)
        {
            if (AlarmClockSource == null || AlarmClockSource.IsNoArrangementNode)
            {
#if Flurry

                FlurryWP8SDK.Api.LogEvent(
                    ((int)FlurryWP8SDK.Models.EventName.ClickReminder).ToString(),
                    new List<FlurryWP8SDK.Models.Parameter>(
                        new FlurryWP8SDK.Models.Parameter[]{
                                        new FlurryWP8SDK.Models.Parameter((
                                            (int)FlurryWP8SDK.Models.ParameterName.ClickReminderParameter).ToString(), 
                                            ((int)FlurryWP8SDK.Models.ParameterValue.NoArrangement).ToString())})
                    );
#endif
                this.NavigationService.Navigate(new Uri("/Pages/MyAgenda.xaml", UriKind.RelativeOrAbsolute));
            }
            else
            {
                var src = AlarmClockSource;
                switch (src.NodeType)
                {
                    case CalendarNodeType.kActivity:
                        {
#if Flurry
                            FlurryWP8SDK.Api.LogEvent(
                                ((int)FlurryWP8SDK.Models.EventName.ClickReminder).ToString(),
                                new List<FlurryWP8SDK.Models.Parameter>(
                                    new FlurryWP8SDK.Models.Parameter[]{
                                                    new FlurryWP8SDK.Models.Parameter((
                                                        (int)FlurryWP8SDK.Models.ParameterName.ClickReminderParameter).ToString(), 
                                                        ((int)FlurryWP8SDK.Models.ParameterValue.Activity).ToString())})
                                );
#endif
                            this.NavigationService.Navigate(new Uri(String.Format("/Pages/Activity.xaml?q={0}", src.Id), UriKind.RelativeOrAbsolute));
                        }
                        break;
                    case CalendarNodeType.kExam:
                        {
#if Flurry
                            FlurryWP8SDK.Api.LogEvent(
                                ((int)FlurryWP8SDK.Models.EventName.ClickReminder).ToString(),
                                new List<FlurryWP8SDK.Models.Parameter>(
                                    new FlurryWP8SDK.Models.Parameter[]{
                                                    new FlurryWP8SDK.Models.Parameter((
                                                        (int)FlurryWP8SDK.Models.ParameterName.ClickReminderParameter).ToString(), 
                                                        ((int)FlurryWP8SDK.Models.ParameterValue.Exam).ToString())})
                                );
#endif
                            this.NavigationService.Navigate(new Uri(String.Format("/Pages/CourseDetail.xaml?q={0}", src.Id), UriKind.RelativeOrAbsolute));
                        }
                        break;
                    case CalendarNodeType.kObligedCourse:
                    case CalendarNodeType.kOptionalCourse:
                        {
#if Flurry
                            FlurryWP8SDK.Api.LogEvent(
                                ((int)FlurryWP8SDK.Models.EventName.ClickReminder).ToString(),
                                new List<FlurryWP8SDK.Models.Parameter>(
                                    new FlurryWP8SDK.Models.Parameter[]{
                                                    new FlurryWP8SDK.Models.Parameter((
                                                        (int)FlurryWP8SDK.Models.ParameterName.ClickReminderParameter).ToString(), 
                                                        ((int)FlurryWP8SDK.Models.ParameterValue.Course).ToString())})
                                );
#endif

                            this.NavigationService.Navigate(new Uri(String.Format("/Pages/CourseDetail.xaml?q={0}&d={1}", src.Id, src.BeginTime), UriKind.RelativeOrAbsolute));
                        }
                        break;
                }
            }
        }

        private void ViewMyAgenda(Object sender, RoutedEventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ViewAgenda).ToString());
#endif
            #endregion

            this.NavigationService.Navigate(new Uri("/Pages/MyAgenda.xaml", UriKind.RelativeOrAbsolute));
        }

        private void NavigateToSettings(Object sender, EventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickAppBarSettingsMenuItem).ToString());
#endif
            #endregion

            this.NavigationService.Navigate(new Uri("/Pages/MySettings.xaml", UriKind.RelativeOrAbsolute));
        }

        private void NavigateToAbout(Object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Pages/About.xaml", UriKind.RelativeOrAbsolute));
        }

        private void NavToPeopleOfWeekList(Object sender, EventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickAppBarHistoryButton).ToString());
#endif
            #endregion

            this.NavigationService.Navigate(new Uri("/Pages/PeopleOfWeekList.xaml", UriKind.RelativeOrAbsolute));
        }

        #endregion

        #region [Api]

        /// <summary>
        /// LogOn demo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogOn_Click(object sender, EventArgs e)
        {
            try
            {
                Button_Login.IsEnabled = false;
            }
            catch { }

            this.Focus();

            var no = this.TextBox_Id.Text;
            var pw = this.PasswordBox_Password.Password;


            var req = new UserLogOnRequest<UserLogOnResponse>();
            var client = new WTDefaultClient<UserLogOnResponse>();

            req.NO = no;
            req.Password = pw;

            try
            {
                req.Validate();
            }
            catch (Exception ex)
            {
                if (ex is ArgumentOutOfRangeException)
                {
                    MessageBox.Show(StringLibrary.MainPage_StudentNOErrorPromptText, StringLibrary.MainPage_StudentNOErrorPromptCaption, MessageBoxButton.OK);

                    TextBox_Id.Focus();
                    TextBox_Id.SelectAll();
                    return;
                }
                else if (ex is ArgumentNullException)
                {
                    MessageBox.Show(StringLibrary.MainPage_PasswordLengthErrorPromptText, StringLibrary.MainPage_PasswordLengthErrorPromptCaption, MessageBoxButton.OK);

                    PasswordBox_Password.Focus();
                    PasswordBox_Password.SelectAll();
                    return;
                }
            }

            client.ExecuteCompleted += (obj, arg) =>
                {
                    Global.Instance.CurrentUserID = arg.Result.User.UID;
                    Global.Instance.UpdateSettings(arg.Result.User.UID, pw, arg.Result.Session);
                    Global.Instance.StartSettingAgendaSource();

                    UserExt targetUser = null;

                    #region [Handle database]

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        db.ResetLikeFavoriteSchedule();
                    }

                    using (var db = new WeTongji.DataBase.WTUserDataContext(arg.Result.User.UID))
                    {
                        var userInfo = db.UserInfo.SingleOrDefault();

                        //...Create an instance if the user never signs in.
                        if (userInfo == null)
                        {
                            targetUser = new UserExt();
                            targetUser.SetObject(arg.Result.User);

                            db.UserInfo.InsertOnSubmit(targetUser);
                        }
                        //...Update user's info
                        else
                        {
                            userInfo.SetObject(arg.Result.User);
                            targetUser = userInfo;
                        }

                        db.SubmitChanges();
                    }

                    #endregion

                    #region [Flurry]
#if Flurry
                    FlurryWP8SDK.Api.LogEvent(
                        ((int)FlurryWP8SDK.Models.EventName.SignIn).ToString(),
                        new List<FlurryWP8SDK.Models.Parameter>(new FlurryWP8SDK.Models.Parameter[]
                                    {
                                        new FlurryWP8SDK.Models.Parameter(
                                            ((int)FlurryWP8SDK.Models.ParameterName.SignInParameter).ToString(), 
                                            ((int)FlurryWP8SDK.Models.ParameterValue.SignIn).ToString())
                                    })
                        );

                    FlurryWP8SDK.Api.SetUserId(targetUser.UID);
                    FlurryWP8SDK.Api.SetAge(Math.Max(0, DateTime.Now.Year - targetUser.Birthday.Year));
                    FlurryWP8SDK.Api.SetGender(targetUser.IsMale ? FlurryWP8SDK.Models.Gender.Male : FlurryWP8SDK.Models.Gender.Female);
                    if (!CurrentLocation.IsUnknown)
                        FlurryWP8SDK.Api.SetLocation(CurrentLocation.Latitude, CurrentLocation.Longitude, Convert.ToSingle(CurrentLocation.HorizontalAccuracy));
#endif
                    #endregion

                    #region [download courses, favorites and schedule in order]

                    Global.Instance.ParticipatingActivitiesIdList.Clear();

                    DownloadCourses(arg.Result.Session, arg.Result.User.UID);

                    DownloadFavorite(arg.Result.Session, arg.Result.User.UID);

                    #endregion

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        UserSource = targetUser;

                        Border_SignedOut.Visibility = Visibility.Collapsed;
                        PlayTriggerTodayAnimation();
                        TextBox_Id.Text = String.Empty;
                        PasswordBox_Password.Password = String.Empty;
                        StartComputingCalendarNodes();

                        if (Panorama_Core.SelectedIndex == 0)
                        {
                            (this.ApplicationBar as ApplicationBar).Buttons.Clear();

                            ApplicationBarIconButton button;
                            button = new ApplicationBarIconButton(new Uri("/icons/appbar.refresh.rest.png", UriKind.RelativeOrAbsolute))
                            {
                                Text = StringLibrary.MainPage_AppBarRefreshText
                            };
                            button.Click += RefreshButton_Click;
                            (this.ApplicationBar as ApplicationBar).Buttons.Add(button);
                        }

                        var mi = new ApplicationBarMenuItem()
                        {
                            Text = StringLibrary.MainPage_AppBarSignOutText
                        };
                        mi.Click += SignOut;
                        this.ApplicationBar.MenuItems.Add(mi);

                        WTToast.Instance.Show(StringLibrary.Toast_SignInSucceededPrompt);
                    });
                };

            client.ExecuteFailed += (obj, arg) =>
                {
                    var err = arg.Error;

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            Button_Login.IsEnabled = true;
                        }
                        catch { }

                        if (err is WTException)
                        {
                            var wte = err as WTException;
                            switch (wte.StatusCode.Id)
                            {
                                case WeTongji.Api.Util.Status.LoginTimeOut:
                                    MessageBox.Show(StringLibrary.MainPage_LoginTimeOutPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                    return;
                                case WeTongji.Api.Util.Status.NoAccount:
                                    {
                                        MessageBox.Show(StringLibrary.MainPage_NoAccountPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                        TextBox_Id.Focus();
                                        TextBox_Id.SelectAll();
                                    }
                                    return;
                                case WeTongji.Api.Util.Status.NotActivatedAccount:
                                    {
                                        MessageBox.Show(StringLibrary.MainPage_NotActivatedAccountPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                        TextBox_Id.Focus();
                                        TextBox_Id.SelectAll();
                                    }
                                    return;
                                case WeTongji.Api.Util.Status.NotRegistered:
                                    {
                                        MessageBox.Show(StringLibrary.MainPage_NotRegisteredPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                        TextBox_Id.Focus();
                                        TextBox_Id.SelectAll();
                                    }
                                    return;
                                case WeTongji.Api.Util.Status.InvalidPassword:
                                    {
                                        MessageBox.Show(StringLibrary.MainPage_InvalidPasswordPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                        PasswordBox_Password.Focus();
                                        PasswordBox_Password.SelectAll();
                                    }
                                    return;
                                case WeTongji.Api.Util.Status.AccountPasswordDismatch:
                                    {
                                        MessageBox.Show(StringLibrary.MainPage_AccountPasswordDismatchPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                                        PasswordBox_Password.Focus();
                                        PasswordBox_Password.SelectAll();
                                    }
                                    return;
                            }

                            MessageBox.Show(StringLibrary.MainPage_CommonLoginFailedPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                        }
                        else if (err is System.Net.WebException)
                        {
                            WTToast.Instance.Show(StringLibrary.Toast_NetworkErrorPrompt);
                            try
                            {
                                Button_Login.IsEnabled = true;
                            }
                            catch { }
                        }
                        else
                        {
                            MessageBox.Show(StringLibrary.MainPage_CommonLoginFailedPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                        }
                    });
                };

            client.Execute(req);
        }

        private void AutoLogin()
        {
            //...Return if no user has logged in ever before or the settings file is missing.
            this.Dispatcher.BeginInvoke(() =>
            {
                if (String.IsNullOrEmpty(Global.Instance.Settings.UID))
                {
                    Grid_AutoLoginPrompt.Visibility = Visibility.Collapsed;
                }
                else if (Grid_AutoLoginPrompt.Visibility == Visibility.Collapsed)
                { }
                else
                {
                    AutoLoginCore();
                }
            });
        }

        private void AutoLoginCore()
        {
            var req = new UserLogOnRequest<UserLogOnResponse>();
            var client = new WTDefaultClient<UserLogOnResponse>();

            #region [Assign request parameters]

            //...Get student NO from local database
            if (!WTUserDataContext.UserDataContextExists(Global.Instance.Settings.UID))
            {
                //...Return if local database cannot be found.
                this.Dispatcher.BeginInvoke(() =>
                {
                    Grid_AutoLoginPrompt.Visibility = Visibility.Collapsed;
                });
                return;
            }

            using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
            {
                var userInfo = db.UserInfo.SingleOrDefault();

                //...Return if user info is missing.
                if (userInfo == null)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        Grid_AutoLoginPrompt.Visibility = Visibility.Collapsed;
                    });
                    return;
                }

                req.NO = userInfo.NO;
            }

            req.Password = Global.Instance.Settings.CryptPassword.GetOriginalPassword();

            #endregion

            #region [Add execute completed handlers]

            client.ExecuteCompleted += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();

                    if (Grid_AutoLoginPrompt.Visibility == Visibility.Visible)
                    {
                        var thread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(OnAutoLoginCompleted));
                        thread.Start(arg);
                    }
                });
            };

            client.ExecuteFailed += (obj, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    Grid_AutoLoginPrompt.Visibility = Visibility.Collapsed;
                    ProgressBarPopup.Instance.Close();
                });
            };

            #endregion

            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
            });
            client.Execute(req);
        }

        private void OnAutoLoginCompleted(Object param)
        {
            var arg = param as WTExecuteCompletedEventArgs<UserLogOnResponse>;

            if (arg == null)
                return;

            Global.Instance.CurrentUserID = arg.Result.User.UID;
            Global.Instance.CurrentUserID = arg.Result.User.UID;
            Global.Instance.UpdateSession(arg.Result.Session);

            UserExt targetUser = null;

            #region [Handle database]

            using (var db = WTShareDataContext.ShareDB)
            {
                db.ResetLikeFavoriteSchedule();
            }

            using (var db = new WeTongji.DataBase.WTUserDataContext(arg.Result.User.UID))
            {
                var userInfo = db.UserInfo.SingleOrDefault();

                //...Create an instance if the user never signs in.
                if (userInfo == null)
                {
                    targetUser = new UserExt();
                    targetUser.SetObject(arg.Result.User);

                    db.UserInfo.InsertOnSubmit(targetUser);
                }
                //...Update user's info
                else
                {
                    userInfo.SetObject(arg.Result.User);
                    targetUser = userInfo;
                }

                db.SubmitChanges();
            }

            #endregion

            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(
                ((int)FlurryWP8SDK.Models.EventName.SignIn).ToString(),
                new List<FlurryWP8SDK.Models.Parameter>(new FlurryWP8SDK.Models.Parameter[]
                {
                    new FlurryWP8SDK.Models.Parameter(
                        ((int)FlurryWP8SDK.Models.ParameterName.SignInParameter).ToString(), 
                        ((int)FlurryWP8SDK.Models.ParameterValue.AutoSignIn).ToString())
                })
                );

            FlurryWP8SDK.Api.SetUserId(targetUser.UID);
            FlurryWP8SDK.Api.SetAge(Math.Max(0, DateTime.Now.Year - targetUser.Birthday.Year));
            FlurryWP8SDK.Api.SetGender(targetUser.IsMale ? FlurryWP8SDK.Models.Gender.Male : FlurryWP8SDK.Models.Gender.Female);
            if (!CurrentLocation.IsUnknown)
                FlurryWP8SDK.Api.SetLocation(CurrentLocation.Latitude, CurrentLocation.Longitude, Convert.ToSingle(CurrentLocation.HorizontalAccuracy));
#endif
            #endregion

            #region [download courses, favorites and schedule in order]

            Global.Instance.ParticipatingActivitiesIdList.Clear();

            DownloadCourses(arg.Result.Session, arg.Result.User.UID);

            DownloadFavorite(arg.Result.Session, arg.Result.User.UID);

            #endregion

            this.Dispatcher.BeginInvoke(() =>
            {
                UserSource = targetUser;

                StartComputingCalendarNodes();

                Grid_AutoLoginPrompt.Visibility = Visibility.Collapsed;
                Border_SignedOut.Visibility = Visibility.Collapsed;
                TextBox_Id.Text = String.Empty;
                PasswordBox_Password.Password = String.Empty;
                WTToast.Instance.Show(String.Format(StringLibrary.Toast_AutoLoginSucceededPromptTemplate, arg.Result.User.DisplayName));
                PlayTriggerTodayAnimation();

                if (Panorama_Core.SelectedIndex == 0)
                {
                    (this.ApplicationBar as ApplicationBar).Buttons.Clear();

                    ApplicationBarIconButton button;
                    button = new ApplicationBarIconButton(new Uri("/icons/appbar.refresh.rest.png", UriKind.RelativeOrAbsolute))
                    {
                        Text = StringLibrary.MainPage_AppBarRefreshText
                    };
                    button.Click += RefreshButton_Click;
                    (this.ApplicationBar as ApplicationBar).Buttons.Add(button);
                }

                var mi = new ApplicationBarMenuItem()
                {
                    Text = StringLibrary.MainPage_AppBarSignOutText
                };
                mi.Click += SignOut;
                this.ApplicationBar.MenuItems.Add(mi);
            });
        }

        #region [Refresh related functions]

        #region [Activities]

        private void GetAllUnexpiredActivities()
        {
            int unexpirePageId = -1;
            Action updateUnExpireAction = null;

            #region [Core action]

            updateUnExpireAction = () =>
            {
                ActivitiesGetRequest<ActivitiesGetResponse> req = new ActivitiesGetRequest<ActivitiesGetResponse>();
                WTDefaultClient<ActivitiesGetResponse> client = new WTDefaultClient<ActivitiesGetResponse>();

                Debug.WriteLine("[updateExpireAction Thread]" + Thread.CurrentThread.GetHashCode());

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Open();
                });

                #region [Add additional parameter]

                if (unexpirePageId > 0)
                    req.SetAdditionalParameter(WTDefaultClient<ActivitiesGetResponse>.PAGE, unexpirePageId);

                req.Expire = false;

                #endregion

                #region [Subscribe event handler]

                #region [Execute completed]
                client.ExecuteCompleted += (o, arg) =>
                {
                    int count = arg.Result.Activities.Count();
                    int newActivities = 0;

                    for (int i = 0; i < count; ++i)
                    {
                        ActivityExt itemInDB = null;

                        using (var db = WTShareDataContext.ShareDB)
                        {
                            var item = arg.Result.Activities[i];
                            itemInDB = db.Activities.Where((a) => a.Id == item.Id).FirstOrDefault();

                            //...There is no such item
                            if (itemInDB == null)
                            {
                                ++newActivities;

                                itemInDB = new ActivityExt();
                                itemInDB.SetObject(item);

                                db.Activities.InsertOnSubmit(itemInDB);
                            }
                            else
                            {
                                itemInDB.SetObject(item);
                            }

                            db.SubmitChanges();
                        }

                        unexpirePageId = arg.Result.NextPager;

                        this.Dispatcher.BeginInvoke(() =>
                        {
                            var hasMore = false;

                            if (ActivityListSource != null && ActivityListSource.Count > 0 && !ActivityListSource.Last().IsValid)
                                hasMore = true;

                            UpdateAcitivityList(new ActivityExt[] { itemInDB }, arg.Result.NextPager > 0, hasMore);
                        });
                    }

                    if (newActivities > 0)
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            if (newActivities == 1)
                                WTToast.Instance.Show(StringLibrary.MainPage_ReceiveANewActivityTemplate);
                            else
                                WTToast.Instance.Show(String.Format(StringLibrary.MainPage_ReceiveNewActivitiesTemplate, newActivities));
                        });
                    }

                    if (arg.Result.NextPager > 0)
                    {
                        updateUnExpireAction();
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            ProgressBarPopup.Instance.Close();
                        });
                    }
                };
                #endregion

                #region [Execute Failed]
                client.ExecuteFailed += (o, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();
                    });
                };
                #endregion

                #endregion

                if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                    client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
                else
                    client.Execute(req);
            };

            #endregion

            updateUnExpireAction();
        }

        private void RefreshExistingExpiredActivities()
        {
            Action updateExpireAction = null;

            Global.Instance.ActivityPageId = -1;
            Global.Instance.CurrentActivitySourceState = SourceState.Setting;

            var src = ActivityListSource;
            if (src != null && src.Count > 0 && !src.Last().IsValid)
            {
                src.RemoveAt(src.Count - 1);
            }

            #region [Core action]
            updateExpireAction = () =>
            {
                if (this.NavigationService.CurrentSource.ToString() != "/MainPage.xaml")
                    return;

                ActivitiesGetRequest<ActivitiesGetResponse> req = new ActivitiesGetRequest<ActivitiesGetResponse>();
                WTDefaultClient<ActivitiesGetResponse> client = new WTDefaultClient<ActivitiesGetResponse>();

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Open();
                });

                #region [Add additional parameter]

                if (Global.Instance.ActivityPageId > 0)
                    req.SetAdditionalParameter(WTDefaultClient<ActivitiesGetResponse>.PAGE, Global.Instance.ActivityPageId);

                req.Expire = true;

                #endregion

                #region [Subscribe event handler]

                #region [Execute completed]
                client.ExecuteCompleted += (o, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();
                    });

                    Boolean anyActivityExisting = false;
                    int count = arg.Result.Activities.Count();

                    for (int i = 0; i < count; ++i)
                    {
                        ActivityExt itemInDB = null;
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            var item = arg.Result.Activities[i];
                            itemInDB = db.Activities.Where((a) => a.Id == item.Id).FirstOrDefault();

                            //...There is no such item
                            if (itemInDB == null)
                            {
                                itemInDB = new ActivityExt();
                                itemInDB.SetObject(item);

                                db.Activities.InsertOnSubmit(itemInDB);
                            }
                            else
                            {
                                itemInDB.SetObject(item);
                                anyActivityExisting = true;
                            }

                            db.SubmitChanges();
                        }

                        if (itemInDB != null)
                        {
                            if (i < count - 1)
                            {
                                this.Dispatcher.BeginInvoke(() =>
                                    {
                                        UpdateAcitivityList(new ActivityExt[] { itemInDB }, arg.Result.NextPager != 0, false);
                                    });
                            }
                            else if (arg.Result.NextPager > 0 && anyActivityExisting)
                            {
                                this.Dispatcher.BeginInvoke(() =>
                                {
                                    UpdateAcitivityList(new ActivityExt[] { itemInDB }, true, true);

                                    updateExpireAction();
                                });
                            }
                            else
                            {
                                this.Dispatcher.BeginInvoke(() =>
                                {
                                    UpdateAcitivityList(new ActivityExt[] { itemInDB }, false, arg.Result.NextPager <= 0 ? false : true);

                                    Global.Instance.CurrentActivitySourceState = SourceState.Done;
                                });
                            }
                        }
                    }

                    Global.Instance.ActivityPageId = arg.Result.NextPager;
                };
                #endregion

                #region [Execute Failed]
                client.ExecuteFailed += (o, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        Global.Instance.CurrentActivitySourceState = SourceState.NotSet;
                        ProgressBarPopup.Instance.Close();
                    });
                };
                #endregion

                #endregion

                if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                    client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
                else
                    client.Execute(req);
            };
            #endregion

            updateExpireAction();
        }

        private void LoadAnotherExpiredActivities()
        {
            if (Global.Instance.ActivityPageId == 0)
                return;

            ActivitiesGetRequest<ActivitiesGetResponse> req = new ActivitiesGetRequest<ActivitiesGetResponse>();
            WTDefaultClient<ActivitiesGetResponse> client = new WTDefaultClient<ActivitiesGetResponse>();

            #region [Add additional parameter]

            if (Global.Instance.ActivityPageId > 0)
                req.SetAdditionalParameter(WTDefaultClient<ActivitiesGetResponse>.PAGE, Global.Instance.ActivityPageId);

            req.IsAsc = false;
            req.Expire = true;

            #endregion

            #region [Subscribe event handler]

            #region [Execute completed]
            client.ExecuteCompleted += (o, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                });

                int count = arg.Result.Activities.Count();

                for (int i = 0; i < count; ++i)
                {
                    ActivityExt itemInDB = null;

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        var item = arg.Result.Activities[i];
                        itemInDB = db.Activities.Where((a) => a.Id == item.Id).FirstOrDefault();

                        //...There is no such item
                        if (itemInDB == null)
                        {
                            itemInDB = new ActivityExt();
                            itemInDB.SetObject(item);

                            db.Activities.InsertOnSubmit(itemInDB);
                        }
                        else
                        {
                            itemInDB.SetObject(item);
                        }

                        db.SubmitChanges();
                    }

                    if (i < count - 1)
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            UpdateAcitivityList(new ActivityExt[] { itemInDB }, false, false);
                        });
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            UpdateAcitivityList(new ActivityExt[] { itemInDB }, false, arg.Result.NextPager > 0);
                        });
                    }

                }

                Global.Instance.ActivityPageId = arg.Result.NextPager;
            };
            #endregion

            #region [Execute Failed]
            client.ExecuteFailed += (o, arg) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                });
            };
            #endregion

            #endregion

            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
            });

            if (!String.IsNullOrEmpty(Global.Instance.CurrentUserID))
                client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
            else
                client.Execute(req);
        }

        #endregion

        #region [People of Week]

        private void GetLatestPerson()
        {
            var req = new PersonGetLatestRequest<PersonGetLatestResponse>();
            var client = new WTDefaultClient<PersonGetLatestResponse>();

            var uid = Global.Instance.CurrentUserID;

            client.ExecuteCompleted += (obj, arg) =>
                {
                    //...Try again if user changed
                    if (uid != Global.Instance.CurrentUserID)
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            GetLatestPerson();
                        });
                        return;
                    }

                    PersonExt target = null;

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        target = db.People.Where((p) => p.Id == arg.Result.Person.Id).SingleOrDefault();

                        if (target == null)
                        {
                            target = new PersonExt();
                            target.SetObject(arg.Result.Person);
                            db.People.InsertOnSubmit(target);
                        }
                        else
                        {
                            target.SetObject(arg.Result.Person);
                        }

                        db.SubmitChanges();
                    }

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        ProgressBarPopup.Instance.Close();
                        PersonSourceState = SourceState.Done;

                        var src = PersonSource;
                        if (src == null || src.Id < target.Id)
                        {
                            PersonSource = target;
                        }
                        else if (src.Id == target.Id)
                        {
                            if (src.Name != target.Name)
                            {
                                src.SendPropertyChanged("Name");
                            }
                            if (src.JobTitle != target.JobTitle)
                            {
                                src.SendPropertyChanged("JobTitle");
                            }
                            if (src.Words != target.Words)
                            {
                                src.SendPropertyChanged("Words");
                            }
                        }
                    });
                };

            client.ExecuteFailed += (obj, arg) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        PersonSourceState = SourceState.NotSet;
                        ProgressBarPopup.Instance.Close();
                    });
                };

            ProgressBarPopup.Instance.Open();
            PersonSourceState = SourceState.Setting;

            if (String.IsNullOrEmpty(uid))
            {
                client.Execute(req);
            }
            else
            {
                client.Execute(req, Global.Instance.Session, uid);
            }
        }

        #endregion

        #region [Campus Info]

        private void RefreshSchoolNews(int pageId = 0)
        {
            var req = new SchoolNewsGetListRequest<SchoolNewsGetListResponse>();
            var client = new WTDefaultClient<SchoolNewsGetListResponse>();

            if (pageId > 0)
                req.SetAdditionalParameter(WTDefaultClient<SchoolNewsGetListResponse>.PAGE, pageId);

            #region [Subscribe event handlers]

            client.ExecuteCompleted += (o, e) =>
            {
                int count = e.Result.SchoolNews.Count();
                SchoolNewsExt latest = null;
                SchoolNewsExt[] arr = new SchoolNewsExt[count];

                for (int i = 0; i < count; ++i)
                {
                    var item = e.Result.SchoolNews[i];

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        SchoolNewsExt itemInDB = db.SchoolNewsTable.Where((news) => news.Id == item.Id).SingleOrDefault();

                        //...the item is not saved
                        if (itemInDB == null)
                        {
                            itemInDB = new SchoolNewsExt();
                            itemInDB.SetObject(item);
                            db.SchoolNewsTable.InsertOnSubmit(itemInDB);
                        }
                        //...update info if the item is kept in database
                        else
                        {
                            itemInDB.SetObject(item);
                        }

                        arr[i] = itemInDB;

                        db.SubmitChanges();
                    }
                }

                var srcs = arr.Where((news) => !String.IsNullOrEmpty(news.ImageExtList));
                if (srcs != null && srcs.Count() > 0)
                {
                    latest = srcs.OrderByDescending((news) => news.CreatedAt).First();
                }

                if (latest != null)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        if (TongjiNewsSource == null || TongjiNewsSource.CreatedAt < latest.CreatedAt)
                            TongjiNewsSource = latest;
                        TongjiNewsSourceState = SourceState.Done;
                    });
                }
                else if (e.Result.NextPager > 0)
                {
                    RefreshSchoolNews(e.Result.NextPager);
                }
            };

            client.ExecuteFailed += (o, e) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    TongjiNewsSourceState = SourceState.NotSet;
                });
            };

            #endregion

            TongjiNewsSourceState = SourceState.Setting;
            client.Execute(req);
        }

        private void RefreshAroundNews(int pageId = 0)
        {
            var req = new AroundsGetRequest<AroundsGetResponse>();
            var client = new WTDefaultClient<AroundsGetResponse>();

            if (pageId > 0)
                req.SetAdditionalParameter(WTDefaultClient<AroundsGetResponse>.PAGE, pageId);

            #region [Subscribe event handlers]

            client.ExecuteCompleted += (o, e) =>
            {
                int count = e.Result.Arounds.Count();
                AroundExt latest = null;
                AroundExt[] arr = new AroundExt[count];


                for (int i = 0; i < count; ++i)
                {
                    var item = e.Result.Arounds[i];

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        AroundExt itemInDB = db.AroundTable.Where((news) => news.Id == item.Id).SingleOrDefault();

                        //...the item is not saved
                        if (itemInDB == null)
                        {
                            itemInDB = new AroundExt();
                            itemInDB.SetObject(item);
                            db.AroundTable.InsertOnSubmit(itemInDB);
                        }
                        //...update info if the item is kept in database
                        else
                        {
                            itemInDB.SetObject(item);
                        }

                        arr[i] = itemInDB;

                        db.SubmitChanges();
                    }
                }

                var srcs = arr.Where((news) => !String.IsNullOrEmpty(news.TitleImage));
                if (srcs != null && srcs.Count() > 0)
                {
                    try
                    {
                        latest = srcs.OrderByDescending((news) => news.CreatedAt).First();
                    }
                    catch { }
                }

                if (latest != null)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        if (AroundNewsSource == null || AroundNewsSource.CreatedAt < latest.CreatedAt)
                            AroundNewsSource = latest;

                        AroundNewsSourceState = SourceState.Done;
                    });
                }
                else
                {
                    RefreshAroundNews(e.Result.NextPager);
                }
            };

            client.ExecuteFailed += (o, e) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    AroundNewsSourceState = SourceState.NotSet;
                });
            };

            #endregion

            AroundNewsSourceState = SourceState.Setting;
            client.Execute(req);
        }

        private void RefreshOfficialNotes(int pageId = 0)
        {
            var req = new ForStaffsGetRequest<ForStaffsGetResponse>();
            var client = new WTDefaultClient<ForStaffsGetResponse>();

            if (pageId > 0)
                req.SetAdditionalParameter(WTDefaultClient<ForStaffsGetResponse>.PAGE, pageId);

            #region [Subscribe event handlers]

            client.ExecuteCompleted += (o, e) =>
            {
                int count = e.Result.ForStaffs.Count();
                ForStaffExt target = null;
                ForStaffExt[] arr = new ForStaffExt[count];

                for (int i = 0; i < count; ++i)
                {
                    var item = e.Result.ForStaffs[i];

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        ForStaffExt itemInDB = db.ForStaffTable.Where((news) => news.Id == item.Id).SingleOrDefault();

                        //...the item is not saved
                        if (itemInDB == null)
                        {
                            itemInDB = new ForStaffExt();
                            itemInDB.SetObject(item);
                            db.ForStaffTable.InsertOnSubmit(itemInDB);
                        }
                        //...update info if the item is kept in database
                        else
                        {
                            itemInDB.SetObject(item);
                        }
                        db.SubmitChanges();

                        arr[i] = itemInDB;
                    }
                }

                var srcs = arr.Where((news) => !String.IsNullOrEmpty(news.ImageExtList));
                if (srcs != null && srcs.Count() > 0)
                    target = srcs.OrderByDescending((news) => news.CreatedAt).First();

                if (target != null)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        if (OfficialNoteSource == null || OfficialNoteSource.CreatedAt < target.CreatedAt)
                            OfficialNoteSource = target;

                        OfficialNoteSourceState = SourceState.Done;
                    });
                }
                else
                {
                    RefreshOfficialNotes(e.Result.NextPager);
                }
            };

            client.ExecuteFailed += (o, e) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    OfficialNoteSourceState = SourceState.NotSet;
                });
            };

            #endregion

            OfficialNoteSourceState = SourceState.Setting;
            client.Execute(req);
        }

        private void RefreshClubNews(int pageId = 0)
        {
            var req = new ClubNewsGetListRequest<ClubNewsGetListResponse>();
            var client = new WTDefaultClient<ClubNewsGetListResponse>();

            if (pageId > 0)
                req.SetAdditionalParameter(WTDefaultClient<ClubNewsGetListResponse>.PAGE, pageId);

            #region [Subscribe event handlers]

            client.ExecuteCompleted += (o, e) =>
            {
                int count = e.Result.ClubNews.Count();
                ClubNewsExt latest = null;
                ClubNewsExt[] arr = new ClubNewsExt[count];

                for (int i = 0; i < count; ++i)
                {
                    var item = e.Result.ClubNews[i];

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        ClubNewsExt itemInDB = db.ClubNewsTable.Where((news) => news.Id == item.Id).SingleOrDefault();

                        //...the item is not saved
                        if (itemInDB == null)
                        {
                            itemInDB = new ClubNewsExt();
                            itemInDB.SetObject(item);
                            db.ClubNewsTable.InsertOnSubmit(itemInDB);
                        }
                        //...update info if the item is kept in database
                        else
                        {
                            itemInDB.SetObject(item);
                        }

                        arr[i] = itemInDB;

                        db.SubmitChanges();
                    }
                }

                var srcs = arr.Where((news) => !String.IsNullOrEmpty(news.ImageExtList));
                if (srcs != null && srcs.Count() > 0)
                {
                    latest = srcs.OrderByDescending((news) => news.CreatedAt).First();
                }

                if (latest != null)
                {
                    this.Dispatcher.BeginInvoke(() =>
                        {
                            if (ClubNewsSource == null || ClubNewsSource.CreatedAt < latest.CreatedAt)
                                ClubNewsSource = latest;

                            ClubNewsSourceState = SourceState.Done;
                        });
                }
                else
                {
                    RefreshClubNews(e.Result.NextPager);
                }
            };

            client.ExecuteFailed += (o, e) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ClubNewsSourceState = SourceState.NotSet;
                });
            };

            #endregion

            ClubNewsSourceState = SourceState.Setting;
            client.Execute(req);
        }

        #endregion

        #endregion

        #endregion

        #region [Visual]

        private void Button_CancelAutoLogin_Click(Object sender, RoutedEventArgs e)
        {
            Grid_AutoLoginPrompt.Visibility = Visibility.Collapsed;
            ProgressBarPopup.Instance.Close();
        }

        private void PasswordBox_Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateLoginButton(null, null);
        }

        private void SignInControl_GotFocus(Object sender, RoutedEventArgs e)
        {
            ScrollViewer_SignOutRoot.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        }

        private void SignInControl_LostFocus(Object sender, RoutedEventArgs e)
        {
            ScrollViewer_SignOutRoot.ScrollToVerticalOffset(0);
            ScrollViewer_SignOutRoot.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        private void UpdateLoginButton(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(PasswordBox_Password.Password) && !String.IsNullOrEmpty(TextBox_Id.Text))
                {
                    Button_Login.IsEnabled = true;
                }
                else
                {
                    Button_Login.IsEnabled = false;
                }
            }
            catch { }
        }

        private void Panorama_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            var p = sender as Panorama;

            ProgressBarPopup.Instance.Close();

            #region [App bar]
            if (p.SelectedIndex == 0 && Border_SignedOut.Visibility == Visibility.Visible)
            {
                (this.ApplicationBar as ApplicationBar).Buttons.Clear();

                ApplicationBarIconButton button;
                button = new ApplicationBarIconButton(new Uri("/icons/appbar.check.rest.png", UriKind.RelativeOrAbsolute))
                {
                    Text = StringLibrary.MainPage_AppBarLoginText,
                    IsEnabled = false
                };
                UpdateLoginButton(null, null);
                button.Click += LogOn_Click;
                (this.ApplicationBar as ApplicationBar).Buttons.Add(button);

                button = new ApplicationBarIconButton(new Uri("/icons/appbar.register.rest.png", UriKind.RelativeOrAbsolute))
                {
                    Text = StringLibrary.MainPage_AppBarRegisterText
                };
                button.Click += NavigateToSignUp;
                (this.ApplicationBar as ApplicationBar).Buttons.Add(button);
            }
            else
            {
                if ((this.ApplicationBar as ApplicationBar).Buttons.Count != 1)
                {
                    (this.ApplicationBar as ApplicationBar).Buttons.Clear();

                    ApplicationBarIconButton button;
                    button = new ApplicationBarIconButton(new Uri("/icons/appbar.refresh.rest.png", UriKind.RelativeOrAbsolute))
                    {
                        Text = StringLibrary.MainPage_AppBarRefreshText
                    };
                    button.Click += RefreshButton_Click;
                    (this.ApplicationBar as ApplicationBar).Buttons.Add(button);
                }
            }

            #region [People of Week]

            if (e.AddedItems.Contains(Border_PeopleOfWeek))
            {
                this.ApplicationBar.Buttons.Clear();

                LoadPersonAppBarButtons();
            }

            #endregion

            if (e.AddedItems.Contains(Border_Activities))
            {
                Queue<Object> q = new Queue<Object>();

                foreach (var item in this.ApplicationBar.MenuItems)
                {
                    q.Enqueue(item);
                }

                this.ApplicationBar.MenuItems.Clear();

                ApplicationBarMenuItem mi;

                mi = new ApplicationBarMenuItem()
                {
                    Text = StringLibrary.MainPage_AppBarRecentEvents
                };
                mi.Click += SortActivitiesCompareToNow;
                this.ApplicationBar.MenuItems.Insert(0, mi);

                mi = new ApplicationBarMenuItem()
                {
                    Text = StringLibrary.MainPage_AppBarHotEvents
                };
                mi.Click += SortActivitiesByScheduleNumber;
                this.ApplicationBar.MenuItems.Insert(0, mi);


                mi = new ApplicationBarMenuItem()
                {
                    Text = StringLibrary.MainPage_AppBarLatestEvents
                };
                mi.Click += SortActivitiesByCreationTime;
                this.ApplicationBar.MenuItems.Insert(0, mi);

                while (q.Count > 0)
                {
                    this.ApplicationBar.MenuItems.Add(q.Dequeue());
                }
            }
            else if (e.RemovedItems.Contains(Border_Activities))
            {
                for (int i = 0; i < 3; ++i)
                {
                    this.ApplicationBar.MenuItems.RemoveAt(0);
                }
            }
            #endregion

            #region [Refresh for the first time or Call AutoRefresh]

            #region [Activities]

            if (p.SelectedIndex == 1)
            {
                if (Global.Instance.ActivityPageId == -1 && Global.Instance.CurrentActivitySourceState == SourceState.NotSet)
                {
                    RefreshExistingExpiredActivities();
                }
            }

            #endregion

            #region [Campus Info]

            else if (p.SelectedIndex == 2)
            {
                if (TongjiNewsSourceState == SourceState.NotSet)
                    RefreshSchoolNews();
                if (AroundNewsSourceState == SourceState.NotSet)
                    RefreshAroundNews();
                if (ClubNewsSourceState == SourceState.NotSet)
                    RefreshClubNews();
                if (OfficialNoteSourceState == SourceState.NotSet)
                    RefreshOfficialNotes();
            }

            #endregion

            #region [People of Week]

            else if (p.SelectedIndex == 3)
            {
                if (PersonSourceState == SourceState.NotSet)
                    GetLatestPerson();
            }

            #endregion

            #endregion
        }

        private void SendFeedback(Object sender, EventArgs e)
        {
            var task = new Microsoft.Phone.Tasks.EmailComposeTask();
            var version = AppVersion.Current;

            task.Body = String.Format(StringLibrary.MainPage_UserFeedBackEmailBodyTemplate, Microsoft.Phone.Info.DeviceStatus.DeviceManufacturer, Microsoft.Phone.Info.DeviceStatus.DeviceName, version);
            task.Subject = String.Format(StringLibrary.MainPage_UserFeedBackEmailSubjectTemplate, version);
            task.To = "we@tongji.edu.cn";
            task.Show();
        }

        private void ShareToFriends(Object sender, EventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickAppBarShareByEmailMenuItem).ToString());
#endif
            #endregion

            var task = new Microsoft.Phone.Tasks.EmailComposeTask();

            task.Body = String.Format(StringLibrary.MainPage_ShareToFriendsEmailBody);
            task.Subject = String.Format(StringLibrary.MainPage_ShareToFriendsEmailSubject);
            task.Show();
        }

        private void RefreshButton_Click(Object sender, EventArgs e)
        {
            if (Panorama_Core.SelectedIndex == 0)
            {
                #region [Flurry]
#if Flurry
                FlurryWP8SDK.Api.LogEvent(
                    ((int)FlurryWP8SDK.Models.EventName.ClickAppbarRefreshButton).ToString(),
                    new List<FlurryWP8SDK.Models.Parameter>(new FlurryWP8SDK.Models.Parameter[]
                            {
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.ClickAppBarRefreshButtonParameter).ToString(), 
                                    ((int)FlurryWP8SDK.Models.ParameterValue.Feeds).ToString())
                            })
                    );
#endif
                #endregion

                var src = UserSource;
                if (src != null)
                {
                    UpdateFavoriteNumber(false);
                    src.SendPropertyChanged("AvatarImageBrush");
                }
                if (Global.Instance.CurrentAgendaSourceState == SourceState.Done)
                {
                    var node = Global.Instance.AgendaSource.GetNextCalendarNode();
                    AlarmClockSource = node;
                }

                if (DateTime.Now.Day.ToString() != TextBlock_Today.Text)
                    PlayTriggerTodayAnimation();
                else
                {
                    (this.Resources["StressCurrentDayAnimation"] as Storyboard).Begin();
                }
            }
            else if (Panorama_Core.SelectedIndex == 1)
            {
                #region [Flurry]
#if Flurry
                FlurryWP8SDK.Api.LogEvent(
                    ((int)FlurryWP8SDK.Models.EventName.ClickAppbarRefreshButton).ToString(),
                    new List<FlurryWP8SDK.Models.Parameter>(new FlurryWP8SDK.Models.Parameter[]
                            {
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.ClickAppBarRefreshButtonParameter).ToString(), 
                                    ((int)FlurryWP8SDK.Models.ParameterValue.Activities).ToString())
                            })
                    );
#endif
                #endregion

                GetAllUnexpiredActivities();
            }
            else if (Panorama_Core.SelectedIndex == 2)
            {
                #region [Flurry]
#if Flurry
                FlurryWP8SDK.Api.LogEvent(
                    ((int)FlurryWP8SDK.Models.EventName.ClickAppbarRefreshButton).ToString(),
                    new List<FlurryWP8SDK.Models.Parameter>(new FlurryWP8SDK.Models.Parameter[]
                            {
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.ClickAppBarRefreshButtonParameter).ToString(), 
                                    ((int)FlurryWP8SDK.Models.ParameterValue.CampusNews).ToString())
                            })
                    );
#endif
                #endregion

                //...Reload Image
                {
                    OfficialNoteSource = OfficialNoteSource;
                }
                {
                    ClubNewsSource = ClubNewsSource;
                }
                {
                    TongjiNewsSource = TongjiNewsSource;
                }
                {
                    AroundNewsSource = AroundNewsSource;
                }

                //...Get latest
                RefreshOfficialNotes();
                RefreshClubNews();
                RefreshSchoolNews();
                RefreshAroundNews();
            }
            else if (Panorama_Core.SelectedIndex == 3)
            {
                #region [Flurry]
#if Flurry
                FlurryWP8SDK.Api.LogEvent(
                    ((int)FlurryWP8SDK.Models.EventName.ClickAppbarRefreshButton).ToString(),
                    new List<FlurryWP8SDK.Models.Parameter>(new FlurryWP8SDK.Models.Parameter[]
                            {
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.ClickAppBarRefreshButtonParameter).ToString(), 
                                    ((int)FlurryWP8SDK.Models.ParameterValue.WeeklyStar).ToString())
                            })
                    );
#endif
                #endregion

                PersonSource = PersonSource;

                //...Get latest
                GetLatestPerson();
            }
        }

        private void SignOut(Object sender, EventArgs e)
        {
            var result = MessageBox.Show(StringLibrary.MainPage_ConfirmSignOutPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OKCancel);
            if (MessageBoxResult.OK == result)
            {
                var req = new UserLogOffRequest<WTResponse>();
                var client = new WTDefaultClient<WTResponse>();

                Action a = () =>
                {
                    Global.Instance.CurrentUserID = String.Empty;
                    Global.Instance.CleanSettings();

                    #region [Flurry]
#if Flurry
                    #region [Flurry]

                    FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickSignOutMenuItem).ToString());

                    #endregion

                    FlurryWP8SDK.Api.SetUserId(String.Empty);
                    FlurryWP8SDK.Api.SetGender(FlurryWP8SDK.Models.Gender.Unknown);
                    FlurryWP8SDK.Api.SetAge(0);
                    if (!CurrentLocation.IsUnknown)
                        FlurryWP8SDK.Api.SetLocation(CurrentLocation.Latitude, CurrentLocation.Longitude, Convert.ToSingle(CurrentLocation.HorizontalAccuracy));
#endif
                    #endregion

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        #region [App bar]

                        this.ApplicationBar.MenuItems.RemoveAt(this.ApplicationBar.MenuItems.Count - 1);

                        if (Panorama_Core.SelectedIndex == 0)
                        {
                            this.ApplicationBar.Buttons.Clear();
                            ApplicationBarIconButton button;
                            button = new ApplicationBarIconButton(new Uri("/icons/appbar.check.rest.png", UriKind.RelativeOrAbsolute))
                            {
                                Text = StringLibrary.MainPage_AppBarLoginText,
                                IsEnabled = false
                            };
                            UpdateLoginButton(null, null);
                            button.Click += LogOn_Click;
                            (this.ApplicationBar as ApplicationBar).Buttons.Add(button);

                            button = new ApplicationBarIconButton(new Uri("/icons/appbar.register.rest.png", UriKind.RelativeOrAbsolute))
                            {
                                Text = StringLibrary.MainPage_AppBarRegisterText
                            };
                            button.Click += NavigateToSignUp;
                            (this.ApplicationBar as ApplicationBar).Buttons.Add(button);
                        }

                        #endregion

                        #region [Switch to Border_SignOut]

                        UserSource = null;
                        AlarmClockSource = null;
                        Border_SignedOut.Visibility = Visibility.Visible;

                        #endregion

                        //...Clear global data
                        Global.Instance.CleanAgendaSource();
                    });
                };

                client.ExecuteCompleted += (obj, args) => { a.Invoke(); };
                client.ExecuteFailed += (obj, args) => { a.Invoke(); };

                client.Execute(req, Global.Instance.Session, Global.Instance.Settings.UID);
            }
        }

        private void LoadPersonAppBarButtons()
        {
            this.ApplicationBar.Buttons.Clear();

            var src = PersonSource;

            ApplicationBarIconButton button;

            button = new ApplicationBarIconButton(new Uri("/icons/appbar.person.list.rest.png", UriKind.RelativeOrAbsolute))
            {
                Text = StringLibrary.MainPage_AppBarViewAllPeopleOfWeek
            };
            button.Click += NavToPeopleOfWeekList;
            this.ApplicationBar.Buttons.Add(button);

            button = new ApplicationBarIconButton(new Uri("/icons/appbar.refresh.rest.png", UriKind.RelativeOrAbsolute))
            {
                Text = StringLibrary.MainPage_AppBarRefreshText
            };
            button.Click += RefreshButton_Click;
            this.ApplicationBar.Buttons.Add(button);
        }

        private void ListBox_Activity_MouseMove(Object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (this.activitySortMethod != ActivitySortMethod.kByCreationTime)
                return;

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
                if (!isLoadingMoreActivities && Math.Abs(sv.ScrollableHeight - sv.VerticalOffset) <= 0.1)
                {
                    var src = ActivityListSource;
                    if (src != null && src.Count > 0)
                    {
                        if (src.Last().IsValid)
                        {
                            var thread = new Thread(new ParameterizedThreadStart(LoadMoreActivities));

                            thread.Start(src.Count);
                        }
                    }
                }
            }
        }

        private void LoadMoreActivities(Object param)
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
                isLoadingMoreActivities = true;
            });

            var count = (int)param;
            ActivityExt[] arr = null;

            using (var db = WTShareDataContext.ShareDB)
            {
                arr = db.Activities.ToArray();
            }

            if (arr != null && arr.Count() > count)
            {
                var src = arr.OrderByDescending((activity) => activity.CreatedAt).Skip(count);

                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                });

                if (src.Count() > 10)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        isLoadingMoreActivities = false;

                        if (activitySortMethod == ActivitySortMethod.kByCreationTime)
                        {
                            var activitySource = ActivityListSource;
                            bool hasMore = false;
                            if (activitySource != null && !activitySource.Last().IsValid)
                                hasMore = true;

                            UpdateAcitivityList(src.Take(10), false, hasMore);
                        }
                    });
                }
                else
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        isLoadingMoreActivities = false;

                        if (activitySortMethod == ActivitySortMethod.kByCreationTime)
                        {
                            var activitySource = ActivityListSource;
                            bool hasMore = false;
                            if (activitySource != null && !activitySource.Last().IsValid)
                                hasMore = true;

                            UpdateAcitivityList(src, false, hasMore);
                        }
                    });
                }
            }
            else
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    ProgressBarPopup.Instance.Close();
                    isLoadingMoreActivities = false;
                });
            }
        }

        private void UpdateFavoriteNumber(bool setForTheFirstTime)
        {
            UserExt src = UserSource;
            if (String.IsNullOrEmpty(Global.Instance.CurrentUserID) || Border_SignedIn.Visibility == Visibility.Collapsed || src == null)
            {
                return;
            }

            if (setForTheFirstTime)
            {
                src.SendPropertyChanged("DisplayFavoritesCount");
                (this.Resources["VisibleFavoriteNumber"] as Storyboard).Begin();
            }
            else
            {
                EventHandler handler = null;
                handler = (o, e) =>
                     {
                         src.SendPropertyChanged("DisplayFavoritesCount");
                         (this.Resources["TurnDownFavoriteNumber"] as Storyboard).Begin();
                         (this.Resources["TurnUpFavoriteNumber"] as Storyboard).Completed -= handler;
                     };

                (this.Resources["TurnUpFavoriteNumber"] as Storyboard).Completed += handler;
                (this.Resources["TurnUpFavoriteNumber"] as Storyboard).Begin();
            }
        }

        private void PeopleOfWeekLargeImage_MouseLeftButtonDown(Object sender, MouseButtonEventArgs e)
        {
            var img = sender as Image;
            var pnt = e.GetPosition(img);

            //...Tap left
            if (pnt.X < img.RenderSize.Width * 0.4f)
            {
                (img.Resources["TapLeft"] as Storyboard).Begin();
            }
            //...Tap right
            else if (pnt.X > img.RenderSize.Width * 0.6f)
            {
                (img.Resources["TapRight"] as Storyboard).Begin();
            }
            //...Tap top
            else if (pnt.Y < img.RenderSize.Height * 0.4f)
            {
                (img.Resources["TapTop"] as Storyboard).Begin();
            }
            //...Tap bottom
            else if (pnt.Y > img.RenderSize.Height * 0.6f)
            {
                (img.Resources["TapBottom"] as Storyboard).Begin();
            }
            //...Tap center
            else
            {
                (img.Resources["TapCenter"] as Storyboard).Begin();
            }
        }

        private void PeopleOfWeekLargeImage_MouseLeftButtonUp(Object sender, MouseButtonEventArgs e)
        {
            (sender as UIElement).Projection = new PlaneProjection();
        }

        private void PeopleOfWeekLargeImage_MouseLeave(Object sender, MouseEventArgs e)
        {
            (sender as UIElement).Projection = new PlaneProjection();
        }

        private void PeopleOfWeekLargeImage_MouseEnter(Object sender, MouseEventArgs e)
        {
            var img = sender as Image;
            var pnt = e.GetPosition(img);

            //...Tap left
            if (pnt.X < img.RenderSize.Width * 0.25f)
            {
                (img.Resources["TapLeft"] as Storyboard).Begin();
            }
            //...Tap right
            else if (pnt.X > img.RenderSize.Width * 0.75f)
            {
                (img.Resources["TapRight"] as Storyboard).Begin();
            }
            //...Tap top
            else if (pnt.Y < img.RenderSize.Height * 0.25f)
            {
                (img.Resources["TapTop"] as Storyboard).Begin();
            }
            //...Tap bottom
            else if (pnt.Y > img.RenderSize.Height * 0.75f)
            {
                (img.Resources["TapBottom"] as Storyboard).Begin();
            }
            //...Tap center
            else
            {
                (img.Resources["TapCenter"] as Storyboard).Begin();
            }
        }

        private void OnPeopleOfWeekLargeImageTap(Object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            var src = PersonSource;

            if (src != null)
            {
                #region [Flurry]
#if Flurry
                FlurryWP8SDK.Api.LogEvent(
                    ((int)FlurryWP8SDK.Models.EventName.ViewWeeklyStar).ToString(),
                    new List<FlurryWP8SDK.Models.Parameter>(new FlurryWP8SDK.Models.Parameter[]
                            {
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.TapToViewWeeklyStarParameter).ToString(), 
                                    ((int)FlurryWP8SDK.Models.ParameterValue.Picture).ToString())
                            })
                    );
#endif
                #endregion

                this.NavigationService.Navigate(new Uri(String.Format("/Pages/PeopleOfWeek.xaml?q={0}", src.Id), UriKind.RelativeOrAbsolute));
            }
        }

        /// <summary>
        /// Start playing LoadingCalendarNodeAnimation
        /// </summary>
        /// <remarks>
        /// This function is thread-safe.
        /// </remarks>
        private void StartComputingCalendarNodes()
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                Global.Instance.StartSettingAgendaSource();
                Grid_LoadingCalendarNode.Visibility = Visibility.Visible;
                var sb = (this.Resources["LoadingCalendarNodeAnimation"] as Storyboard);
                sb.Seek(TimeSpan.FromMilliseconds(0));
                sb.Begin();
            });
        }

        /// <summary>
        /// Stop playing LoadingCalendarNodeAnimation
        /// </summary>
        /// <remarks>
        /// This function is thread-safe.
        /// </remarks>
        private void StopComputingCalendarNodes()
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                Grid_LoadingCalendarNode.Visibility = Visibility.Collapsed;
                var sb = (this.Resources["LoadingCalendarNodeAnimation"] as Storyboard);
                sb.Stop();
            });
        }

        #region [Activity]

        private void ReloadActivities()
        {
            Thread thread = null;

            switch (this.activitySortMethod)
            {
                case ActivitySortMethod.kByScheduleNumber:
                    thread = new Thread(new ThreadStart(SortActivitiesByScheduleNumberCore))
                    {
                        IsBackground = true,
                        Name = "SortActivitiesByScheduleNumberCore"
                    };
                    break;
                case ActivitySortMethod.kByCreationTime:
                    thread = new Thread(new ThreadStart(SortActivitiesByCreationTimeCore))
                    {
                        IsBackground = true,
                        Name = "SortActivitiesByCreationTimeCore"
                    };
                    break;
                case ActivitySortMethod.kCompareToNow:
                    thread = new Thread(new ThreadStart(SortActivitiesCompareToNowCore))
                    {
                        IsBackground = true,
                        Name = "SortActivitiesCompareToNowCore"
                    };
                    break;
            }

            thread.Start();
        }

        private void ResetListBoxActivityVerticalOffset()
        {
            var obj = ListBox_Activity as DependencyObject;
            try
            {
                while (obj != null)
                {
                    if (obj is ScrollViewer)
                    {
                        (obj as ScrollViewer).ScrollToVerticalOffset(0);
                        break;
                    }
                    else
                    {
                        obj = VisualTreeHelper.GetChild(obj, 0);
                    }
                }
            }
            catch { }
        }

        private void Button_LoadMoreActivities_Click(Object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            btn.IsHitTestVisible = false;
            btn.Content = StringLibrary.MainPage_LoadingMoreEvents;

            var thread = new Thread(new ThreadStart(LoadAnotherExpiredActivities))
            {
                IsBackground = true,
                Name = "Button_LoadMoreActivities_Click"
            };

            thread.Start();
        }

        /// <summary>
        /// Insert activities to or update activities in ListBox_Activity's ItemsSource
        /// </summary>
        /// <param name="activities">a range of activities to be insert or update in UI</param>
        /// <param name="willContinue">refresh will continue</param>
        /// <param name="hasMore">"Load more" should be insert to the end of the List</param>
        private void UpdateAcitivityList(IEnumerable<ActivityExt> activities, Boolean willContinue, Boolean hasMore)
        {
            if (this.NavigationService.CurrentSource.ToString() != "/MainPage.xaml")
            {
                return;
            }

            var src = ActivityListSource;
            if (src == null || src.Count == 0)
            {
                ActivityListSource = new ObservableCollection<ActivityExt>(activities);

                if (!willContinue && hasMore)
                {
                    ActivityListSource.Add(ActivityExt.InvalidActivityExt);
                    this.hasLoadMoreActivities = true;
                }
                else
                {
                    this.hasLoadMoreActivities = false;
                }

                ListBox_Activity.Visibility = Visibility.Visible;
            }
            else
            {
                //...Remove "add more"
                if (!src.Last().IsValid)
                    src.RemoveAt(src.Count - 1);

                #region [Update or Insert activities]
                foreach (var a in activities)
                {
                    ActivityExt target = src.Where((item) => item.Id == a.Id).SingleOrDefault();
                    if (target != null)
                    {
                        if (target.Schedule != a.Schedule)
                        {
                            target.Schedule = a.Schedule;
                            target.SendPropertyChanged("Schedule");
                        }
                        if (target.Title != a.Title)
                        {
                            target.Title = a.Title;
                            target.SendPropertyChanged("Title");
                        }
                        if (target.Location != a.Location)
                        {
                            target.Location = a.Location;
                            target.SendPropertyChanged("Location");
                        }
                        if (target.Begin != a.Begin || target.End != a.End)
                        {
                            target.Begin = a.Begin;
                            target.End = a.End;
                            target.SendPropertyChanged("DisplayTime");
                        }
                    }
                    else
                    {
                        switch (this.activitySortMethod)
                        {
                            case ActivitySortMethod.kByCreationTime:
                                {
                                    var count = src.Where((item) => item.CreatedAt > a.CreatedAt).Count();
                                    src.Insert(count, a);
                                }
                                break;
                            case ActivitySortMethod.kByScheduleNumber:
                                {
                                    if (a.Begin > DateTime.Now)
                                    {
                                        var count = src.Where((item) => item.Schedule > a.Schedule).Count();
                                        src.Insert(count, a);
                                    }
                                }
                                break;
                            case ActivitySortMethod.kCompareToNow:
                                {
                                    if (a.Begin > DateTime.Now)
                                    {
                                        var count = src.Where((item) => item.Begin < a.Begin).Count();
                                        src.Insert(count, a);
                                    }
                                }
                                break;
                        }
                    }
                }
                #endregion

                if (!willContinue && hasMore)
                {
                    src.Add(ActivityExt.InvalidActivityExt);
                    this.hasLoadMoreActivities = true;
                }
                else
                {
                    this.hasLoadMoreActivities = false;
                }
            }

            ListBox_Activity.UpdateLayout();
        }

        /// <summary>
        /// 最新, do not filter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortActivitiesByCreationTime(Object sender, EventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickTheLatestMenuItem).ToString());
#endif
            #endregion

            if (this.activitySortMethod != ActivitySortMethod.kByCreationTime)
            {
                var thread = new Thread(new ThreadStart(SortActivitiesByCreationTimeCore))
                {
                    IsBackground = true,
                    Name = "SortActivitiesByCreationTimeCore"
                };

                thread.Start();
            }
            else
            {
                ResetListBoxActivityVerticalOffset();
            }
        }

        private void SortActivitiesByCreationTimeCore()
        {
            ActivityExt[] activities = null;
            ObservableCollection<ActivityExt> result = null;

            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
            });

            using (var db = WTShareDataContext.ShareDB)
            {
                activities = db.Activities.ToArray();
            }

            if (null != activities)
            {
                var q = from ActivityExt a in activities
                        orderby a.CreatedAt descending
                        select a;
                result = new ObservableCollection<ActivityExt>(q);

                this.Dispatcher.BeginInvoke(() =>
                {
                    this.activitySortMethod = ActivitySortMethod.kByCreationTime;
                    if (this.hasLoadMoreActivities)
                        result.Add(ActivityExt.InvalidActivityExt);

                    ActivityListSource = result;
                    ResetListBoxActivityVerticalOffset();
                    ProgressBarPopup.Instance.Close();
                });
            }
        }

        /// <summary>
        /// 最火,filter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortActivitiesByScheduleNumber(Object sender, EventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickTheHottestMenuItem).ToString());
#endif
            #endregion

            if (this.activitySortMethod != ActivitySortMethod.kByScheduleNumber)
            {
                var thread = new Thread(new ThreadStart(SortActivitiesByScheduleNumberCore))
                {
                    IsBackground = true,
                    Name = "SortActivitiesByLikeNumberCore"
                };

                thread.Start();
            }
            else
            {
                ResetListBoxActivityVerticalOffset();
            }
        }

        private void SortActivitiesByScheduleNumberCore()
        {
            ActivityExt[] activities = null;
            ObservableCollection<ActivityExt> result = null;

            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
            });

            using (var db = WTShareDataContext.ShareDB)
            {
                activities = db.Activities.ToArray();
            }

            if (null != activities)
            {
                var q = activities.Where((a) => a.Begin > DateTime.Now);

                if (q.Count() == 0)
                {
                    q = activities.OrderByDescending((a) => a.Schedule).ThenByDescending((a) => a.CreatedAt);
                }
                else
                {
                    q = q.OrderByDescending((a) => a.Schedule).ThenByDescending((a) => a.CreatedAt);
                }

                result = new ObservableCollection<ActivityExt>(q);

                this.Dispatcher.BeginInvoke(() =>
                {
                    this.activitySortMethod = ActivitySortMethod.kByScheduleNumber;
                    ActivityListSource = result;
                    ResetListBoxActivityVerticalOffset();
                    ProgressBarPopup.Instance.Close();
                });
            }
        }

        /// <summary>
        /// 最近,filter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortActivitiesCompareToNow(Object sender, EventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickTheMostRecentMenuItem).ToString());
#endif
            #endregion

            if (this.activitySortMethod != ActivitySortMethod.kCompareToNow)
            {
                var thread = new Thread(new ThreadStart(SortActivitiesCompareToNowCore))
                {
                    IsBackground = true,
                    Name = "SortActivitiesCompareToNowCore"
                };

                thread.Start();
            }
            else
            {
                ResetListBoxActivityVerticalOffset();
            }
        }

        private void SortActivitiesCompareToNowCore()
        {
            ActivityExt[] activities = null;
            ObservableCollection<ActivityExt> result = null;

            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
            });

            using (var db = WTShareDataContext.ShareDB)
            {
                activities = db.Activities.ToArray();
            }

            if (null != activities)
            {
                var q = activities.Where((a) => a.Begin > DateTime.Now);

                if (q.Count() == 0)
                {
                    q = activities.OrderByDescending((a) => a.Begin);
                    if (q.Count() > 5)
                    {
                        q = q.Take(5);
                    }
                }
                else
                {
                    q = q.OrderBy((a) => a.Begin);
                }

                result = new ObservableCollection<ActivityExt>(q);

                this.Dispatcher.BeginInvoke(() =>
                {
                    this.activitySortMethod = ActivitySortMethod.kCompareToNow;
                    ActivityListSource = result;
                    ResetListBoxActivityVerticalOffset();
                    ProgressBarPopup.Instance.Close();
                });
            }
        }

        #endregion

        private void PlayTriggerTodayAnimation()
        {
            TextBlock_Yesterday.Text = (DateTime.Now - TimeSpan.FromDays(1)).Day.ToString();
            TextBlock_Today.Text = DateTime.Now.Day.ToString();
            (this.Resources["TriggerCalendar"] as Storyboard).Begin();
        }

        #endregion

        #region [Data Operatings]

        private void LoadDataFromDatabase()
        {
            ActivityExt[] activityArr = null;
            ObservableCollection<ActivityExt> activitySrc = null;
            PersonExt personSrc = null;
            SchoolNewsExt snSrc = null;
            AroundExt anSrc = null;
            ForStaffExt fsSrc = null;
            ClubNewsExt cnSrc = null;

            Debug.WriteLine("Load data from db started.");

            #region [Auto Log in]

            AutoLogin();

            #endregion

            #region [Activity]
            using (var db = WTShareDataContext.ShareDB)
            {
                if (db.Activities.Count() > 0)
                {
                    activityArr = db.Activities.ToArray();
                }
            }

            if (activityArr != null)
            {
                IEnumerable<ActivityExt> src = activityArr.OrderByDescending((a) => a.CreatedAt);

                //...Only take the latest 5 news in database to accelerate refreshing ListBox_Activities process
                if (src.Count() > 5)
                    src = src.Take(5);
                activitySrc = new ObservableCollection<ActivityExt>(src);

                this.Dispatcher.BeginInvoke(() =>
                {
                    ActivityListSource = activitySrc;
                });
            }
            #endregion

            #region [PeopleOfWeek]
            PersonExt[] personArr = null;
            using (var db = WTShareDataContext.ShareDB)
            {
                personArr = db.People.ToArray();
            }

            if (null != personArr && personArr.Count() > 0)
            {
                personSrc = personArr.OrderBy((person) => person.Id).Last();
                this.Dispatcher.BeginInvoke(() =>
                {
                    PersonSource = personSrc;
                });
            }
            #endregion

            #region [Campus Info]

            //...Load Campus info order by the corresponding UI Buttons
            {
                #region [Official Note]
                {
                    ForStaffExt[] fs = null;
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        fs = db.ForStaffTable.ToArray();
                    }

                    if (fs != null && fs.Count() > 0)
                    {
                        var sources = fs.Where((news) => !String.IsNullOrEmpty(news.ImageExtList)).OrderBy((news) => news.CreatedAt);
                        if (sources.Count() > 0)
                        {
                            fsSrc = sources.Last();
                            this.Dispatcher.BeginInvoke(() =>
                            {
                                OfficialNoteSource = fsSrc;
                            });
                        }
                    }
                }
                #endregion

                #region [Club News]
                {
                    ClubNewsExt[] cn = null;
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        cn = db.ClubNewsTable.ToArray();
                    }

                    if (cn != null && cn.Count() > 0)
                    {
                        var sources = cn.Where((news) => !String.IsNullOrEmpty(news.ImageExtList)).OrderBy((news) => news.CreatedAt);
                        if (sources.Count() > 0)
                        {
                            cnSrc = sources.Last();
                            this.Dispatcher.BeginInvoke(() =>
                            {
                                ClubNewsSource = cnSrc;
                            });
                        }
                    }
                }
                #endregion

                #region [Tongji News]
                {
                    SchoolNewsExt[] sn = null;
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        sn = db.SchoolNewsTable.ToArray();
                    }

                    if (sn != null && sn.Count() > 0)
                    {
                        var sources = sn.Where((news) => !String.IsNullOrEmpty(news.ImageExtList)).OrderBy((news) => news.CreatedAt);
                        if (sources.Count() > 0)
                        {
                            snSrc = sources.Last();
                            this.Dispatcher.BeginInvoke(() =>
                            {
                                TongjiNewsSource = snSrc;
                            });
                        }
                    }
                }
                #endregion

                #region [Around News]
                {
                    AroundExt[] an = null;
                    using (var db = WTShareDataContext.ShareDB)
                    {
                        an = db.AroundTable.ToArray();
                    }


                    if (an != null && an.Count() > 0)
                    {
                        try
                        {
                            anSrc = (from AroundExt news in an
                                     where !String.IsNullOrEmpty(news.TitleImage) && !news.TitleImage.EndsWith("missing.png")
                                     orderby news.CreatedAt descending
                                     select news).First();
                        }
                        catch { }

                        this.Dispatcher.BeginInvoke(() =>
                        {
                            AroundNewsSource = anSrc;
                        });
                    }
                }
                #endregion
            }

            #endregion
        }

        /// <summary>
        /// Update corresponding semester properties and delete existing courses of the response semester
        /// if the response semester exists in the database, then inserting response courses.
        /// Otherwise, create a new semester and the semester's courses from the response and store them
        /// in database.
        /// </summary>
        /// <remarks>
        /// This function is called when timetable has been downloaded completely.
        /// </remarks>
        /// <param name="param">TimetableGetResponse</param>
        private void OnDownloadCoursesCompleted(Object param, String session, String uid)
        {
            var result = param as TimeTableGetResponse;

            if (result == null)
                throw new NotSupportedException("TimeTableGetResponse is expected");

            // target semester
            Semester semester = null;

            #region [Create or update semester in database]

            using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
            {
                semester = db.Semesters.Where((sem) => sem.SchoolYearStartAt == result.SchoolYearStartAt
                    && sem.SchoolYearWeekCount == result.SchoolYearWeekCount).SingleOrDefault();
            }

            if (semester == null)
            {
                //...Create and store a new semester
                semester = new Semester()
                {
                    Id = Guid.NewGuid().ToString(),
                    SchoolYearStartAt = result.SchoolYearStartAt,
                    SchoolYearWeekCount = result.SchoolYearWeekCount,
                    SchoolYearCourseWeekCount = result.SchoolYearCourseWeekCount
                };

                using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                {
                    db.Semesters.InsertOnSubmit(semester);

                    db.SubmitChanges();
                }
            }
            else
            {
                CourseExt[] existingCoursesOfCurrentSemester = null;

                using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                {
                    existingCoursesOfCurrentSemester = db.Courses.Where((c) => c.SemesterGuid == semester.Id).ToArray();
                }

                if (existingCoursesOfCurrentSemester != null)
                {
                    foreach (var c in existingCoursesOfCurrentSemester)
                        using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                        {
                            try
                            {
                                db.Courses.Attach(c);
                            }
                            catch { }

                            db.Courses.DeleteOnSubmit(c);
                            db.SubmitChanges();
                        }
                }
            }

            #endregion

            #region [Inserting courses]

            int courseCount = result.Courses.Count();
            CourseExt[] courseExtArr = null;

            if (courseCount > 0)
            {
                courseExtArr = new CourseExt[courseCount];
                for (int i = 0; i < courseCount; ++i)
                {
                    courseExtArr[i] = new CourseExt()
                    {
                        Id = Guid.NewGuid().ToString(),
                        UID = Global.Instance.Settings.UID,
                        SemesterGuid = semester.Id
                    };
                    courseExtArr[i].SetObject(result.Courses[i]);
                }

                using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                {
                    db.Courses.InsertAllOnSubmit(courseExtArr);
                    db.SubmitChanges();
                }
            }
            #endregion

            DownloadSchedule(session, uid);
        }

        private void OnDownloadFavoriteCompleted(Object param, String session, String uid)
        {
            if (param == null)
                throw new ArgumentNullException();

            var args = param as WTExecuteCompletedEventArgs<FavoriteGetResponse>;
            if (args == null)
                throw new NotSupportedException();

            var sb = new StringBuilder();

            #region [Activity]

            {
                sb.Clear();

                foreach (var a in args.Result.Activities)
                {
                    ActivityExt itemInShareDB = null;

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        itemInShareDB = db.Activities.Where((ac) => ac.Id == a.Id).SingleOrDefault();

                        //...Not stored in Share DB
                        if (itemInShareDB == null)
                        {
                            itemInShareDB = new ActivityExt();
                            itemInShareDB.SetObject(a);

                            db.Activities.InsertOnSubmit(itemInShareDB);
                        }
                        //...Stored in share DB
                        else
                        {
                            itemInShareDB.CanFavorite = false;
                        }
                        db.SubmitChanges();
                    }

                    sb.AppendFormat("{0}_", a.Id);

                    this.Dispatcher.BeginInvoke(() =>
                    {
                        Boolean hasMore = false;
                        if (ActivityListSource != null && ActivityListSource.Count > 0 && !ActivityListSource.Last().IsValid)
                            hasMore = true;

                        UpdateAcitivityList(new ActivityExt[] { itemInShareDB }, false, hasMore);
                    });
                }

                using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                {
                    var favObj = db.Favorites.Where((fo) => fo.Id == (uint)FavoriteIndex.kActivity).Single();
                    favObj.Value = sb.ToString().TrimEnd('_');

                    db.SubmitChanges();
                }
            }

            #endregion

            #region [PeopleOfWeek]

            {
                sb.Clear();

                foreach (var p in args.Result.People)
                {
                    PersonExt item = null;

                    #region [Handle Share db]

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        item = db.People.Where((people) => people.Id == p.Id).SingleOrDefault();
                    }

                    //...Not stored in Share DB
                    if (item == null)
                    {
                        item = new PersonExt();
                        item.SetObject(p);

                        using (var db = WTShareDataContext.ShareDB)
                        {
                            db.People.InsertOnSubmit(item);

                            db.SubmitChanges();
                        }
                    }
                    //...Stored in share DB
                    else
                    {
                        item.SetObject(p);
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            db.People.Attach(item);

                            db.SubmitChanges();
                        }
                    }

                    #endregion

                    sb.AppendFormat("{0}_", p.Id);
                }

                using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                {
                    var favObj = db.Favorites.Where((fo) => fo.Id == (uint)FavoriteIndex.kPeopleOfWeek).Single();
                    favObj.Value = sb.ToString().TrimEnd('_');

                    db.SubmitChanges();
                }
            }

            #endregion

            #region [Campus Info]

            #region [School News]

            {
                sb.Clear();

                foreach (var news in args.Result.SchoolNews)
                {
                    SchoolNewsExt item = null;

                    #region [Handle Share db]

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        item = db.SchoolNewsTable.Where((n) => n.Id == news.Id).SingleOrDefault();
                    }

                    //...Not stored in Share DB
                    if (item == null)
                    {
                        item = new SchoolNewsExt();
                        item.SetObject(news);

                        using (var db = WTShareDataContext.ShareDB)
                        {
                            db.SchoolNewsTable.InsertOnSubmit(item);

                            db.SubmitChanges();
                        }
                    }
                    //...Stored in share DB
                    else
                    {
                        item.SetObject(news);
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            db.SchoolNewsTable.Attach(item);

                            db.SubmitChanges();
                        }
                    }

                    #endregion

                    sb.AppendFormat("{0}_", news.Id);
                }

                using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                {
                    var favObj = db.Favorites.Where((fo) => fo.Id == (uint)FavoriteIndex.kTongjiNews).Single();
                    favObj.Value = sb.ToString().TrimEnd('_');

                    db.SubmitChanges();
                }
            }

            #endregion

            #region [Around News]

            {
                sb.Clear();

                foreach (var news in args.Result.Arounds)
                {
                    AroundExt item = null;

                    #region [Handle Share db]

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        item = db.AroundTable.Where((n) => n.Id == news.Id).SingleOrDefault();
                    }

                    //...Not stored in Share DB
                    if (item == null)
                    {
                        item = new AroundExt();
                        item.SetObject(news);

                        using (var db = WTShareDataContext.ShareDB)
                        {
                            db.AroundTable.InsertOnSubmit(item);

                            db.SubmitChanges();
                        }
                    }
                    //...Stored in share DB
                    else
                    {
                        item.SetObject(news);
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            db.AroundTable.Attach(item);

                            db.SubmitChanges();
                        }
                    }

                    #endregion

                    sb.AppendFormat("{0}_", news.Id);
                }

                using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                {
                    var favObj = db.Favorites.Where((fo) => fo.Id == (uint)FavoriteIndex.kAroundNews).Single();
                    favObj.Value = sb.ToString().TrimEnd('_');

                    db.SubmitChanges();
                }
            }

            #endregion

            #region [Official Notes]

            {
                sb.Clear();

                foreach (var news in args.Result.ForStaffs)
                {
                    ForStaffExt item = null;

                    #region [Handle Share db]

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        item = db.ForStaffTable.Where((n) => n.Id == news.Id).SingleOrDefault();
                    }

                    //...Not stored in Share DB
                    if (item == null)
                    {
                        item = new ForStaffExt();
                        item.SetObject(news);

                        using (var db = WTShareDataContext.ShareDB)
                        {
                            db.ForStaffTable.InsertOnSubmit(item);

                            db.SubmitChanges();
                        }
                    }
                    //...Stored in share DB
                    else
                    {
                        item.SetObject(news);
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            db.ForStaffTable.Attach(item);

                            db.SubmitChanges();
                        }
                    }

                    #endregion

                    sb.AppendFormat("{0}_", news.Id);
                }

                using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                {
                    var favObj = db.Favorites.Where((fo) => fo.Id == (uint)FavoriteIndex.kOfficialNotes).Single();
                    favObj.Value = sb.ToString().TrimEnd('_');

                    db.SubmitChanges();
                }
            }

            #endregion

            #region [Club News]

            {
                sb.Clear();

                foreach (var news in args.Result.ClubNews)
                {
                    ClubNewsExt item = null;

                    #region [Handle Share db]

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        item = db.ClubNewsTable.Where((n) => n.Id == news.Id).SingleOrDefault();
                    }

                    //...Not stored in Share DB
                    if (item == null)
                    {
                        item = new ClubNewsExt();
                        item.SetObject(news);

                        using (var db = WTShareDataContext.ShareDB)
                        {
                            db.ClubNewsTable.InsertOnSubmit(item);

                            db.SubmitChanges();
                        }
                    }
                    //...Stored in share DB
                    else
                    {
                        item.SetObject(news);
                        using (var db = WTShareDataContext.ShareDB)
                        {
                            db.ClubNewsTable.Attach(item);

                            db.SubmitChanges();
                        }
                    }

                    #endregion

                    sb.AppendFormat("{0}_", news.Id);
                }

                using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
                {
                    var favObj = db.Favorites.Where((fo) => fo.Id == (uint)FavoriteIndex.kClubNews).Single();
                    favObj.Value = sb.ToString().TrimEnd('_');

                    db.SubmitChanges();
                }
            }

            #endregion

            #endregion

            //...Update UI
            this.Dispatcher.BeginInvoke(() =>
            {
                UpdateFavoriteNumber(false);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="param">WTExecuteCompletedEventArgs[ScheduleGetResponse]</param>
        private void OnDownloadScheduleCompleted(Object param, String uid)
        {
            if (param == null)
            {
                throw new ArgumentNullException();
            }

            var arg = param as WTExecuteCompletedEventArgs<ScheduleGetResponse>;
            if (arg == null)
                throw new NotSupportedException("WTExecuteCompletedEventArgs<ScheduleGetResponse> is expected.");

            #region [Store or update activities]

            List<ActivityExt> unstoredActivities = new List<ActivityExt>();

            foreach (var a in arg.Result.Activities)
            {
                ActivityExt itemInDB = null;
                using (var db = WTShareDataContext.ShareDB)
                {
                    itemInDB = db.Activities.Where((activity) => activity.Id == a.Id).SingleOrDefault();
                }

                if (itemInDB == null)
                {
                    itemInDB = new ActivityExt();
                    itemInDB.SetObject(a);

                    unstoredActivities.Add(itemInDB);

                    using (var db = WTShareDataContext.ShareDB)
                    {
                        db.Activities.InsertOnSubmit(itemInDB);
                        db.SubmitChanges();
                    }
                }

                if (!Global.Instance.ParticipatingActivitiesIdList.Contains(a.Id))
                    Global.Instance.ParticipatingActivitiesIdList.Add(a.Id);
            }

            //....Insert new activities in UI
            this.Dispatcher.BeginInvoke(() =>
            {
                bool hasMore = false;
                if (ActivityListSource != null && ActivityListSource.Count > 0 && !ActivityListSource.Last().IsValid)
                    hasMore = true;

                if (unstoredActivities.Count > 0)
                    UpdateAcitivityList(unstoredActivities, false, hasMore);
            });

            #endregion

            #region [Save exams]

            {
                var firstExam = arg.Result.Exams.FirstOrDefault();
                if (firstExam != null)
                {
                    int count = arg.Result.Exams.Count();
                    ExamExt[] examExtArr = new ExamExt[count];
                    Semester targetSemester = null;
                    Semester[] allSemesters = null;

                    for (int i = 0; i < count; ++i)
                    {
                        examExtArr[i] = new ExamExt();
                        examExtArr[i].SetObject(arg.Result.Exams.ElementAt(i));
                        examExtArr[i].UID = uid;
                    }

                    using (var db = new WTUserDataContext(uid))
                    {
                        allSemesters = db.Semesters.ToArray();
                    }

                    if (allSemesters != null)
                        foreach (var s in allSemesters)
                        {
                            if (s.IsInSemester(examExtArr[0]))
                            {
                                for (int i = 0; i < count; ++i)
                                {
                                    examExtArr[i].SemesterGuid = s.Id;
                                }

                                ExamExt[] previousData = null;
                                using (var db = new WTUserDataContext(uid))
                                {
                                    previousData = db.Exams.Where((e) => e.SemesterGuid == s.Id).ToArray();
                                }

                                if (previousData != null && previousData.Count() > 0)
                                {
                                    foreach (var exam in previousData)
                                    {
                                        using (var db = new WTUserDataContext(uid))
                                        {
                                            try
                                            {
                                                db.Exams.Attach(exam);
                                            }
                                            catch { }

                                            db.Exams.DeleteOnSubmit(exam);
                                            db.SubmitChanges();
                                        }
                                    }
                                }

                                targetSemester = s;

                                break;
                            }
                        }

                    if (targetSemester != null)
                    {
                        foreach (var e in examExtArr)
                        {
                            using (var db = new WTUserDataContext(uid))
                            {
                                db.Exams.InsertOnSubmit(e);
                                db.SubmitChanges();
                            }
                        }
                    }
                }
            }

            #endregion

            Debug.WriteLine("Download schedule completed.");

            ComputeCalendar();
        }

        private void DownloadCourses(String session, String uid)
        {
            var tt_req = new TimeTableGetRequest<TimeTableGetResponse>();
            var tt_client = new WTDefaultClient<TimeTableGetResponse>();

            Debug.WriteLine("In download courses.");

            #region [Add handlers]

            tt_client.ExecuteFailed += (s, args) =>
            {
                Debug.WriteLine("Fail to get user's course.\nError:{0}", args.Error);
                DownloadSchedule(session, uid);
            };

            tt_client.ExecuteCompleted += (s, args) =>
            {
                OnDownloadCoursesCompleted(args.Result, session, uid);
            };

            #endregion

            tt_client.Execute(tt_req, session, uid);
        }

        private void DownloadFavorite(String session, String uid, int pageId = 0)
        {
            var fav_req = new FavoriteGetRequest<FavoriteGetResponse>();
            var fav_client = new WTDefaultClient<FavoriteGetResponse>();

            if (pageId > 0)
                fav_req.SetAdditionalParameter(WTDefaultClient<FavoriteGetResponse>.PAGE, pageId);

            #region [Add handlers]

            fav_client.ExecuteCompleted += (s, args) =>
            {
                if (Global.Instance.CurrentUserID == uid)
                {
                    OnDownloadFavoriteCompleted(args, session, uid);

                    if (args.Result.NextPager > 0)
                    {
                        DownloadFavorite(session, uid, args.Result.NextPager);
                    }
                }
            };

            #endregion

            fav_client.Execute(fav_req, session, uid);
        }

        private void DownloadSchedule(String session, String uid)
        {
            var schedule_req = new ScheduleGetRequest<ScheduleGetResponse>();
            var schedule_client = new WTDefaultClient<ScheduleGetResponse>();

            schedule_req.Begin = DateTime.Now.Date;
            schedule_req.End = DateTime.Now.Date + TimeSpan.FromDays(1);

            using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
            {
                var semesters = db.Semesters.ToArray();

                var s = semesters.Where((semester) => semester.SchoolYearStartAt <= DateTime.Now && semester.SchoolYearEndAt >= DateTime.Now).SingleOrDefault();

                if (s != null)
                {
                    if (s.SchoolYearStartAt < schedule_req.Begin)
                    {
                        schedule_req.Begin = s.SchoolYearStartAt;
                    }
                    if (s.SchoolYearEndAt > schedule_req.End)
                    {
                        schedule_req.End = s.SchoolYearEndAt;
                    }
                }
            }

            using (var db = WTShareDataContext.ShareDB)
            {
                var activities = db.Activities.ToArray();

                var latestActivity = activities.OrderByDescending((a) => a.CreatedAt).FirstOrDefault();

                if (ActivityExt.FirstActivityCreatedAt < schedule_req.Begin)
                {
                    schedule_req.Begin = ActivityExt.FirstActivityCreatedAt;
                }
                if (latestActivity != null && latestActivity.End > schedule_req.End)
                {
                    schedule_req.End = latestActivity.End;
                }
            }

            #region [Add handlers]

            schedule_client.ExecuteFailed += (s, args) =>
            {
                ComputeCalendar();
            };

            schedule_client.ExecuteCompleted += (s, args) =>
            {
                Debug.WriteLine("Get user's schedule completed.");

                OnDownloadScheduleCompleted(args, uid);
            };

            #endregion

            schedule_client.Execute(schedule_req, session, uid);
        }

        private void ComputeCalendar()
        {
            List<CalendarNode> list = new List<CalendarNode>();
            CourseExt[] courses = null;
            ExamExt[] exams = null;
            Semester[] semesters = null;

            #region [Collect source]

            using (var db = new WTUserDataContext(Global.Instance.Settings.UID))
            {
                courses = db.Courses.ToArray();
                exams = db.Exams.ToArray();
                semesters = db.Semesters.ToArray();
            }

            if (courses != null && semesters != null)
            {
                foreach (var c in courses)
                {
                    var s = semesters.Where((semester) => semester.Id == c.SemesterGuid).SingleOrDefault();
                    if (s != null)
                    {
                        list.AddRange(c.GetCalendarNodes(s));
                    }
                }
            }

            if (exams != null)
            {
                foreach (var e in exams)
                {
                    list.Add(e.GetCalendarNode());
                }
            }

            for (int i = 0; i < Global.Instance.ParticipatingActivitiesIdList.Count; ++i)
            {
                using (var db = WTShareDataContext.ShareDB)
                {
                    try
                    {
                        var activity = db.Activities.Where((a) => a.Id == Global.Instance.ParticipatingActivitiesIdList[i]).SingleOrDefault();
                        if (activity != null)
                            list.Add(activity.GetCalendarNode());
                    }
                    catch { }
                }
            }

            #endregion

            var calendarNodesByDayList = (from CalendarNode node in list
                                          group node by node.BeginTime.Date into n
                                          orderby n.Key
                                          select new CalendarGroup<CalendarNode>(n.Key, n)).ToList();

            CalendarNode targetNode = calendarNodesByDayList.GetNextCalendarNode();

            Global.Instance.SetAgendaSource(calendarNodesByDayList);

            this.Dispatcher.BeginInvoke(() =>
            {
                AlarmClockSource = targetNode;
                StopComputingCalendarNodes();
            });

        }

        #endregion

        #endregion

        #region [Enum]

        public enum ActivitySortMethod
        {
            kByCreationTime,
            kByScheduleNumber,
            kCompareToNow
        }

        #endregion
    }
}