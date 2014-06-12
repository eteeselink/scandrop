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
    public partial class FileListPage : PhoneApplicationPage
    {
        public FileListPage()
        {
            InitializeComponent();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CloudHandler ch = Application.Current.Resources["CloudHandler"] as CloudHandler;
            if (ch != null)
            {
                ListBox lb = sender as ListBox;
                if (lb != null)
                {
                    if (lb.SelectedIndex > -1)
                    {
                        CloudItem ci = lb.SelectedItem as CloudItem;
                        if (ci != null)
                        {
                            ch.CurrentItem = ci;
                            lb.SelectedIndex = -1;
                            NavigationService.Navigate(new Uri("/ImagePage.xaml", UriKind.RelativeOrAbsolute));
                        }
                    }
                }
            }
        }
    }
}