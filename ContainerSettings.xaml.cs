using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Desktop_Container
{
    /// <summary>
    /// Logique d'interaction pour ContainerSettings.xaml
    /// </summary>
    public partial class ContainerSettings : Window
    {
        long ownerTimestamp;
        List<string> positionsContainers = new();

        readonly string saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\DesktopContainer";
        public ContainerSettings(long ownerTimestampPassed)
        {
            InitializeComponent();

            ownerTimestamp = ownerTimestampPassed;

            // Récupération des fichiers Container pour le dropdown
            string saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\DesktopContainer";
            if (!Directory.Exists(saveDirectory))
                Directory.CreateDirectory(saveDirectory);

            List<string> savedContainer = Directory.GetFiles(saveDirectory, "*.json").ToList();
            List<string> backedUpContainer = Directory.GetFiles(saveDirectory, "*.json.backup").ToList();
            List<string> activeContainerTS = new();

            foreach (string container in savedContainer)
            {
                StreamReader r = new(container);
                string json = r.ReadToEnd();
                if (json != "")
                {
                    List<List<string>> save_datas = JsonSerializer.Deserialize<List<List<string>>>(json);
                    r.Close();

                    string name = save_datas[0][0];
                    positionsContainers.Add(save_datas[0][5]);
                    string timestampText = container.Split("\\")[^1].Split("container")[1].Split(".")[0];
                    CmbBox_ContainersList.Items.Add(name + " [" + timestampText + "]");

                    activeContainerTS.Add(timestampText);
                }
            }

            foreach (string container in backedUpContainer)
            {
                StreamReader r = new(container);
                string json = r.ReadToEnd();
                if (json != "")
                {
                    List<List<string>> save_datas = JsonSerializer.Deserialize<List<List<string>>>(json);
                    r.Close();

                    string name = save_datas[0][0];
                    string timestampText = container.Split("\\")[^1].Split("container")[1].Split(".")[0];
                    if (!activeContainerTS.Contains(timestampText))
                    {
                        ComboBoxItem item = new()
                        {
                            Foreground = Brushes.Firebrick,
                            Content = "(BACKUP) " + name + " [" + timestampText + "]"
                        };
                        CmbBox_ContainersList.Items.Add(item);
                    }
                }
            }
        }

        private void BtnCloseAll_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Btn_CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Process.GetCurrentProcess().MainModule.FileName);
            Application.Current.Shutdown();
        }

        int containerIndex;
        string selectedTimestamp;
        private void CmbBox_ContainersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedValue = CmbBox_ContainersList.SelectedItem.ToString();
            selectedTimestamp = selectedValue.Split("[")[1].Replace("]","");

            if (selectedValue.Contains("(BACKUP)"))
            {
                btnBackup.IsEnabled = false;
            }
            else
            {
                btnBackup.IsEnabled = true;
            }

            if (File.Exists(saveDirectory + @"\container" + selectedTimestamp + ".json.backup"))
            {
                btnRestore.IsEnabled = true;
            }
            else
            {
                btnRestore.IsEnabled = false;
            }
            containerIndex = CmbBox_ContainersList.SelectedIndex;

            Update_Checkboxes();
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            File.Copy(saveDirectory + @"\container" + selectedTimestamp + ".json", saveDirectory + @"\container" + selectedTimestamp + ".json.backup", true);
        }

        private void BtnOpenSaveLocation_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", saveDirectory);
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            File.Delete(saveDirectory + @"\container" + selectedTimestamp + ".json");
            File.Copy(saveDirectory + @"\container" + selectedTimestamp + ".json.backup", saveDirectory + @"\container" + selectedTimestamp + ".json", true);

            btnReload.Background = Brushes.LightGreen;
        }

        private void Radio_AnchorTop_Checked(object sender, RoutedEventArgs e)
        {
            PositionAnchorEdit();
        }

        private void Radio_AnchorLeft_Checked(object sender, RoutedEventArgs e)
        {
            PositionAnchorEdit();
        }

        private void Radio_AnchorRight_Checked(object sender, RoutedEventArgs e)
        {
            PositionAnchorEdit();
        }

        private void Radio_AnchorBottom_Checked(object sender, RoutedEventArgs e)
        {
            PositionAnchorEdit();
        }

        private void PositionAnchorEdit()
        {
            int positionHori = 0;
            int positionVert = 0;
            if (Radio_AnchorLeft.IsChecked == true)
            {
                positionHori = (int)(Owner as MainWindow).PointToScreen(new Point(0, 0)).X;
            }
            else if (Radio_AnchorRight.IsChecked == true)
            {
                positionHori = -(int)(SystemParameters.PrimaryScreenWidth - (Owner as MainWindow).PointToScreen(new Point((Owner as MainWindow).Width, 0)).X);
            }
            if (Radio_AnchorTop.IsChecked == true)
            {
                positionVert = (int)(Owner as MainWindow).PointToScreen(new Point(0, 0)).Y;
            }
            else if (Radio_AnchorBottom.IsChecked == true)
            {
                positionVert = -(int)(SystemParameters.PrimaryScreenHeight - (Owner as MainWindow).PointToScreen(new Point(0, (Owner as MainWindow).Height)).Y);
            }


            string position = positionHori + "," + positionVert;


            StreamReader r = new(saveDirectory + @"\container" + selectedTimestamp + ".json");
            string json = r.ReadToEnd();
            if (json != "")
            {

                List<List<string>> save_datas = JsonSerializer.Deserialize<List<List<string>>>(json);
                r.Close();

                save_datas[0][5] = position;

                var jsonNew = JsonSerializer.Serialize(save_datas);

                File.WriteAllText(saveDirectory + @"\container" + selectedTimestamp + ".json", jsonNew);
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            ItemCollection listContainer = CmbBox_ContainersList.Items;
            foreach (string container in listContainer)
            {
                if (container.Contains(ownerTimestamp.ToString()))
                {
                    CmbBox_ContainersList.SelectedItem = container;
                    break;
                }
            }

            Update_Checkboxes();
        }

        private void Update_Checkboxes()
        {
            if(CmbBox_ContainersList.SelectedIndex != -1)
            {
                string posX = positionsContainers[containerIndex].Split(",")[0];
                string posY = positionsContainers[containerIndex].Split(",")[1];
                if (posX[0] == '-')
                    Radio_AnchorRight.IsChecked = true;
                else
                    Radio_AnchorLeft.IsChecked = true;
                if (posY[0] == '-')
                    Radio_AnchorBottom.IsChecked = true;
                else
                    Radio_AnchorTop.IsChecked = true;
            }
            else
            {
                Radio_AnchorTop.IsChecked = false;
                Radio_AnchorLeft.IsChecked = false;
                Radio_AnchorRight.IsChecked = false;
                Radio_AnchorBottom.IsChecked = false;
            }
        }
    }
}