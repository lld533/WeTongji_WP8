using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.Diagnostics;
using WeTongji.Api.Domain;
using WeTongji.DataBase;
using WeTongji.Business;
using System.Collections.ObjectModel;
using WeTongji.Pages;
using System.Threading;
using System.Windows.Media.Animation;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Threading;
using System.Globalization;

namespace WeTongji
{
    public partial class MyAgenda : PhoneApplicationPage
    {
        public MyAgenda()
        {
            InitializeComponent();

            Global.Instance.AgendaSourceChanged += AgendaSourceStateChanged;
            refresh_dt.Tick += Refresh_Tick;
        }

        private DispatcherTimer refresh_dt = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };

        private void AgendaSourceStateChanged(Object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                var src = this.LongListSelector_Core.ItemsSource;
                LongListSelector_Core.ItemsSource = null;
                LongListSelector_Core.ItemsSource = src;

                var node = Global.Instance.AgendaSource.GetNextCalendarNode();
            });
        }

        private CalendarNode CurrentNode { get; set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            //...Create AppButton_Today if it has not been created yet.
            if (this.ApplicationBar == null || this.ApplicationBar.Buttons.Count == 0)
            {
                var appBtn = new Microsoft.Phone.Shell.ApplicationBarIconButton()
                {
                    Text = StringLibrary.MyAgenda_AppBarTodayText,
                    IsEnabled = false
                };

                //...Todo @_@ Localizable
                if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "zh")
                    appBtn.IconUri = new Uri(String.Format("/icons/days_zh/{0}/{0}_{1}.png", DateTime.Now.Month, DateTime.Now.Day),
                            UriKind.RelativeOrAbsolute);
                else
                    appBtn.IconUri = new Uri(String.Format("/icons/days_en/{0}/{0}_{1}.png", DateTime.Now.Month, DateTime.Now.Day),
                        UriKind.RelativeOrAbsolute);

                appBtn.Click += AppButton_Today_Clicked;
                this.ApplicationBar = new Microsoft.Phone.Shell.ApplicationBar();
                this.ApplicationBar.Buttons.Add(appBtn);
            }
            //...Refresh the icon AppButton_Today if it has already been created.
            else
            {
                var btn = this.ApplicationBar.Buttons[0] as Microsoft.Phone.Shell.ApplicationBarIconButton;

                //...Todo @_@ Localizable
                if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "zh")
                    btn.IconUri = new Uri(String.Format("/icons/days_zh/{0}/{0}_{1}.png", DateTime.Now.Month, DateTime.Now.Day),
                            UriKind.RelativeOrAbsolute);
                else
                    btn.IconUri = new Uri(String.Format("/icons/days_en/{0}/{0}_{1}.png", DateTime.Now.Month, DateTime.Now.Day),
                        UriKind.RelativeOrAbsolute);

                btn.IsEnabled = false;
            }

            Global.Instance.AgendaSourceStateChanged += this.AgendaSourceStateChangedHandler;

            var thread = new Thread(new ThreadStart(LoadData)) { IsBackground = true };
            thread.Start();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            Global.Instance.AgendaSourceStateChanged -= this.AgendaSourceStateChangedHandler;
        }

        private void LoadData()
        {
            AgendaSourceStateChangedHandler(Global.Instance, EventArgs.Empty);
        }

        private void AppButton_Today_Clicked(Object sender, EventArgs e)
        {
            if (LongListSelector_Core.ItemsSource != null)
            {
                try
                {
#if Flurry
                    FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickAppBarTodayButton).ToString());
#endif

                    var src = LongListSelector_Core.ItemsSource as List<CalendarGroup<CalendarNode>>;
                    var group = src.Where((g) => g.Key == DateTime.Now.Date).SingleOrDefault();
                    if (group != null)
                    {
                        //LongListSelector_Core.ScrollToGroup(group);
                        LongListSelector_Core.ScrollTo(group);
                    }
                }
                catch { }
            }
        }

        private void AgendaSourceStateChangedHandler(Object sender, EventArgs e)
        {
            var global = sender as Global;

            switch (global.CurrentAgendaSourceState)
            {
                case SourceState.Done:
                    {
                        var nodeToScrollTo = global.AgendaSource.GetNextCalendarNode();

                        this.Dispatcher.BeginInvoke(() =>
                        {
                            CurrentNode = nodeToScrollTo;

                            #region [LongListSelector has no source]

                            if (LongListSelector_Core.ItemsSource == null)
                            {
                                TextBlock_Loading.Visibility = Visibility.Collapsed;
                                LongListSelector_Core.ItemsSource = global.AgendaSource;
                                LongListSelector_Core.UpdateLayout();
#if false
                                #region [Binding Top item with scrolling offset]

                                if (!registerVerticalScrollChanged)
                                {
                                    DependencyObject depObj = LongListSelector_Core;

                                    while (!(depObj is ScrollViewer))
                                    {
                                        Debug.WriteLine(depObj.GetType());
                                        depObj = VisualTreeHelper.GetChild(depObj, 0);
                                    }

                                    this.SetBinding(DependencyProperty.RegisterAttached("VerticalOffset",

                                        typeof(double),

                                        this.GetType(),

                                        new PropertyMetadata((obj, arg) =>
                                        {
                                            if (VerticalScrollChanged != null)

                                                VerticalScrollChanged(this, EventArgs.Empty);

                                        })),

                                    new Binding("VerticalOffset") { Source = depObj as ScrollViewer });

                                    registerVerticalScrollChanged = true;
                                }

                                #endregion
#endif

                                if (nodeToScrollTo.IsNoArrangementNode)
                                {
                                    AppButton_Today_Clicked(this.ApplicationBar.Buttons[0], EventArgs.Empty);
                                }
                                else
                                {
                                    LongListSelector_Core.ScrollTo(nodeToScrollTo);
                                    LongListSelector_Core.UpdateLayout();
#if false
                                    #region [Play donate animation]
                                    {
                                        DependencyObject obj = LongListSelector_Core;
                                        while (!(obj is VirtualizingStackPanel))
                                        {
                                            obj = VisualTreeHelper.GetChild(obj, 0);
                                        }

                                        int count = VisualTreeHelper.GetChildrenCount(obj);

                                        for (int i = 0; i < count; ++i)
                                        {
                                            var tmp = VisualTreeHelper.GetChild(obj, i);

                                            //var source = (tmp as Control).DataContext as LongListSelectorItem;
                                            //if (source.Item == nodeToScrollTo)
                                            //{
                                            //    obj = tmp;

                                            //    //...Get the layout root of the item
                                            //    while (!(obj is ContentPresenter))
                                            //    {
                                            //        obj = VisualTreeHelper.GetChild(obj, 0);
                                            //    }

                                            //    var itemLayoutRoot = VisualTreeHelper.GetChild(obj, 0) as Panel;
                                            //    Storyboard sb = itemLayoutRoot.Resources["Donate"] as Storyboard;
                                            //    sb.Begin();
                                            //    break;
                                            //}
                                        }
                                    }
                                    #endregion
#endif
                                }
                            }
                            #endregion
                            #region [LongListSelector has source]
                            else
                            {
                                this.Dispatcher.BeginInvoke(() =>
                                {
                                    LongListSelector_Core.ItemsSource = global.AgendaSource;
                                });
                            }
                            #endregion

                            (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;
                            refresh_dt.Start();
                        });
                    }
                    break;
                case SourceState.NotSet:
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            LongListSelector_Core.ItemsSource = null;
                            (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false;
                        });
                    }
                    break;
                case SourceState.Setting:
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            TextBlock_Loading.Visibility = Visibility.Visible;
                            (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false;
                        });
                    }
                    break;
            }

        }

        private void Refresh_Tick(Object sender, EventArgs e)
        {
            if (CurrentNode == null)
                return;

            Action core = () =>
            {
                CurrentNode = Global.Instance.AgendaSource.GetNextCalendarNode();
                AgendaSourceStateChanged(Global.Instance, EventArgs.Empty);
            };

            //...date changed
            if (CurrentNode.IsNoArrangementNode && DateTime.Now.Date > CurrentNode.BeginTime.Date)
            {
                //...Todo @_@ Localizable
                if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "zh")
                    (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IconUri
                        = new Uri(String.Format("/icons/days_zh/{0}/{0}_{1}.png", DateTime.Now.Month, DateTime.Now.Day), UriKind.RelativeOrAbsolute);
                else
                    (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IconUri
                        = new Uri(String.Format("/icons/days_en/{0}/{0}_{1}.png", DateTime.Now.Month, DateTime.Now.Day), UriKind.RelativeOrAbsolute);

                core();
            }
            //...Current node is expired
            else if (!CurrentNode.IsNoArrangementNode && DateTime.Now > CurrentNode.BeginTime)
            {
                core();
            }
        }

        private void LongListtSelector_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            var lls = sender as LongListSelector;

            if (lls.SelectedItem == null)
                return;

            var item = lls.SelectedItem as CalendarNode;
            lls.SelectedItem = null;

            if (!item.IsNoArrangementNode)
                switch (item.NodeType)
                {
                    case CalendarNodeType.kActivity:
                        this.NavigationService.Navigate(new Uri("/Pages/Activity.xaml?q=" + item.Id, UriKind.RelativeOrAbsolute));
                        return;
                    case CalendarNodeType.kObligedCourse:
                    case CalendarNodeType.kOptionalCourse:
                        this.NavigationService.Navigate(new Uri(String.Format("/Pages/CourseDetail.xaml?q={0}&d={1}", item.Id, item.BeginTime), UriKind.RelativeOrAbsolute));
                        return;
                    case CalendarNodeType.kExam:
                        this.NavigationService.Navigate(new Uri(String.Format("/Pages/CourseDetail.xaml?q={0}", item.Id), UriKind.RelativeOrAbsolute));
                        return;
                }
        }
        
    }
}