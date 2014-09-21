using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TMDbLib.Objects.TvShows;
using System.Collections.ObjectModel;

namespace WhatsNew
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public List<TvShow> shows;
        public HashSet<int> watchedEpisodes;

        List<Series> treeItems = new List<Series>();

        bool building = false;

        public MainWindow()
        {
            InitializeComponent();

            shows = new List<TvShow>();
            watchedEpisodes = new HashSet<int>();

            BuildTree();

            //http://www.wpf-tutorial.com/list-controls/listbox-control/
            
        }

        public void BuildTree()
        {
            building = true;
            var context = TaskScheduler.FromCurrentSynchronizationContext();

            treeItems.Clear();
            Series loading = new Series("Loading...");
            treeItems.Add(loading);

            Task task = Task.Factory.StartNew(() =>
            {
                readShows();

                foreach (TvShow ts in shows)
                {
                    Series s = new Series(ts);
                    foreach (TvSeason tse in s.show.Seasons)
                    {
                        Season se = new Season(tse);
                        tse.Episodes = App.client.GetTvSeason(ts.Id, tse.SeasonNumber).Episodes;
                        foreach (TvEpisode te in tse.Episodes)
                        {
                            se.episodes.Add(new Episode(te));
                        }
                        s.seasons.Add(se);
                    }
                    treeItems.Add(s);
                }
            }).ContinueWith(_ =>
            {
                seriesTree.ItemsSource = treeItems;
                treeItems.Remove(loading);
                building = false;
            }, context);

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!building)
            {
                BuildTree();
            }
            
        }

        private void readShows()
        {
            Console.WriteLine("reading");
            if (!System.IO.File.Exists("shows.txt"))
            {
                Console.WriteLine("No shows loaded.");
                return;
            }
            System.IO.StreamReader file = new System.IO.StreamReader("shows.txt");
            while (!file.EndOfStream)
            {
                int id = int.Parse(file.ReadLine());
                shows.Add(App.client.GetTvShow(id));
            }
            file.Close();

            if (!System.IO.File.Exists("watched.txt"))
            {
                Console.WriteLine("No shows watched.");
                return;
            }
            file = new System.IO.StreamReader("watched.txt");
            while (!file.EndOfStream)
            {
                watchedEpisodes.Add(int.Parse(file.ReadLine()));
            }
            file.Close();
        }

        private void writeShows()
        {
            Console.WriteLine("saving");
            System.IO.StreamWriter file = new System.IO.StreamWriter("shows.txt");
            foreach (TvShow s in shows)
            {
                file.WriteLine(s.Id);
            }
            file.Flush();

            file = new System.IO.StreamWriter("watched.txt");
            foreach (int i in watchedEpisodes)
            {
                file.WriteLine(i);
            }
            file.Flush();

            file.Close();
        }
    }

    public class Series
    {
        public TvShow show;
        public ObservableCollection<Season> seasons { get; set; }
        
        public Series(TvShow show)
        {
            this.show = show;
            seasons = new ObservableCollection<Season>();
            Name = show.Name;
        }

        public Series(string Name)
        {
            this.Name = Name;
        }

        public string Name { get; set; }
    }

    public class Season
    {
        public TvSeason season;
        public ObservableCollection<Episode> episodes { get; set; }

        public Season(TvSeason season)
        {
            this.season = season;
            episodes = new ObservableCollection<Episode>();
            SeasonNumber = season.SeasonNumber;
        }

        public int SeasonNumber { get; set; }
    }

    public class Episode
    {
        public TvEpisode episode;

        public Episode(TvEpisode episode)
        {
            this.episode = episode;
            Name = episode.Name;
        }

        public string Name { get; set; }
    }
}
