using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Cache;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TMDbLib.Objects.TvShows;
using System.Collections.ObjectModel;

namespace WhatsNew
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static MainWindow _main;

        private ObservableCollection<Series> seriesList = new ObservableCollection<Series>();

        public MainWindow()
        {
            InitializeComponent();
            if (_main == null) _main = this;
            BuildTree();
        }

        private void BuildTree()
        {
            SeriesTree.ItemsSource = null;
            var loading = new Series("Loading...");
            seriesList.Add(loading);
            SeriesTree.ItemsSource = seriesList;
            DataContext = seriesList;

            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                foreach (var s in SaveHandler.ReadShows())
                {
                    seriesList.Add(s);
                }
                seriesList.Remove(loading);

                if (seriesList.Count > 0)
                {
                    SeriesTree.ItemsSource = new List<Series> {seriesList[0]};
                }
                SeriesList.ItemsSource = seriesList;
                FindNewEpisodes();
            }));

        }

        private void FindNewEpisodes()
        {
            NewEpisodeList.Items.Clear();
            foreach (var series in seriesList)
            {
                var nextShow = false;
                foreach (var season in series.Seasons)
                {
                    if (nextShow) break;
                    if (season.SeasonNumber == 0) continue;
                    foreach (var e in season.Episodes.Where(e =>  (!e.Watched && e.AirDate.CompareTo(DateTime.Today) < 0)))
                    {
                        var line = String.Format("{0} Season {1} - {2}", series.Name, season.SeasonNumber, e.Name);
                        NewEpisodeList.Items.Add(line);
                        nextShow = true;
                        break;
                    }
                }
            }
            NewEpisodeList.Items.Refresh();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach(var s in seriesList){
                s.UpdateIcon();
            }
            FindNewEpisodes();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveHandler.SaveShows(seriesList);
        }

        private void ShowList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var temp = new List<Series> {SeriesList.SelectedItem as Series};
            SeriesTree.ItemsSource = temp;
        }
        private void AddButtonClick(object sender, RoutedEventArgs e)
        {
            SeriesListPanel.Visibility = Visibility.Hidden;
            ResultsListPanel.Visibility = Visibility.Visible;
        }

        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            var results = App.Client.SearchTvShow(SearchTextBox.Text).Results;
            ResultsList.ItemsSource = results;
        }

        private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddSeriesButton.IsEnabled = true;
        }

        private void AddSeriesButton_Click(object sender, RoutedEventArgs e)
        {
            var temp = ResultsList.SelectedItem as TvShowBase;

            if (temp == null) return;
            seriesList.Add(SaveHandler.ReadFromId(null, temp.Id));
            SeriesList.ItemsSource = null;
            SeriesList.ItemsSource = seriesList;
        }

        private void BackButtonClick(object sender, RoutedEventArgs e)
        {
            SeriesListPanel.Visibility = Visibility.Visible;
            ResultsListPanel.Visibility = Visibility.Hidden;
        }

        private void MarkItemWatched(object sender, RoutedEventArgs e)
        {
            var selectedItem = SeriesTree.SelectedItem as ISeriesListItem;
            if (selectedItem == null) return;
            selectedItem.MarkWatched();
            SeriesTree.Items.Refresh();
            FindNewEpisodes();
            foreach (var s in seriesList)
            {
                s.UpdateIcon();
            }
        }

        private void SeriesTree_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem == null) return;
            treeViewItem.Focus();
            e.Handled = true;
        }

        static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        private void SeriesTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = SeriesTree.SelectedItem;
            if (selectedItem == null) return;
            switch (selectedItem.GetType().Name)
            {
                case "Series":
                    SeriesTree.ContextMenu = SeriesTree.Resources["SeriesContext"] as ContextMenu;
                    break;
                case "Season":
                    SeriesTree.ContextMenu = SeriesTree.Resources["SeasonContext"] as ContextMenu;
                    break;
                case "Episode":
                    SeriesTree.ContextMenu = SeriesTree.Resources["EpisodeContext"] as ContextMenu;
                    break;
            }
        }

        public static void ReloadList()
        {
            _main.SeriesList.Items.Refresh();
        }
    }

    interface ISeriesListItem
    {
        void MarkWatched();

    }

    public class Series : ISeriesListItem
    {
        public readonly TvShow Show;
        public ObservableCollection<Season> Seasons { get; set; }

        private static BitmapImage _orangeBullet, _whiteBullet;

        
        public Series(TvShow show)
        {
            Show = show;
            Seasons = new ObservableCollection<Season>();
            Name = show.Name;
            Id = show.Id;

            UpdateIcon();
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
                if (_orangeBullet == null) _orangeBullet = InitImage("../../res/bullet_orange.png");
                if (_whiteBullet == null) _whiteBullet = InitImage("../../res/bullet_white.png");

                var t = Watched();
                Source = (t ? _whiteBullet : _orangeBullet);
            });
            MainWindow.ReloadList();
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

    public class Season : ISeriesListItem
    {
        public readonly TvSeason SeasonData;
        public ObservableCollection<Episode> Episodes { get; set; }

        public Season(TvSeason season)
        {
            SeasonData = season;
            Episodes = new ObservableCollection<Episode>();
            SeasonNumber = season.SeasonNumber;
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

    public class Episode : ISeriesListItem
    {
        public TvEpisode EpisodeData;

        public ObservableCollection<string> Info { get; set; }
        public DateTime AirDate { get; set; }
        public int EpisodeNumber { get; set; }

        public Episode(TvEpisode episodeData)
        {
            EpisodeData = episodeData;
            Info = new ObservableCollection<string> {episodeData.Overview};
            AirDate = episodeData.AirDate;
            Name = episodeData.Name;
            EpisodeNumber = episodeData.EpisodeNumber;
            if (episodeData.Id != null) Id = (int)episodeData.Id;
            Watched = false;
        }

        public void MarkWatched()
        {
            Watched = true;
        }

        public string Name { get; set; }
        public int Id { get; set; }
        public bool Watched { get; set; }
        public Boolean IsSelected { get; set; }
        public Boolean IsExpanded { get; set; }
    }
}

public class SearchFilterConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return ((int)values[0] + " " + (String)values[1]); ;
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}