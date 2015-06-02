using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace WhatsNew
{
    static class RequestHandler
    {
        private static readonly Uri BaseAddress = new Uri("http://api.themoviedb.org/3/");
        private const string ApiKey = "b861bd20b4d5e1044703f7fb0b64f0b7";

        private static HttpClient client;

        private static int reset;
        private static int remaining = 30;

        public static JObject MakeCall(string uri, string query=null, int retryAfter=0)
        {
            if (client == null)
            {
                client = new HttpClient { BaseAddress = BaseAddress };
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            //string json;

            var hash = (uri + query).GetHashCode();
            //if (ResourceCache.TryGetResource(hash, out json))
            //{
            //    return JObject.Parse(json);
            //}

            var requestUri = uri;
            requestUri += "?api_key=" + ApiKey;
            requestUri += (query != null) ? "&query=" + query : "";

            if (retryAfter > 0)
            {
                Thread.Sleep((retryAfter + 1) * 1000);
            }
            if (remaining == 0)
            {
                var unixTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                if (unixTimestamp < reset)
                {
                    Console.WriteLine(@"Gotta wait {0}.5 seconds", reset - unixTimestamp);
                    Thread.Sleep((reset - unixTimestamp) * 1000 + 500);
                }
            }

            var response = client.GetAsync(requestUri).Result;

            if (!response.IsSuccessStatusCode)
            {
                // Explicitly handle 429 errors if system time is wrong 
                if (response.StatusCode == (HttpStatusCode)429)
                {
                    IEnumerable<string> retryHeader;
                    if (response.Headers.TryGetValues("Retry-After", out retryHeader))
                    {
                        var retry = int.Parse(retryHeader.First());
                        Console.WriteLine(@"retrying after {0} seconds", retry);
                        // ReSharper disable once TailRecursiveCall
                        return MakeCall(uri, query, retry);
                    }
                }

                Console.WriteLine(@"Error: " + response.StatusCode);
                return null;
            }

            IEnumerable<string> values;
            if (response.Headers.TryGetValues("X-RateLimit-Remaining", out values))
            {
                remaining = int.Parse(values.First());
            }
            if (response.Headers.TryGetValues("X-RateLimit-Reset", out values))
            {
                reset = int.Parse(values.First());
            }

            var json = response.Content.ReadAsStringAsync().Result;

            ResourceCache.Cache(hash, json);

            return JObject.Parse(json);
        }
    }
}
