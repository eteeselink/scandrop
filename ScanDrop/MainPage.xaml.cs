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
using System.Threading.Tasks;
using System.ComponentModel;

namespace ScanDrop
{
    public partial class MainPage : PhoneApplicationPage
    {
        private CloudStore cloudStore = new CloudStore();
        private string currentTab;
        private const string LatestPhotoFilename = "LatestPhoto.jpg";

        private Task<Stream> LoadPhoto()
        {
            return Task.Run(() =>
            {
                return (Stream)LocalStorage.Load(LatestPhotoFilename, (stream) =>
                {
                    var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms;
                });
            });
        }

        private Task SavePhoto(Stream chosenPhoto)
        {
            return Task.Run(() =>
            {
                LocalStorage.Save(LatestPhotoFilename, stream =>
                {
                    chosenPhoto.Seek(0, SeekOrigin.Begin);
                    chosenPhoto.CopyTo(stream);
                });
            });
        }

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        private void StartScan()
        {
            var task = new CameraCaptureTask();
            task.Completed += (object photoSender, PhotoResult result) =>
            {
                if (result.TaskResult == TaskResult.OK)
                {
                    SavePhoto(result.ChosenPhoto);

                    ShowDropTab();
                }
                else
                {
                    // called when the user presses "back" on the camera task. as the camera task "feels" like the
                    // first screen, we want the app to terminate when this happens.
                    App.Current.Terminate();
                }
            };
            task.Show();
        }


        private void OnAuthenticated()
        {
            Status.Text = "Signed in to Dropbox.";
            SigninPleaseWait.Visibility = Visibility.Collapsed;

            if (currentTab == "Load")
            {
                StartScan();
            }
        }

        private void ShowLoadTab()
        {
            NavigationService.Navigate(new Uri("/MainPage.xaml?Tab=Load", UriKind.Relative));
        }

        private void ShowDropTab()
        {
            NavigationService.Navigate(new Uri("/MainPage.xaml?Tab=Drop", UriKind.Relative));
        }

        private void ShowRequestedPage()
        {
            NavigationContext.QueryString.TryGetValue("Tab", out currentTab);
            currentTab = currentTab ?? "Load";

            if (currentTab == "Drop")
            {
                WorkPanel.Visibility = Visibility.Visible;
                LoadingPanel.Visibility = Visibility.Collapsed;

                LoadPhoto().ContinueWith(photo =>
                {
                    if (photo.Result != null)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() => 
                        {
                            var bmp = new System.Windows.Media.Imaging.BitmapImage();
                            bmp.SetSource(photo.Result);
                            PreviewImage.Source = bmp;
                        });
                    }
                });

                Prefix.Text = DateTime.Now.ToString("yyyy-MM-dd_HH:mm_");
                Filename.Focus();
                Filename.SelectAll();
            }
            else
            {
                WorkPanel.Visibility = Visibility.Collapsed;
                LoadingPanel.Visibility = Visibility.Visible;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //if(e.NavigationMode == NavigationMode.Back)
            {
                //NavigationService.GoBack();
            }
            //else
            {
                ShowRequestedPage();
                InitializeDropbox();
            }

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
                ShowLoadTab();

                return;
            }
            
            // ok, we didn't just confirm a request token. normal flow starts now. load auth status
            // from file storage or request auth token if somehow signed out of dropbox (or this is the first
            // run on this phone).
            var authStatus = await cloudStore.Load();

            if (authStatus.NeedsAuthorization)
            {
                Status.Text = "No login info available";
                // no sign in at all, let's get oauthed.
                Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Not signed in to dropbox. Opening browser...");
                    var browser = new WebBrowserTask();
                    browser.Uri = new Uri(authStatus.AuthorizationUrl);
                    browser.Show();
                });
            }
            else
            {
                OnAuthenticated();
            }
        }

        private async void DropButton_Tap(object sender, System.Windows.Input.GestureEventArgs evt)
        {
            var filename = Prefix.Text + Filename.Text + Extension.Text;
            var photo = await LoadPhoto();
            if (photo == null)
            {
                MessageBox.Show("First picture take, you must!");
                return;
            }

            try
            {
                Status.Text = "Uploading..";
                await cloudStore.SaveFile(photo, filename);
                Status.Text = "Uploaded " + filename + ".";
                MessageBox.Show("Uploaded " + filename + ".");
                ShowLoadTab();
            }
            catch(Exception e)
            {
                Status.Text = "Couldn't upload! " + e.Message;
            }
        }
    }
}