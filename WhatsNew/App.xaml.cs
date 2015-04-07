using System.Windows;
using TMDbLib.Client;

namespace WhatsNew
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private const string ApiKey = "b861bd20b4d5e1044703f7fb0b64f0b7";
        public static TMDbClient Client;

        void App_Startup(object sender, StartupEventArgs e)
        {
            Client = new TMDbClient(ApiKey);

            var window = new MainWindow();
            window.Show();
        }


    }
}
