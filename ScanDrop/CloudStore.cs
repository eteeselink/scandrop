using AppLimit.CloudComputing.SharpBox;
using AppLimit.CloudComputing.SharpBox.StorageProvider.DropBox;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ScanDrop
{
    class CloudStore
    {
        private const string AppKey = "v1emabm3teyw5rc";
        private const string AppSecret = "vx9657sjpfc4ff8";
        private const string AccessTokenFn = "access_token.txt";

        public ICloudStorageAccessToken accessToken = null;
        private DropBoxRequestToken requestToken;
        private readonly DropBoxConfiguration config;
        private readonly CloudStorage cloudStorage = new CloudStorage();

        enum TokenKind
        {
            Request,
            Access
        }

        public bool IsAuthenticated { get { return accessToken != null; } }
        public string AuthorizationUrl
        {
            get
            {
                return DropBoxStorageProviderTools.GetDropBoxAuthorizationUrl(config, requestToken);
            }
        }

        public CloudStore()
        {
            config = (DropBoxConfiguration)CloudStorage.GetCloudConfigurationEasy(nSupportedCloudConfigurations.DropBox);
            //config.AuthorizationCallBack = new Uri("http://www.catipsum.com/index.php#.U5mfZyzTb1A");
            //config.AuthorizationCallBack = new Uri(@"data:text/html;charset=utf-8,<%21DOCTYPE%20HTML%20PUBLIC%20""-%2F%2FW3C%2F%2FDTD%20HTML%204.0%2F%2FEN"">%0D%0A<html%20lang%3D""en"">%0D%0A%20<head>%0D%0A%20%20<title>Test<%2Ftitle>%0D%0A%20%20<style%20type%3D""text%2Fcss"">%0D%0A%20%20<%2Fstyle>%0D%0A%20<%2Fhead>%0D%0A%20<body>%0D%0A%20%20<p>Thanks%21%20Now%20please%20close%20the%20browser%20and%20reopen%20ScanDrop<%2Fp>%0D%0A%20<%2Fbody>%0D%0A<%2Fhtml>%0D%0A");
            config.AuthorizationCallBack = new Uri(@"superset-scandrop:hello");
        }

        public async Task Load()
        {
            await Task.Factory.StartNew(() =>
            {
                accessToken = LoadAccessToken();
                if (accessToken == null)
                {
                    // TODO: om de een of andere reden is die requestToken soms leeg. snap niet waarom.
                    // Aanpak: Fuck sharpbox, ik doe het zelf wel. Het is een simpele REST API, en alle "moeilijke" dingen (oauth volgorde etc)
                    // heb ik toch al opgelost. Ha!
                    requestToken = DropBoxStorageProviderTools.GetDropBoxRequestToken(config, AppKey, AppSecret);
                    SaveRequestToken(requestToken);
                } 
                else
                {
                    SignIn();
                }
            });
        }

        public async Task Authenticate()
        {
            await Task.Factory.StartNew(() =>
            {
                var storedRequestToken = LoadRequestToken();
                accessToken = DropBoxStorageProviderTools.ExchangeDropBoxRequestTokenIntoAccessToken(config, AppKey, AppSecret, storedRequestToken);
                SaveAccessToken(accessToken);
                SignIn();
            });
        }

        private void SignIn()
        {
            cloudStorage.Open(config, accessToken);
        }

        public async Task SaveFile(string sourceFilename, string targetFilename)
        {
            await Task.Factory.StartNew(() =>
            {
                try
                {
                var dir = cloudStorage.GetRoot();
                cloudStorage.UploadFile(sourceFilename, dir, targetFilename);
                }
                catch(Exception)
                {
                    //nothing
                }
            });
        }

        private string BuildFilename(TokenKind kind) {
            return kind.ToString() + "_" + AccessTokenFn;
        }

        private void SaveAccessToken(ICloudStorageAccessToken token)
        {
            Save(BuildFilename(TokenKind.Access), (stream) =>
            {
                cloudStorage.SerializeSecurityTokenToStream(accessToken, stream);
            });
            
        }

        private ICloudStorageAccessToken LoadAccessToken()
        {
            return Load(BuildFilename(TokenKind.Access), (stream) =>
            {
                return (ICloudStorageAccessToken)cloudStorage.DeserializeSecurityToken(stream);
            });
        }

        private void SaveRequestToken(DropBoxRequestToken token)
        {
            Save(BuildFilename(TokenKind.Request), (stream) => {
                
                var serializedStream = cloudStorage.SerializeSecurityTokenEx(token, typeof(DropBoxConfiguration), null);
                stream.Flush();
                byte[] accessTokenBytes;
                using (var memoryStream = new MemoryStream())
                {
                    serializedStream.CopyTo(memoryStream);
                    accessTokenBytes = memoryStream.ToArray();
                    stream.Write(accessTokenBytes, 0, accessTokenBytes.Length);
                }
            });
        }

        private DropBoxRequestToken LoadRequestToken()
        {
            return Load(BuildFilename(TokenKind.Request), (stream) => {
                return (DropBoxRequestToken)cloudStorage.DeserializeSecurityToken(stream);
            });
        }


        private T Load<T>(string filename, Func<Stream, T> reader)
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists(filename))
                {
                    using (var stream = new IsolatedStorageFileStream(filename, FileMode.OpenOrCreate, FileAccess.Read, store))
                    {
                        try
                        {
                            return reader(stream);
                        }
                        catch (Exception)
                        {
                            // silently ignore failures: something is iffy.
                        }
                    }
                }
            }
            return default(T);
        }

        private void Save(string filename, Action<Stream> writer)
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var stream = new IsolatedStorageFileStream(filename, FileMode.Create, FileAccess.Write, store))
            {
                writer(stream);
            }
        }
    }
}
