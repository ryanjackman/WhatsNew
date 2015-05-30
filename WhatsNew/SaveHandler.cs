using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace WhatsNew
{
    static class SaveHandler
    {
        private const String SeriesDirectory = @"data/";

        public static IEnumerable<Series> ReadShows()
        {
            var shows = new List<Series>();
            if (!Directory.Exists(SeriesDirectory))
            {
                Console.WriteLine(@"No such directory");
                return shows;
            }

            var fileEntries = Directory.GetFiles(SeriesDirectory);

            foreach (var file in fileEntries)
            {
                var openFile = File.OpenRead(file);
                var fileName = Path.GetFileName(openFile.Name);
                openFile.Close();

                var stream = new StreamReader(file);
                if (fileName == null) continue;
                var id = int.Parse(fileName);
                var s = ReadFromJson(stream, id);
                if (s != null)
                {
                    shows.Add(s);
                }  
                stream.Close();
            }

            return shows;
        }

        public static Series ReadFromJson(StreamReader stream, int id)
        {
            var watched = new List<int>();
            while (stream != null && !stream.EndOfStream)
            {
                var watchedId = stream.ReadLine();
                if (watchedId == null) throw new NullReferenceException();
                watched.Add(int.Parse(watchedId));
            }

            var jSeries = RequestHandler.MakeCall("tv/" + id);
            if (jSeries == null) return null;

            var show = new Series(jSeries);

            foreach(var season in show.Seasons)
            {
                foreach (var episode in season.Episodes)
                {
                    episode.Watched = watched.Contains(episode.Id);
                }
            }

            show.UpdateIcon();

            return show;

        }

        public static void SaveShows(IEnumerable<Series> shows)
        {
            foreach(var show in shows)
            {
                var file = show.Id.ToString(CultureInfo.InvariantCulture);

                var writer = new StreamWriter(SeriesDirectory + file);

                foreach (var e in from s in show.Seasons from e in s.Episodes where e.Watched select e)
                {
                    writer.WriteLine(e.Id);
                }

                writer.Close();
            }
        }
    }
}
