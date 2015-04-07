using System;
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
                shows.Add(ReadFromId(stream, id));
                stream.Close();
            }

            return shows;
        }

        public static Series ReadFromId(StreamReader stream, int id)
        {
            var watched = new List<int>();
            while (stream != null && !stream.EndOfStream)
            {
                var watchedId = stream.ReadLine();
                if (watchedId == null) throw new NullReferenceException();
                watched.Add(int.Parse(watchedId));
            }

            var show = new Series(App.Client.GetTvShow(id));

            foreach (var tse in show.Show.Seasons)
            {
                var se = new Season(tse);
                tse.Episodes = App.Client.GetTvSeason(show.Show.Id, tse.SeasonNumber).Episodes;
                foreach (var te in tse.Episodes)
                {
                    var episode = new Episode(te);
                    episode.Watched = watched.Contains(episode.Id);
                    se.Episodes.Add(episode);
                }
                show.Seasons.Add(se);
            }

            show.UpdateIcon();
            return show;
        }

        public static void SaveShows(IEnumerable<Series> shows)
        {
            foreach(var show in shows)
            {
                var file = show.Id.ToString();

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
