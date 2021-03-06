﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using RestSharp.Contrib;

namespace WhatsNew
{
    public partial class MainWindow
    {
        private readonly List<Series> loadedSeries = new List<Series>();
        private readonly ObservableCollection<Series> seriesList = new ObservableCollection<Series>();

        public MainWindow()
        {
            InitializeComponent();
            BuildTree();
        }

        private void BuildTree()
        {
            SeriesTree.ItemsSource = null;
            seriesList.Add(new Series("Loading..."));
            SeriesTree.ItemsSource = seriesList;
            DataContext = seriesList;

            var worker = new BackgroundWorker();
            worker.DoWork += LoadSeries;
            worker.RunWorkerCompleted += LoadComplete;
            worker.RunWorkerAsync();
        }

        private void LoadSeries(object sender, DoWorkEventArgs e)
        {
            foreach (var s in SaveHandler.ReadShows())
            {
                loadedSeries.Add(s);
            }
        }

        private void LoadComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            seriesList.Clear();
            foreach (var s in loadedSeries)
            {
                seriesList.Add(s);
            }

            if (seriesList.Count > 0)
            {
                SeriesTree.ItemsSource = new List<Series> {seriesList[0]};
            }
            SeriesList.ItemsSource = seriesList;

            FindNewEpisodes();
            SeriesList.Items.Refresh();
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
                    foreach (
                        var e in season.Episodes.Where(e => (!e.Watched && e.AirDate.CompareTo(DateTime.Today) < 0)))
                    {
                        var line = string.Format("{0} Season {1} - {2}", series.Name, season.SeasonNumber, e.Name);
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
            foreach (var s in seriesList)
            {
                s.UpdateIcon();
            }
            FindNewEpisodes();
            SeriesList.Items.Refresh();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
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
            SeriesList.Items.Refresh();
        }

        private void SeriesTree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem == null) return;
            treeViewItem.Focus();
            e.Handled = true;
        }

        private static TreeViewItem VisualUpwardSearch(DependencyObject source)
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
    }

    internal interface ISeriesListItem
    {
        void MarkWatched();
    }

    public class SearchFilterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return ((int) values[0] + " " + (string) values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}