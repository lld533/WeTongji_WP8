using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace WeTongji
{
    public partial class SignUpSuccess : PhoneApplicationPage
    {
        public SignUpSuccess()
        {
            InitializeComponent();

            var btn = new ApplicationBarIconButton(new Uri("/icons/appbar.check.rest.png", UriKind.RelativeOrAbsolute))
                {
                    Text = StringLibrary.SignUpSuccess_AppBarFinishSigningUpText
                };
            btn.Click += NavBackToMainPage;
            this.ApplicationBar.Buttons.Add(btn);
            
        }

        private void NavBackToMainPage(Object sender, EventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickToGoBackAtSignUpThirdStep).ToString());
#endif
            #endregion

            this.NavigationService.RemoveBackEntry();
            this.NavigationService.RemoveBackEntry();
            this.NavigationService.GoBack();
        }

        private void BrowseTongjiMail(Object sender, RoutedEventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickToBrowseTongjiMailPortAtSignUpThirdStep).ToString());
#endif
            #endregion

            var task = new Microsoft.Phone.Tasks.WebBrowserTask();
            task.Uri = new Uri("http://mail.tongji.edu.cn");
            task.Show();
        }
    }
}