using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using AppLimit.CloudComputing.SharpBox.StorageProvider;
using AppLimit.CloudComputing.SharpBox;
using AppLimit.CloudComputing.SharpBox.Common;
using AppLimit.CloudComputing.SharpBox.StorageProvider.DropBox;
using AppLimit.CloudComputing.SharpBox.StorageProvider.API;
using System.IO.IsolatedStorage;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Resources;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace DropBoxImages
{
    public class CloudHandler : ModelBase
    {
        internal class BackgroundRequest : AsyncObjectRequest
        {
            public Object OperationResult;
            public Object OperationParameter;
            public ICloudFileSystemEntry fileEntry;
        }

        private static string FileName = "accessToken.xml";
        const string APP_SECRET = "<<YOUR_APP_SECRET>>";
        const string APP_KEY = "<<YOUR_APP_KEY>>";

        ICloudStorageAccessToken token = null;
        private ICloudStorageConfiguration mCloudConfig = CloudStorage.GetCloudConfigurationEasy(nSupportedCloudConfigurations.DropBox);
        DropBoxConfiguration config = null;

        private DropBoxRequestToken requestToken = null;
        private BackgroundWorker _bgAppUpdateLog;
        private CloudStorage m_dropBox = new CloudStorage();
        private List<ICloudDirectoryEntry> m_directories = new List<ICloudDirectoryEntry>();
        private ObservableCollection<CloudItem> m_files = new ObservableCollection<CloudItem>();
        public ObservableCollection<CloudItem> Files
        {
            get { return m_files; }
            set
            {
                if (value != m_files)
                {
                    m_files = value;
                    NotifyPropertyChanged("Files");
                }
            }
        }

        private bool m_loadingFileList;
        public bool LoadingFileList
        {
            get { return m_loadingFileList; }
            set
            {
                if (value != m_loadingFileList)
                {
                    m_loadingFileList = value;
                    NotifyPropertyChanged("LoadingFileList");
                }
            }
        }
        
        private CloudItem m_currentItem;
        public CloudItem CurrentItem
        {
            get { return m_currentItem; }
            set
            {
                if (value != m_currentItem)
                {
                    m_currentItem = value;
                    NotifyPropertyChanged("CurrentItem");
                }
            }
        }

        public CloudHandler()
        {
        }

        public void CloseCloud()
        {
            m_dropBox.Close();
        }

        public void InvokeGetAccessToken()
        {
            _bgAppUpdateLog = new BackgroundWorker();
            _bgAppUpdateLog.DoWork += new DoWorkEventHandler(bg_bgAppUpdateLog_DoWork);
            _bgAppUpdateLog.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bg_bgAppUpdateLog_RunWorkerCompleted);
            _bgAppUpdateLog.RunWorkerAsync();
        }

        private void bg_bgAppUpdateLog_DoWork(object sender, DoWorkEventArgs e)
        {
            config = DropBoxConfiguration.GetStandardConfiguration();
            if (IsolatedStorageFile.GetUserStoreForApplication().FileExists(FileName))
            {
                token = LoadAccessTokenFromIsolatedStorage();
            }
            else
            {
                requestToken = DropBoxStorageProviderTools.GetDropBoxRequestToken(config, APP_KEY, APP_SECRET);
                //Put a breakpoint in the next line and make sure to visit AuthorizationUrl manually in the web browser
                String AuthorizationUrl = DropBoxStorageProviderTools.GetDropBoxAuthorizationUrl(config, requestToken);
                token = DropBoxStorageProviderTools.ExchangeDropBoxRequestTokenIntoAccessToken(config, APP_KEY, APP_SECRET, requestToken);
            }
            m_dropBox.Open(config, token);

            //// get a specific directory in the cloud storage, e.g. /Public
            //var publicFolder = cloudStorage.GetFolder("/Photos");

            //foreach (var fof in publicFolder)
            //{
            //    //check if we have a directory
            //    Boolean bIsDirectory = fof is ICloudDirectoryEntry;

            //    //output the info
            //    Debug.WriteLine("{0}: {1}", bIsDirectory ? "DIR" : "FIL", fof.Name);
            //}

            //cloudStorage.Close();
        }

        private void bg_bgAppUpdateLog_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {  
            SaveAccessTokenToIsolatedStorage(token);
            ConnectCloud();
        }

        /*
        //Sharpbox 1.1 and older
        public void ConnectCloud(string username, string password)
        {
            DropBoxCredentials creds = new DropBoxCredentials();
            creds.ConsumerSecret = APP_SECRET;
            creds.ConsumerKey = APP_KEY;
            creds.UserName = username;
            creds.Password = password;

            m_dropBox.BeginOpenRequest(LoginCallback, mCloudConfig, creds);
        }
        */

        public void ConnectCloud()
        {
            LoadingFileList = true;
            m_files.Clear();
            CurrentItem = new CloudItem() { Name = "no files" };
            m_files.Add(CurrentItem);

            if (token != null)
            {
                m_dropBox.BeginOpenRequest(LoginCallback, mCloudConfig, token);
            }
        }

        private void SaveAccessTokenToIsolatedStorage(ICloudStorageAccessToken accessToken)
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var stream = new IsolatedStorageFileStream(FileName, FileMode.Create, FileAccess.Write, store))
                {
                    Stream accessTokenStream;
                    accessTokenStream = m_dropBox.SerializeSecurityToken(accessToken);
                    stream.Flush();
                    byte[] accessTokenBytes;
                    using (var streamReader = new MemoryStream())
                    {
                        accessTokenStream.CopyTo(streamReader);
                        accessTokenBytes = streamReader.ToArray();
                        stream.Write(accessTokenBytes, 0, accessTokenBytes.Length);
                    }
                }
            }
        }

        private ICloudStorageAccessToken LoadAccessTokenFromIsolatedStorage()
        {
            ICloudStorageAccessToken accessToken = null;
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var stream =
                   new IsolatedStorageFileStream(FileName, FileMode.OpenOrCreate, FileAccess.Read, store))
                {
                    //using (var reader = new StreamReader(stream))
                    {
                        var cloudStorage = new CloudStorage();
                        accessToken = (ICloudStorageAccessToken)cloudStorage.DeserializeSecurityToken(stream);
                    }
                }
            }
            return accessToken;
        }

        void LoginCallback(IAsyncResult result)
        {
            ICloudStorageAccessToken token = m_dropBox.EndOpenRequest(result);
            if (token != null)
            {
                m_dropBox.BeginGetRootRequest(RootCallback);
            }
            else if (m_dropBox.IsOpened)
            {
                m_dropBox.BeginGetRootRequest(RootCallback);
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        LoadingFileList = false;
                    });
            }
        }

        private void RootCallback(IAsyncResult result)
        {
            ICloudDirectoryEntry root = m_dropBox.EndGetRootRequest(result);
            if (root != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        m_files.Clear();
                    });
                m_dropBox.BeginGetChildsRequest(ChildCallback, root);
            }
        }

        private void ChildCallback(IAsyncResult result)
        {

            List<ICloudFileSystemEntry> fs = m_dropBox.EndGetChildsRequest(result);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (fs != null)
                {
                    parseFilesAndDirectories(fs);
                    while (m_directories.Count > 0)
                    {
                        ICloudDirectoryEntry e = m_directories[0];
                        m_directories.RemoveAt(0);
                        m_dropBox.BeginGetChildsRequest(ChildCallback, e);
                    }
                    LoadingFileList = false;
                }
                else
                {
                    LoadingFileList = false;
                }
            });
        }

        private void parseFilesAndDirectories(List<ICloudFileSystemEntry> direntry)
        {
            foreach (ICloudFileSystemEntry entry in direntry)
            {
                System.Diagnostics.Debug.WriteLine("entry: " + entry.Name);
                if (entry is ICloudDirectoryEntry)
                {
                    m_directories.Add((ICloudDirectoryEntry)entry);
                }

                if (entry is ICloudFileSystemEntry && entry.Name.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase))
                {
                    Files.Add(new CloudItem() { Name = entry.Name, Entry = entry });
                }
            }
        }

        public void GetFileUri(AsyncCallback callback, ICloudFileSystemEntry entry)
        {
            BackgroundRequest request = new BackgroundRequest();
            request.callback = callback;
            request.result = new AsyncResultEx(request);
            request.fileEntry = entry;
            ThreadPool.QueueUserWorkItem(GetFileUriCallback, request);
        }

        private void GetFileUriCallback(object state)
        {
            BackgroundRequest req = state as BackgroundRequest;

            try
            {
                req.OperationResult = m_dropBox.GetFileSystemObjectUrl(req.fileEntry.Name, req.fileEntry.Parent);
            }
            catch (Exception e)
            {
                var openRequest = req.result.AsyncState as BackgroundRequest;
                openRequest.OperationResult = null;
                openRequest.errorReason = e;
            }

            req.callback(req.result);
        }

        public Uri EndGetFileUri(IAsyncResult result)
        {
            BackgroundRequest req = result.AsyncState as BackgroundRequest;
            return req.OperationResult as Uri;
        }        
    }
}
