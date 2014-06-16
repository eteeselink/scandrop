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
    /// <summary>
    /// Necessary because UserLogin does not have [DataMember] attrs around its members. Annoying.
    /// </summary>
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
            var accessToken = await LocalStorage.LoadObject<SerializeableUserLogin>(AccessTokenFn);
            if ((accessToken == null) || (accessToken.Secret == null))
            {
                var requestToken = await dropnet.GetRequestToken();
                await LocalStorage.SaveObject(RequestTokenFn, new SerializeableUserLogin(requestToken));
                return new AuthenticationStatus(true, dropnet.BuildAuthorizeUrl(requestToken, @"superset-scandrop:hello"));
            } 
            else
            {
                SignIn(accessToken.AsUserLogin());
                return new AuthenticationStatus(false, null);
            }
        }

        public async Task Authenticate()
        {
            var storedRequestToken = await LocalStorage.LoadObject<SerializeableUserLogin>(RequestTokenFn);
            dropnet.SetUserToken(storedRequestToken.AsUserLogin());
            var accessToken = await dropnet.GetAccessToken();
            SignIn(accessToken);
            await LocalStorage.SaveObject(AccessTokenFn, new SerializeableUserLogin(accessToken));            
        }

        private void SignIn(UserLogin accessToken)
        {
            dropnet.SetUserToken(accessToken);
        }

        public async Task SaveFile(Stream sourceData, string targetFilename)
        {
            var metadata = await dropnet.Upload("/", targetFilename, sourceData);
            if(metadata.Name != targetFilename)
            {
                throw new Exception("Filenames dont match!");
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
