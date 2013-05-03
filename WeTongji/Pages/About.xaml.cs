using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WeTongji.Utility;

namespace WeTongji
{
    public partial class About : PhoneApplicationPage
    {
        public About()
        {
            InitializeComponent();

            this.Loaded += (o, e) =>
                {
                    Run_Version.Text = AppVersion.Current;
                };
        }

        private void Button_ViewAgreement_Click(Object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Pages/Agreement.xaml", UriKind.RelativeOrAbsolute));
        }

        private void Button_Rate_Click(Object sender, RoutedEventArgs e)
        {
            var task = new Microsoft.Phone.Tasks.MarketplaceReviewTask();
            task.Show();
        }

        private void Button_ViewOfficialWebsite_Click(Object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Pages/ViewOfficialWebsite.xaml", UriKind.RelativeOrAbsolute));
        }
    }
}