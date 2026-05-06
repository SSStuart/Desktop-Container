using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            // Only allow one instance of the application
            Process thisProc = Process.GetCurrentProcess();
            if (Process.GetProcessesByName(thisProc.ProcessName).Length > 1)
            {
                int launchedInstanceId = Process.GetProcessesByName(thisProc.ProcessName)[0].Id;
                MessageBoxResult userChoice = MessageBox.Show("  Another Desktop Container instance is already running.\r\n\r\n  Do you want to close all instances?", "The app is already running", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                if (userChoice == MessageBoxResult.OK)
                {
                    // Close all instances
                    Process.GetProcessById(launchedInstanceId).Kill();
                }
                Application.Current.Shutdown();
                return;
            }

            string saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\DesktopContainer";
            if (!Directory.Exists(saveDirectory))
                Directory.CreateDirectory(saveDirectory);

            List<string> savedContainer = [.. Directory.GetFiles(saveDirectory, "*.json")];

            foreach (string container in savedContainer)
            {
                StreamReader r = new(container);
                string json = r.ReadToEnd();
                r.Close();
                if(json != "")
                {
                    SaveDatas saveDatas;
                    if (json.StartsWith("[["))
                    {
                        saveDatas = MigrateOldSaveFormat(JsonSerializer.Deserialize<List<List<string>>>(json) ?? [], container);
                    } else
                    {
                        saveDatas = JsonSerializer.Deserialize<SaveDatas>(json) ?? new SaveDatas();
                    }

                    string timestampText = container.Split("\\")[^1].Split("container")[1].Split(".")[0];

                    MainWindow newContainer = new(saveDatas, timestampText);
                    newContainer.Show();
                }
            }

            if (savedContainer.Count == 0)
            {
                MainWindow emptyContainer = new();
                emptyContainer.Show();
            }
                
        }

        private static SaveDatas MigrateOldSaveFormat(List<List<string>> save_datas, string filepath)
        {
            SaveDatas newSaveDatas = new()
            {
                Title = save_datas[0][0],
                Reduced = save_datas[0][1] == "True",
                BottomTitleBar = save_datas[0][2] == "True",
                Color = save_datas[0][3],
                Size = [int.Parse(save_datas[0][4].Split(";")[0]), int.Parse(save_datas[0][4].Split(";")[1])],
                Position = [int.Parse(save_datas[0][5].Split(",")[0]), int.Parse(save_datas[0][5].Split(",")[1])],
                Files = save_datas[1],
                LinkedDirectory = save_datas[2].Count > 0 ? save_datas[2][0] : null
            };


            string oldSavePath = Path.GetDirectoryName(filepath);
            string oldSaveFilename = Path.GetFileName(filepath);
            File.Copy(filepath, oldSavePath + "\\" + oldSaveFilename + ".migratebackup");
            File.WriteAllText(filepath, JsonSerializer.Serialize(newSaveDatas));

            return newSaveDatas;
        }

        void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            //var main = App.Current.MainWindow as MainWindow;
            //main.Save_Container();
        }
    }
}
