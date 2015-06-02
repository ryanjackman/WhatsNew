using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WhatsNew
{
    static class ResourceCache
    {
        private const string CacheDirectory = @"cache/";
        private const string FileType = @".json";

        public static void Cache(int hash, string json)
        {
            var parsedJson = JObject.Parse(json);
            var timestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            parsedJson.Add("cache_timestamp", timestamp);

            var p = Priority.Never;

            if (parsedJson["in_production"] != null)
            {
                var status = (bool)parsedJson["in_production"];
                p = status ? Priority.Low : Priority.Never;
            }

            if (parsedJson["season_number"] != null)
            {
                DateTime airdate;
                if (DateTime.TryParse((string) parsedJson["air_date"], out airdate))
                {
                    var airdateTimestamp = (int) (airdate.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                    p = airdateTimestamp + (60*60*24*365) > timestamp ? Priority.High : Priority.Never;
                }
                else
                {
                    p = Priority.High;
                }
            }

            parsedJson.Add("update_priority", (int)p);

            try
            {
                var writer = new StreamWriter(CacheDirectory + hash + FileType);
                writer.WriteLine(parsedJson.ToString(Formatting.None));
                writer.Close();
            }
            catch (Exception)
            {
                Console.WriteLine(@"Error cacheing " + hash);
            }
        }

        private static bool TryGetResource(int hash, out string json)
        {
            json = null;

            if(!File.Exists(CacheDirectory + hash + FileType))
            {
                return false;
            }

            var reader = new StreamReader(CacheDirectory + hash + FileType);
            json = reader.ReadLine();
            reader.Close();

            return true;
        }

        public static JObject GetResourceOrUpdate(string uri, string query=null)
        {
            if (query != null) return RequestHandler.MakeCall(uri, query);

            var hash = (uri).GetHashCode();
            var timenow = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            
            string json;
            if (!TryGetResource(hash, out json))
            {
                Console.WriteLine(@"no cache for {0}", uri);
                return RequestHandler.MakeCall(uri);
            }
            
            var parsedJson = JObject.Parse(json);
            var timestamp = (int)parsedJson["cache_timestamp"];

            switch ((Priority)(int)parsedJson["update_priority"])
            {
                case Priority.High:
                    // Update minutely
                    if (timestamp + (60) < timenow)
                    {
                        Console.WriteLine(@"updating {0}", uri);
                        return RequestHandler.MakeCall(uri);
                    }
                    return parsedJson;
                case Priority.Low:
                    // Update weekly
                    return timestamp + (60 * 60 * 24 * 7) < timenow ? RequestHandler.MakeCall(uri) : parsedJson;
                case Priority.Never:
                    // Never Update
                    Console.WriteLine(@"using local {0}", uri);
                    return parsedJson;
            }

            return RequestHandler.MakeCall(uri);
        }
    }

    enum Priority
    {
        High,
        Low,
        Never
    };
}
