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

namespace WeTongji
{
    public partial class TongjiMail : PhoneApplicationPage
    {
        public TongjiMail()
        {
            InitializeComponent();
        }

        void Button_Registered_Click(Object sender, RoutedEventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickSignUpNextStepButton).ToString());
#endif
            #endregion

            this.NavigationService.Navigate(new Uri("/Pages/SignUp.xaml", UriKind.RelativeOrAbsolute));
        }

        void Button_SignUpTongjiMail_Click(Object sender, RoutedEventArgs e)
        {
            #region [Flurry]
#if Flurry
            FlurryWP8SDK.Api.LogEvent(((int)FlurryWP8SDK.Models.EventName.ClickActivateButton).ToString());
#endif
            #endregion

            var wbt = new WebBrowserTask();
            wbt.Uri = new System.Uri("http://mail.tongji.edu.cn:9900/activate/register.jsp");
            wbt.Show();
        }
    }
}