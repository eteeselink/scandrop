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
using System.Windows.Media.Imaging;

namespace DropBoxImages
{
    public partial class ImagePage : PhoneApplicationPage
    {
        public ImagePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            CloudHandler ch = Application.Current.Resources["CloudHandler"] as CloudHandler;
            if (ch != null)
            {
                imageProgress.IsIndeterminate = true;
                ch.GetFileUri(UrlCallback, ch.CurrentItem.Entry);
            }
        }

        void UrlCallback(IAsyncResult result)
        {
            Uri fileUri = result as Uri;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                CloudHandler ch = Application.Current.Resources["CloudHandler"] as CloudHandler;
                fileUri = ch.EndGetFileUri(result);
                BitmapImage bi = new BitmapImage();
                bi.DownloadProgress += new EventHandler<DownloadProgressEventArgs>(bi_DownloadProgress);
                bi.UriSource = fileUri;
                currentImage.Source = bi;
            });
        }

        void bi_DownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            if (e.Progress > 99)
            {
                imageProgress.IsIndeterminate = false;
            }
        }

    }
}