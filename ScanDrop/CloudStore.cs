using DropNetRT;
using DropNetRT.Models;
using ServiceShack;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ScanDrop
{

    [DataContract]
    public class SerializeableUserLogin
    {
        [DataMember]
        public string Secret { get; set; }

        [DataMember]
        public string Token { get; set; }
        public SerializeableUserLogin(UserLogin ul)
        {
            Secret = ul.Secret;
            Token = ul.Token;
        }

        public UserLogin AsUserLogin()
        {
            return new UserLogin
            {
                Secret = Secret,
                Token = Token
            };
        }
    }

    public class CloudStore
    {
        private const string AccessTokenFn = "access_token.txt";
        private const string RequestTokenFn = "request_token.txt";
        private const string AppKey = "v1emabm3teyw5rc";
        private const string AppSecret = "vx9657sjpfc4ff8";

        private readonly DropNetClient dropnet = new DropNetClient(AppKey, AppSecret);

        enum TokenKind
        {
            Request,
            Access
        }

        public CloudStore()
        {
            dropnet.UseSandbox = true;
        }

        public async Task<AuthenticationStatus> Load()
        {
            var accessToken = (await LoadObject<SerializeableUserLogin>(AccessTokenFn)).AsUserLogin();
            if ((accessToken == null) || (accessToken.Secret == null))
            {
                var requestToken = await dropnet.GetRequestToken();
                await SaveObject(RequestTokenFn, new SerializeableUserLogin(requestToken));
                return new AuthenticationStatus(true, dropnet.BuildAuthorizeUrl(requestToken, @"superset-scandrop:hello"));
            } 
            else
            {
                SignIn(accessToken);
                return new AuthenticationStatus(false, null);
            }
        }

        public async Task Authenticate()
        {
            var storedRequestToken = (await LoadObject<SerializeableUserLogin>(RequestTokenFn)).AsUserLogin();
            dropnet.SetUserToken(storedRequestToken);
            var accessToken = await dropnet.GetAccessToken();
            SignIn(accessToken);
            await SaveObject(AccessTokenFn, new SerializeableUserLogin(accessToken));            
        }

        private void SignIn(UserLogin accessToken)
        {
            dropnet.SetUserToken(accessToken);
        }

        public async Task SaveFile(string sourceFilename, string targetFilename)
        {
            var bytes = Encoding.UTF8.GetBytes(sourceFilename);
            await dropnet.Upload("/", targetFilename, bytes);
        }

        private Task SaveObject<T>(string filename, T data)
        {
            return Task.Run(() => 
            {
                Save(filename, (stream) =>
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));
                    serializer.WriteObject(stream, data);
                });
            });
        }

        private Task<T> LoadObject<T>(string filename)
        {
            return Task.Run(() =>
            {
                return Load<T>(filename, (stream) =>
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));
                    return (T)serializer.ReadObject(stream);
                });
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

        /// <summary>
        /// Tuple class returned from Load(). C# could use some shorthand for this stuff.
        /// </summary>
        public class AuthenticationStatus
        {
            public bool NeedsAuthorization { get; private set; }

            public string AuthorizationUrl { get; private set; }

            public AuthenticationStatus(bool needsAuthorization, string authorizationUrl)
            {
                NeedsAuthorization = needsAuthorization;
                AuthorizationUrl = authorizationUrl;
            }
        }
    }
}
