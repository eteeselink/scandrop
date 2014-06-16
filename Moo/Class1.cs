using DropNetRT.Models;
using ScanDrop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Moo
{
    public class Class1
    {
        public async Task Moo()
        {
            var cs = new CloudStore();
            await cs.Load();
            Debug.WriteLine(cs.AuthorizationUrl);
        }

        public async Task Auth()
        {
            var cs = new CloudStore();
            var requestToken = new UserLogin
            {
                Secret = "pmi2qxfHOwYOvyBF",
                Token = "W91mXsPy0wNWLyTU"
            };
            await cs.Authenticate(requestToken);
        }

        public async Task Test()
        {
            var cs = new CloudStore();
            await cs.Load();
            await cs.Moo();
        }

        public void LoadLoad()
        {
            //https://www.dropbox.com/1/oauth/authorize?oauth_token=oRcaQSL3XxSMTtmo&oauth_callback=superset-scandrop:hello
            var t = Moo();
            t.Wait();
        }


        public void AuthAuth()
        {
            var t = Auth();
            t.Wait();
        }

        public void TestTest()
        {
            var t = Test();
            t.Wait();
        }

        

        public void UsesrLogin()
        {
            var u = new UserLogin
            {
                Secret = "moo",
                Token = "maa"
            };
            var serializer = new DataContractJsonSerializer(typeof(SerializeableUserLogin));
            var str = new MemoryStream();
            serializer.WriteObject(str, new SerializeableUserLogin(u));
            var arr = str.ToArray();
            Debug.WriteLine(Encoding.UTF8.GetString(arr, 0, arr.Length));
        }
    }
}
