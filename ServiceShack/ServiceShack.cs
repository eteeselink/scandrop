using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ServiceShack
{
    public interface IReturn<T>
    {
        // marker interface for request DTOs.
    }

    [Flags]
    public enum Method
    {
        None = 0,
        Get = 1,
        Put = 2,
        Post = 4,
        Delete = 8,
        Patch = 16,
        Options = 32
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public class RouteAttribute : Attribute
    {
        public static readonly Method AllMethods = Enum.GetValues(typeof(Method)).Cast<Method>().Aggregate(Method.None, (m1, m2) => m1 | m2);

        public string UrlTemplate { get; set; }
        public Method AcceptedMethods { get; set; }

        public RouteAttribute(string urlTemplate, Method allowedMethods)
        {
            UrlTemplate = urlTemplate;
            AcceptedMethods = allowedMethods;
        }

        public RouteAttribute(string urlTemplate)
            : this(urlTemplate, AllMethods)
        {
        }
    }

    public class JsonClient
    {
        private readonly string baseUri;

        public JsonClient(string baseUri)
        {
            this.baseUri = baseUri;
            
        }

        public async Task<T> Get<T>(IReturn<T> request)
        {
            var method = Method.Get;
            var uri = BuildUri(request, method);
            var webRequest = WebRequest.CreateHttp(uri);

            webRequest.Method = method.ToString().ToUpper();

            var webResponse = await Task<WebResponse>.Factory.FromAsync(webRequest.BeginGetResponse, webRequest.EndGetResponse, null);
            var response = (HttpWebResponse)webResponse;

            using (var stream = response.GetResponseStream())
            {
                var statusCode = (int)response.StatusCode;
                if (statusCode > 200 || statusCode >= 300)
                {
                    var str = new StreamReader(stream).ReadToEnd();
                    throw new Exception("Oh no! Error! " + str);
                }

                var serializer = new DataContractJsonSerializer(typeof(T));

                var obj = serializer.ReadObject(stream);
                return (T)obj;
            }
        }

        internal string BuildUri<T>(IReturn<T> request, Method method)
        {
            var type = request.GetType().GetTypeInfo();
            var attrs = type.GetCustomAttributes<RouteAttribute>(true);
            var validRoutes = attrs
                .Where(attr => (attr.AcceptedMethods & method) == method)
                .Select(attr => attr.UrlTemplate);

            var properties = type.DeclaredProperties
                .ToDictionary(
                    prop => String.Format("{{{0}}}", prop.Name),
                    prop => prop.GetValue(request))
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var matchedRoutes = validRoutes.Select(route => MatchRoute(route, properties)).ToList();
            var highest = matchedRoutes.Max(t => t.Item1);
            var bestRoutes = matchedRoutes.Where(t => t.Item1 == highest);
            if(bestRoutes.Count() > 1)
            {
                // TODO: class
                throw new Exception("Ambiguous routes on type " + type.Name + ": " + String.Join(", ", validRoutes));
            }
            if(!bestRoutes.Any())
            {
                // TODO: class
                throw new Exception("Could not find any matching route on type " + type.Name);
            }
            return baseUri + bestRoutes.Single().Item2;
        }

        private Tuple<int, string> MatchRoute(string routeTemplate, Dictionary<string, object> properties)
        {
            int replacements = 0;
            string newRouteTemplate = routeTemplate;
            foreach(var kvp in properties)
            {
                // TODO: this is slow and evil: we "tostring" an object even when it's not found.
                newRouteTemplate = routeTemplate.Replace(kvp.Key, kvp.Value.ToString());
                if (newRouteTemplate != routeTemplate) replacements++;
                routeTemplate = newRouteTemplate;
            }
            return Tuple.Create(replacements, routeTemplate);
        }
    }
}
