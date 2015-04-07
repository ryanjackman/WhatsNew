using System.Windows;

namespace WhatsNew
{
    public partial class App
    {
        void App_Startup(object sender, StartupEventArgs e)
        {
            var window = new MainWindow();
            window.Show();
        }
    }
}
