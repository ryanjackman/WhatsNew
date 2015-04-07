using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;

namespace WhatsNew
{
    public class Episode : ISeriesListItem
    {

        public Episode(JToken episodeData)
        {
            Name = (string) episodeData["name"];
            Id = (int) episodeData["id"];
            EpisodeNumber = (int) episodeData["episode_number"];
            Info = new ObservableCollection<string> { (string) episodeData["overview"] };
            try
            {
                AirDate = DateTime.Parse((string) episodeData["air_date"]);
            }
            catch (Exception)
            {
                Console.WriteLine(@"No air date");
            }

            Watched = false;

        }

        public void MarkWatched()
        {
            Watched = true;
        }

        public ObservableCollection<string> Info { get; set; }
        public DateTime AirDate { get; private set; }
        public int EpisodeNumber { get; set; }
        public string Name { get; set; }
        public int Id { get; private set; }
        public bool Watched { get; set; }
        public Boolean IsSelected { get; set; }
        public Boolean IsExpanded { get; set; }
    }
}