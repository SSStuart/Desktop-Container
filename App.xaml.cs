using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace Desktop_Container
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\DesktopContainer";
            if (!Directory.Exists(saveDirectory))
                Directory.CreateDirectory(saveDirectory);

            List<string> savedContainer = Directory.GetFiles(saveDirectory, "*.json").ToList();

            foreach (string container in savedContainer)
            {
                StreamReader r = new StreamReader(container);
                string json = r.ReadToEnd();
                List<string> items = JsonSerializer.Deserialize<List<string>>(json);
                r.Close();

                string timestampText = container.Split("\\")[^1].Split("container")[1].Split(".")[0];

                MainWindow newContainer = new MainWindow(items, timestampText);
                newContainer.Show();
            }
        }

        void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            var main = App.Current.MainWindow as MainWindow;
            main.Save_Container();
        }

        void App_Restart()
        {
            Process.Start(Process.GetCurrentProcess().MainModule.FileName);
            Application.Current.Shutdown();
        }
    }
}
