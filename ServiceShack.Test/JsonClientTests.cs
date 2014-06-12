using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ServiceShack.Test
{
    [TestFixture]
    public class JsonClientTests
    {
        [Route("/help/{UserId}/{Banana}/")]
        public class SomeRequest : IReturn<string>
        {
            public int UserId { get; set; }
            public string Banana { get; set; }
        }

        
        [Test]
        public void Moo()
        {
            var client = new JsonClient("http://example.com");
            var req = new SomeRequest { UserId = 5, Banana = "hello" };
            Debug.WriteLine(client.BuildUri(req, Method.Get));
        }

        [Route("/stream/before/{BeforeDate}?format=json")]
        [Route("/stream/after/{AfterDate}?format=json")]
        public class StreamRequest : IReturn<Stream>
        {
            public string AfterDate { get; set; }
            public string BeforeDate { get; set; }
        }
        
        public class Stream
        {
            public List<Review> Reviews { get; set; }
        }

        public class Review
        {
            public string Id { get; set; }
            public double Rating { get; set; }
        }

        [Test]
        public async void RottenTomato()
        {
            var client = new JsonClient("https://beta.izooble.com");
            var request = new StreamRequest
            {
                BeforeDate = "2014-06-12T19:07:09.771Z"
            };

            var result = await client.Get(request);
            Assert.That(result.Reviews.Count(), Is.EqualTo(10));
        }
    }
}
