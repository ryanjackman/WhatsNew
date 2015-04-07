using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace WhatsNew
{
    static class RequestHandler
    {
        private static readonly Uri BaseAddress = new Uri("http://api.themoviedb.org/3/");
        private const string ApiKey = "b861bd20b4d5e1044703f7fb0b64f0b7";

        public static JObject MakeCall(string uri, string query=null)
        {
            var client = new HttpClient {BaseAddress = BaseAddress};
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            uri += "?api_key=" + ApiKey;
            uri += (query != null) ? "&query=" + query : "";

            var response = client.GetAsync(uri).Result;

            if (response.IsSuccessStatusCode)
            {
                var json = response.Content.ReadAsStringAsync().Result;
                return JObject.Parse(json);
            }
            Console.WriteLine(@"Error:" + response.RequestMessage);
            return null;
        }

    }
}
