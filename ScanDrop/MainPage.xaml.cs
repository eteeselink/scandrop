using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using ScanDrop.Resources;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using System.IO;

namespace ScanDrop
{
    public partial class MainPage : PhoneApplicationPage
    {
        private CloudStore cloudStore = new CloudStore();
        private string chosenPhotoFilename;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var task = new CameraCaptureTask();
            task.Completed += (object photoSender, PhotoResult result) =>
            {
                if (result.TaskResult == TaskResult.OK)
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.SetSource(result.ChosenPhoto);
                    PreviewImage.Source = bmp;

                    chosenPhotoFilename = result.OriginalFileName;
                }
            };
            task.Show();
        }


        private void OnAuthenticated()
        {
            Status.Text = "Hooray!";
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            InitializeDropbox();
            base.OnNavigatedTo(e);
        }

        private bool IsReadyToAuthenticate()
        {
            return NavigationContext.QueryString.ContainsKey("ReturnFromOauth");
        }

        private async void InitializeDropbox()
        {
            Status.Text = "Connecting to Dropbox..";
            if (IsReadyToAuthenticate())
            {
                // we just got back from dropbox's oauth web page, so we must have a request token. try to close the cycle.
                Status.Text = "Authenticating...";
                await cloudStore.Authenticate();

                // reload page, so that "ReturnFromOauth" data is never accidentally triggered.
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            }
            else
            {
                await cloudStore.Load();
            }

            if (cloudStore.IsAuthenticated)
            {
                OnAuthenticated();
            }
            else
            {
                Status.Text = "No login info available";
                // no sign in at all, let's get oauthed.
                Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Not signed in to dropbox. Opening browser...");
                    var browser = new WebBrowserTask();
                    browser.Uri = new Uri(cloudStore.AuthorizationUrl);
                    browser.Show();
                });
            }
        }

        private async void DropButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var filename = Filename.Text;
            if(chosenPhotoFilename == null)
            {
                MessageBox.Show("First picture take, you must!");
                return;
            }
            await cloudStore.SaveFile(chosenPhotoFilename, filename);
        }
    }
}