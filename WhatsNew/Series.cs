using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Cache;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;

namespace WhatsNew
{
    public class Series : ISeriesListItem
    {
        public ObservableCollection<Season> Seasons { get; set; }

        private static BitmapImage orangeBullet, whiteBullet;

        public Series(JObject json)
        {
            Seasons = new ObservableCollection<Season>();
            try
            {
                Name = (string) json["name"];
                Id = (int) json["id"];
            }
            catch (Exception)
            {
                Console.WriteLine(@"no series name and/or id");
            }
            
            JToken[] jSeasons;
            try
            {
                jSeasons = json["seasons"].ToArray();
            }
            catch (Exception)
            {
                return;
            }

            foreach(var jSeason in jSeasons)
            {
                Seasons.Add(new Season(Id, jSeason));
            }
        }

        public Series(string name)
        {
            Name = name;
        }

        private bool Watched()
        {
            foreach (var s in Seasons)
            {
                if (s.SeasonNumber == 0) continue;
                foreach (var e in s.Episodes)
                {
                    if (!e.Watched) return false;
                }
            }
            return true;
        }

        public void UpdateIcon()
        {
            Application.Current.Dispatcher.Invoke( delegate
            {
                if (orangeBullet == null) orangeBullet = InitImage("../../res/bullet_orange.png");
                if (whiteBullet == null) whiteBullet = InitImage("../../res/bullet_white.png");

                var t = Watched();
                Source = (t ? whiteBullet : orangeBullet);
            });
        }

        private static BitmapImage InitImage( string path )
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.None;
            image.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.UriSource = new Uri(path, UriKind.Relative);
            image.EndInit();

            return image;
        }

        public void MarkWatched()
        {
            foreach (var s in Seasons)
                s.MarkWatched();
        }

        public string Name { get; set; }
        public int Id { get; private set; }
        public ImageSource Source { get; set; }
        public Boolean IsSelected  { get; set; }
        public Boolean IsExpanded { get; set; }
    }
}