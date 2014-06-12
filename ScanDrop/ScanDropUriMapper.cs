using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace ScanDrop
{
    class ScanCropUriMapperUriMapper : UriMapperBase
    {
        public override Uri MapUri(Uri uri)
        {
            var tempUri = System.Net.HttpUtility.UrlDecode(uri.ToString());
            if (tempUri.Contains("hello"))
            {
                return new Uri("/MainPage.xaml?ReturnFromOauth=yes", UriKind.Relative);
            }
            else
            {
                return uri;
            }
        }
    }
}
