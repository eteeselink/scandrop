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
using Windows.Storage;

namespace ScanDrop
{
    public partial class MainPage : PhoneApplicationPage
    {
        private CloudStore cloudStore = new CloudStore();
        private string currentTab;
        private const string LatestPhotoFilename = "LatestPhoto.jpg";
        private const string LatestPhotoFilenameCompressed = "LatestPhoto_Compressed.jpg";

        public MainPage()
        {
            InitializeComponent();
        }

        private void StartScan(bool takePicture = true)
        {
            var task = takePicture 
                ? (ChooserBase<PhotoResult>)new CameraCaptureTask()
                : (ChooserBase<PhotoResult>)new PhotoChooserTask();

            task.Completed += async (object photoSender, PhotoResult result) =>
            {
                if (result.TaskResult == TaskResult.OK)
                {
                    await SavePhoto(result.ChosenPhoto);
                    ShowDropTab();
                }
                else
                {
                    // called when the user presses "back" on the camera task.
                    ShowScanTab();
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


        private void ShowScanTab()
        {
            NavigationService.Navigate(new Uri("/MainPage.xaml?Tab=Scan", UriKind.Relative));
        }

        private DateTime GetCreationDate(Stream imageData)
        {
            DateTime creationDate;

            // Using ExifLib to read creationDate. Not using `using` on purpose, because then ExifReader disposes `photo` as well.
            var reader = new ExifLib.ExifReader(imageData);

            // Found by experimenting that WP-made photos have DateTimeOriginal set. Good enough!
            reader.GetTagValue<DateTime>(ExifLib.ExifTags.DateTimeOriginal, out creationDate);

            // rewind stream.
            imageData.Seek(0, SeekOrigin.Begin);

            return creationDate;
        }

        private void SetupDropTab(Task<Stream> photoResult)
        {
            var photo = photoResult.Result;
            if (photo != null)
            {
                DateTime creationDate = GetCreationDate(photo);

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.SetSource(photo);
                    PreviewImage.Source = bmp;

                    Prefix.Text = creationDate.ToString("yyyy-MM-dd_HH.mm_");
                });
            }
        }

        private void ShowRequestedPage()
        {
            NavigationContext.QueryString.TryGetValue("Tab", out currentTab);
            currentTab = currentTab ?? "Load";

            switch(currentTab)
            {
                case "Scan":
                    LoadingPanel.Visibility = Visibility.Collapsed;
                    ScanPanel.Visibility = Visibility.Visible;
                    WorkPanel.Visibility = Visibility.Collapsed;

                    while(NavigationService.BackStack.Any()) 
                        NavigationService.RemoveBackEntry();

                    break;

                case "Drop":
                    LoadingPanel.Visibility = Visibility.Collapsed;
                    ScanPanel.Visibility = Visibility.Collapsed;
                    WorkPanel.Visibility = Visibility.Visible;

                    LoadPhoto(LatestPhotoFilename).ContinueWith(SetupDropTab);

                    // for reasons completely past my understanding, we need to do this inside the UI thread for it to work. I thought 
                    // OnNavigatedTo was already on the ui thread? Ahwell.
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        
                        Filename.Focus();
                        Filename.SelectAll();
                    });
                    break;

                case "Load":
                default:
                    LoadingPanel.Visibility = Visibility.Visible;
                    ScanPanel.Visibility = Visibility.Collapsed;
                    WorkPanel.Visibility = Visibility.Collapsed;
                    break;

            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {           
            ShowRequestedPage();
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
            var photo = await LoadPhoto(LatestPhotoFilenameCompressed);
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


        private Task<Stream> LoadPhoto(string filename)
        {
            return Task.Run(() =>
            {
                return (Stream)LocalStorage.Load(filename, (stream) =>
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
            return Dispatcher.InvokeAsync(() =>
            {
                // save compressed file
                chosenPhoto.Seek(0, SeekOrigin.Begin);

                var bitmap = new BitmapImage
                {
                    CreateOptions = BitmapCreateOptions.None
                };
                bitmap.SetSource(chosenPhoto);
                var wbitmap = new WriteableBitmap(bitmap);

                LocalStorage.Save(LatestPhotoFilenameCompressed, stream =>
                {
                    wbitmap.SaveJpeg(stream, bitmap.PixelWidth / 2, bitmap.PixelHeight / 2, 0, 80);
                });

                // save original, for exif data.
                chosenPhoto.Seek(0, SeekOrigin.Begin);
                LocalStorage.Save(LatestPhotoFilename, stream =>
                {
                    chosenPhoto.CopyTo(stream);
                });
            });
        }

        private void TakePictureButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StartScan();
        }

        private void ChoosePictureButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StartScan(false);
        }

        private async void PreviewImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // Because we store our image in ApplicationData, we can just launch the picture viewer with a file reference.
            // The OS finds out the appropriate association for ".jpg". This took a *long* time to find out, by the way.
            var file = await LocalStorage.Folder.GetFileAsync(LatestPhotoFilename);

            await Windows.System.Launcher.LaunchFileAsync(file);
        }
    }
}