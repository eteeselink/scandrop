using DropNetRT;
using DropNetRT.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ScanDrop
{
    //class Dropbox
    //{
    //    private const string AppKey = "v1emabm3teyw5rc";
    //    private const string AppSecret = "vx9657sjpfc4ff8";
    //    private const string BaseUri = "https://api.dropbox.com/1";

    //    private readonly JsonClient jsonClient = new JsonClient(baseUri);
        

    //    public async Task<string> RequestToken()
    //    {
    //        return await Get("/oauth/request_token");
    //    }

    //    public string GetAuthorizeUrl(string requestToken, string callbackUrl)
    //    {
    //        return String.Format("https://www.dropbox.com/1/oauth/authorize?oauth_token={0}&oauth_callback={1}", requestToken, callbackUrl);
    //    }

    //    public async Task<string> AccessToken(string requestToken)
    //    {
    //        return await Post("/oauth/access_token", requestToken);
            
    //    }

    //    private async Task<string> Get(string uri)
    //    {
    //        var cl = new HttpClient();
    //        var response = await cl.GetAsync(BaseUri + uri);
    //        response.EnsureSuccessStatusCode();
    //        var result = await response.Content.ReadAsStringAsync();
    //        return result;
    //    }

    //    private async Task<string> Post(string uri, string content)
    //    {
    //        var cl = new HttpClient();
            
    //        var response = await cl.PostAsync(BaseUri + uri, new StringContent(content));
    //        response.EnsureSuccessStatusCode();
    //        var result = await response.Content.ReadAsStringAsync();
    //        return result;

    //    }
    //}
    class CloudStore
    {
        private const string AccessTokenFn = "access_token.txt";
        private const string AppKey = "v1emabm3teyw5rc";
        private const string AppSecret = "vx9657sjpfc4ff8";

        public UserLogin accessToken = null;
        private UserLogin requestToken;

        private readonly DropNetClient dropnet = new DropNetClient(AppKey, AppSecret);

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
                return dropnet.BuildAuthorizeUrl(requestToken, @"superset-scandrop:hello");
            }
        }

        public CloudStore()
        {
            //config = (DropBoxConfiguration)CloudStorage.GetCloudConfigurationEasy(nSupportedCloudConfigurations.DropBox);
            //config.AuthorizationCallBack = new Uri("http://www.catipsum.com/index.php#.U5mfZyzTb1A");
            //config.AuthorizationCallBack = new Uri(@"data:text/html;charset=utf-8,<%21DOCTYPE%20HTML%20PUBLIC%20""-%2F%2FW3C%2F%2FDTD%20HTML%204.0%2F%2FEN"">%0D%0A<html%20lang%3D""en"">%0D%0A%20<head>%0D%0A%20%20<title>Test<%2Ftitle>%0D%0A%20%20<style%20type%3D""text%2Fcss"">%0D%0A%20%20<%2Fstyle>%0D%0A%20<%2Fhead>%0D%0A%20<body>%0D%0A%20%20<p>Thanks%21%20Now%20please%20close%20the%20browser%20and%20reopen%20ScanDrop<%2Fp>%0D%0A%20<%2Fbody>%0D%0A<%2Fhtml>%0D%0A");
            //config.AuthorizationCallBack = new Uri(@"superset-scandrop:hello");
            dropnet.UseSandbox = true;
        }

        public async Task Load()
        {
            accessToken = await LoadObject<UserLogin>(TokenKind.Access);
            if (accessToken == null)
            {
                // TODO: om de een of andere reden is die requestToken soms leeg. snap niet waarom.
                // Aanpak: Fuck sharpbox, ik doe het zelf wel. Het is een simpele REST API, en alle "moeilijke" dingen (oauth volgorde etc)
                // heb ik toch al opgelost. Ha!
                requestToken = await dropnet.GetRequestToken();
                Debug.WriteLine(requestToken.Secret);
                Debug.WriteLine(requestToken.Token);
                await SaveObject(TokenKind.Request, requestToken);
            }
            else
            {
                SignIn();
            }
        }

        public async Task Authenticate(UserLogin storedRequestToken)
        {
            //var storedRequestToken = await LoadObject<UserLogin>(TokenKind.Request);
            dropnet.SetUserToken(storedRequestToken);
            accessToken = await dropnet.GetAccessToken();
            Debug.WriteLine(accessToken.Secret);
            Debug.WriteLine(accessToken.Token);
            await SaveObject(TokenKind.Access, accessToken);
            SignIn();

        }

        private void SignIn()
        {
            dropnet.SetUserToken(accessToken);
        }

        public async Task SaveFile(string sourceFilename, string targetFilename)
        {
            var bytes = Encoding.UTF8.GetBytes(sourceFilename);
            await dropnet.Upload("/", targetFilename, bytes);
        }

        private string BuildFilename(TokenKind kind)
        {
            return kind.ToString() + "_" + AccessTokenFn;
        }

        //private void SaveAccessToken(ICloudStorageAccessToken token)
        //{
        //    Save(BuildFilename(TokenKind.Access), (stream) =>
        //    {
        //        cloudStorage.SerializeSecurityTokenToStream(accessToken, stream);
        //    });

        //}

        //private ICloudStorageAccessToken LoadAccessToken()
        //{
        //    return Load(BuildFilename(TokenKind.Access), (stream) =>
        //    {
        //        return (ICloudStorageAccessToken)cloudStorage.DeserializeSecurityToken(stream);
        //    });
        //}

        private async Task SaveObject<T>(TokenKind kind, T data)
        {
        }

        private async Task<UserLogin> LoadObject<T>(TokenKind kind)
        {
            return new UserLogin
            {
                Secret = "9zzdpct174plso1",
                Token = "lyvaw5drepz1dayg"
            };
        }

        internal async Task Moo()
        {
            try
            {
                var q = await dropnet.GetMetaData("/");
                await dropnet.Upload("/", "moo.txt", Encoding.UTF8.GetBytes("obnoisnieo"));
                Debug.WriteLine(q.Name);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            
        }
    }
}
