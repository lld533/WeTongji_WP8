using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone;
using System.IO.IsolatedStorage;
using System.IO;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Input;
using WeTongji.Api.Request;
using WeTongji.Api.Domain;
using WeTongji.Api.Response;
using WeTongji.Api;
using System.Threading;
using WeTongji.Pages;
using WeTongji.Utility;
using WeTongji.Business;

namespace WeTongji
{
    public partial class MySettings : PhoneApplicationPage
    {
        public MySettings()
        {
            InitializeComponent();
        }

        private void SettingsPageLoaded(Object sender, RoutedEventArgs e)
        {
            ToggleSwitch_AutoRefresh.IsChecked = Global.Instance.Settings.AutoRefresh;
            ToggleSwitch_HintOnExit.IsChecked = Global.Instance.Settings.HintOnExit;


            Run_Version.Text = AppVersion.Current;

            var thread = new Thread(new ThreadStart(ComputeImageCacheSize))
            {
                IsBackground = true
            };

            thread.Start();
        }

        private String WrapCacheSize(long sz, int digits)
        {
            float f = (float)(sz) / (float)(1 << digits);
            return f == 0.0F ? "0" : f.ToString("0.00");
        }

        private void SettingsButtonMouseEnter(Object sender, MouseEventArgs e)
        {
            var btn = sender as Button;
            var obj = VisualTreeHelper.GetChild(btn, 0);
            obj = VisualTreeHelper.GetChild(obj, 0);
            var cc = VisualTreeHelper.GetChild(obj, 0) as ContentControl;

            (cc.Projection as PlaneProjection).RotationY = 10;
        }

        private void SettingsButtonMouseLeave(Object sender, MouseEventArgs e)
        {
            var btn = sender as Button;
            var obj = VisualTreeHelper.GetChild(btn, 0);
            obj = VisualTreeHelper.GetChild(obj, 0);
            var cc = VisualTreeHelper.GetChild(obj, 0) as ContentControl;

            cc.Projection = new PlaneProjection();
        }

        private void CheckVersion(Object sender, RoutedEventArgs e)
        {
            var req = new SystemVersionRequest<SystemVersionResponse>();
            var client = new WTDefaultClient<SystemVersionResponse>();

            client.ExecuteFailed += (obj, args) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    if (args.Error is System.Net.WebException)
                    {
                        WTToast.Instance.Show(StringLibrary.Toast_NetworkErrorPrompt);
                    }
                    else
                    {
                        MessageBox.Show(StringLibrary.MySettings_CheckNewVersionFailedPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                    }
                });
            };

            client.ExecuteCompleted += (obj, args) =>
                {
                    #region [Flurry]
#if Flurry
                    FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.CheckUpdate).ToString());
#endif
                    #endregion

                    if (args.Result.Version == null)
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            MessageBox.Show(StringLibrary.MySettings_NoNewVersionPrompt, StringLibrary.Common_Prompt, MessageBoxButton.OK);
                        });
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            var result = MessageBox.Show(String.Format(StringLibrary.MySettings_NewVersionExistsPrompt, args.Result.Version.Latest), StringLibrary.Common_Prompt, MessageBoxButton.OKCancel);
                            if (result == MessageBoxResult.OK)
                            {
                                var task = new Microsoft.Phone.Tasks.WebBrowserTask();
                                task.Uri = new Uri(args.Result.Version.Url);
                                task.Show();
                            }
                        });
                    }
                };

            client.Execute(req);
        }

        private void ClearImageCache(Object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(StringLibrary.MySettings_ClearImageCacheConfirmation, StringLibrary.Common_Prompt, MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.OK)
            {
                var thread = new Thread(new ThreadStart(ClearImageCacheCore))
                {
                    IsBackground = true,
                    Name = "ClearImageCacheCore"
                };

                thread.Start();
            }
        }

        private void ComputeImageCacheSize()
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
                TextBlock_ImageCache.Text = StringLibrary.MySettings_ComputingImageCache;
            });

            var store = IsolatedStorageFile.GetUserStoreForApplication();
            var files = store.GetFileNames().Where(
                (name) => name.ToLower().EndsWith(".jpg")
                    || name.ToLower().EndsWith(".png")
                    || name.ToLower().EndsWith(".bmp")
                    || name.ToLower().EndsWith(".jpeg")
                    || name.ToLower().EndsWith("gif"));

            long szStore = 0;

            foreach (var f in files)
            {
                try
                {
                    using (var fs = store.OpenFile(f, FileMode.Open))
                        szStore += fs.Length;
                }
                catch { }
            }

            this.Dispatcher.BeginInvoke(() =>
            {
                if (szStore >= (1 << 30))
                {
                    TextBlock_ImageCache.Text = String.Format(StringLibrary.MySettings_ImageCacheSizeDisplayTemplate, WrapCacheSize(szStore, 30) + " GB");
                }
                else if (szStore >= (1 << 20))
                {
                    TextBlock_ImageCache.Text = String.Format(StringLibrary.MySettings_ImageCacheSizeDisplayTemplate, WrapCacheSize(szStore, 20) + " MB");
                }
                else
                {
                    TextBlock_ImageCache.Text = String.Format(StringLibrary.MySettings_ImageCacheSizeDisplayTemplate, WrapCacheSize(szStore, 10) + " KB");
                }

                ProgressBarPopup.Instance.Close();
            });
        }

        private void ClearImageCacheCore() 
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Open();
            });

            var store = IsolatedStorageFile.GetUserStoreForApplication();

            var files = store.GetFileNames().Where(
                            (name) => name.ToLower().EndsWith(".jpg")
                                || name.ToLower().EndsWith(".png")
                                || name.ToLower().EndsWith(".bmp")
                                || name.ToLower().EndsWith(".jpeg")
                                || name.ToLower().EndsWith("gif"));

            foreach (var f in files)
            {
                try
                {
                    store.DeleteFile(f);
                }
                catch { }

            }

            this.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarPopup.Instance.Close();
            });

            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClearImageCache).ToString());
#endif
            #endregion

            var thread = new Thread(new ThreadStart(ComputeImageCacheSize))
            {
                IsBackground = true
            };

            thread.Start();
        }

        private void ToggleSwitch_HintOnExit_Checked(Object sender, RoutedEventArgs e)
        {
            Global.Instance.Settings.HintOnExit = true;
            Global.Instance.SaveSettings();

            #region [Flurry]

#if Flurry

            FlurryWP8SDK.Api.LogEvent(
                ((int)FlurryWP8SDK.Models.EventName.SetConfirmToExit).ToString(),
                new List<FlurryWP8SDK.Models.Parameter>(
                    new FlurryWP8SDK.Models.Parameter[]{
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.ResultState).ToString(), 
                                    "1"
                                    )
                            })
                    );
#endif

            #endregion
        }

        private void ToggleSwitch_HintOnExit_UnChecked(Object sender, RoutedEventArgs e)
        {
            Global.Instance.Settings.HintOnExit = false;
            Global.Instance.SaveSettings();

            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(
                ((int)FlurryWP8SDK.Models.EventName.SetConfirmToExit).ToString(),
                new List<FlurryWP8SDK.Models.Parameter>(
                    new FlurryWP8SDK.Models.Parameter[]{
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.ResultState).ToString(), 
                                    "0"
                                    )
                            })
                    );
#endif
            #endregion
        }

        private void ToggleSwitch_AutoRefresh_Checked(Object sender, RoutedEventArgs e)
        {
            Global.Instance.Settings.AutoRefresh = true;
            Global.Instance.SaveSettings();

            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(
                ((int)FlurryWP8SDK.Models.EventName.SetAutoRefresh).ToString(),
                new List<FlurryWP8SDK.Models.Parameter>(
                    new FlurryWP8SDK.Models.Parameter[]{
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.ResultState).ToString(), 
                                    "1"
                                    )
                            })
                    );
#endif
            #endregion
        }

        private void ToggleSwitch_AutoRefresh_UnChecked(Object sender, RoutedEventArgs e)
        {
            Global.Instance.Settings.AutoRefresh = false;
            Global.Instance.SaveSettings();

            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(
                ((int)FlurryWP8SDK.Models.EventName.SetAutoRefresh).ToString(),
                new List<FlurryWP8SDK.Models.Parameter>(
                    new FlurryWP8SDK.Models.Parameter[]{
                                new FlurryWP8SDK.Models.Parameter(
                                    ((int)FlurryWP8SDK.Models.ParameterName.ResultState).ToString(), 
                                    "0"
                                    )
                            })
                    );
#endif
            #endregion
        }
    }
}