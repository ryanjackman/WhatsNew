using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using RestSharp.Contrib;

namespace WhatsNew
{
    public partial class MainWindow
    {
        private static MainWindow _main;

        private readonly ObservableCollection<Series> seriesList = new ObservableCollection<Series>();

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
            var escapedSearch = HttpUtility.UrlEncode(SearchTextBox.Text);

            var results = RequestHandler.MakeCall("search/tv", escapedSearch);
            var jResults = results["results"].ToArray();
            ResultsList.Items.Clear();
            foreach (var jResult in jResults)
            {
                ResultsList.Items.Add(new Series(JObject.Parse(jResult.ToString())));
            }
            
        }

        private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddSeriesButton.IsEnabled = true;
        }

        private void AddSeriesButton_Click(object sender, RoutedEventArgs e)
        {
            var temp = ResultsList.SelectedItem as Series;

            if (temp == null) return;
            seriesList.Add(SaveHandler.ReadFromJson(null, temp.Id));
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

    public class SearchFilterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return ((int)values[0] + " " + (String)values[1]);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

