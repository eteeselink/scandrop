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

namespace DropBoxImages
{
    public partial class MainPage : PhoneApplicationPage
    {

        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            CloudHandler handler = Application.Current.Resources["CloudHandler"] as CloudHandler;
            handler.InvokeGetAccessToken();
            NavigationService.Navigate(new Uri("/FileListPage.xaml", UriKind.RelativeOrAbsolute));
        }
    }
}