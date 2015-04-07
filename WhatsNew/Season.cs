using System;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace WhatsNew
{
    public class Season : ISeriesListItem
    {
        public ObservableCollection<Episode> Episodes { get; set; }

        public Season(int seriesId, JToken jSeason)
        {
            SeasonNumber = (int) jSeason["season_number"];

            Episodes = new ObservableCollection<Episode>();
            jSeason = RequestHandler.MakeCall(String.Format("tv/{0}/season/{1}", seriesId, SeasonNumber));
            if (jSeason == null) return;
            var jEpisodes = jSeason["episodes"].ToArray();

            foreach (var jEpisode in jEpisodes)
            {
                Episodes.Add(new Episode(jEpisode));
            }
        }

        public void MarkWatched()
        {
            foreach (var e in Episodes)
                e.MarkWatched();
        }

        public int SeasonNumber { get; set; }
        public Boolean IsSelected { get; set; }
        public Boolean IsExpanded { get; set; }
    }
}