using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TMDbLib.Client;
using TMDbLib.Objects.TvShows;

namespace WhatsNew
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly static string apiKey = "b861bd20b4d5e1044703f7fb0b64f0b7";
        public static TMDbClient client;

        void App_Startup(object sender, StartupEventArgs e)
        {
            client = new TMDbClient(apiKey);

            MainWindow window = new MainWindow();
            window.Show();
        }


    }
}
