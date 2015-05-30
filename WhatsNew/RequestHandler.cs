using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace WhatsNew
{
    static class RequestHandler
    {
        private static readonly Uri BaseAddress = new Uri("http://api.themoviedb.org/3/");
        private const string ApiKey = "b861bd20b4d5e1044703f7fb0b64f0b7";

        private static HttpClient client;

        private static int Reset = 0;
        private static int Remaining = 30;

        public static JObject MakeCall(string uri, string query=null)
        {
            if (client == null)
            {
                client = new HttpClient { BaseAddress = BaseAddress };
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            uri += "?api_key=" + ApiKey;
            uri += (query != null) ? "&query=" + query : "";

            if (Remaining == 0)
            {
                var unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                if (unixTimestamp < Reset)
                {
                    Console.WriteLine(@"Gotta wait {0} seconds", Reset - unixTimestamp);
                    Thread.Sleep((Reset - unixTimestamp) * 1000);
                }
            }

            var response = client.GetAsync(uri).Result;

            if (response.IsSuccessStatusCode)
            {
                IEnumerable<string> values;
                if (response.Headers.TryGetValues("X-RateLimit-Remaining", out values))
                {
                    Remaining = int.Parse(values.First());
                }
                if (response.Headers.TryGetValues("X-RateLimit-Reset", out values))
                {
                    Reset = int.Parse(values.First());
                }

                var json = response.Content.ReadAsStringAsync().Result;

                return JObject.Parse(json);
            }
            Console.WriteLine(@"Error: " + response.StatusCode);
            return null;
        }
    }
}
