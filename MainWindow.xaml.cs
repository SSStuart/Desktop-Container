using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Desktop_Container
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        long timestamp = 0;
        string saveDirectory;
        bool containerReduced = false;
        bool pinned = false;

        double CONTAINER_HEIGHT = 400;
        public MainWindow(List<string> items = null, string timestampText = "")
        {
            InitializeComponent();

            saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\DesktopContainer";
            if (!Directory.Exists(saveDirectory))
                Directory.CreateDirectory(saveDirectory);

            if (items != null) // CONTAINER SAUVEGARDE
            {
                timestamp = int.Parse(timestampText);
                // Récupération du nom du container
                Container_Title.Text = items[0];
                MainContainer.Title = items[0];
                items.RemoveAt(0);

                // Récupération de l'état (réduit/ouvert) du container
                containerReduced = items[0] == "True";
                items.RemoveAt(0);

                // Récupération des dimensions du container
                List<string> sizeContainer = items[0].Split(";").ToList();
                MainContainer.Width = int.Parse(sizeContainer[0]);
                int savedContainerHeight = int.Parse(sizeContainer[1]);
                if (savedContainerHeight > 50)
                    CONTAINER_HEIGHT = savedContainerHeight;
                else
                    CONTAINER_HEIGHT = 400;
                MainContainer.Height = CONTAINER_HEIGHT;

                items.RemoveAt(0);

                // Récupération des coordonnées du container
                List<string> posContainer = items[0].Split(";").ToList();
                MainContainer.Left = int.Parse(posContainer[0]);
                MainContainer.Top = int.Parse(posContainer[1]);
                items.RemoveAt(0);

                AddItem(items.ToArray());

                if (containerReduced)
                    ContainerReduce(true);
            } else // NOUVEAU CONTAINER
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

        }

        private void AddItem(string[] files)
        {
            foreach (string file in files)
            {
                if (File.Exists(file) || Directory.Exists(file))
                {
                    string filename;
                    if (File.Exists(file))
                        filename = Path.GetFileNameWithoutExtension(file);
                    else
                        filename = Path.GetFileName(file);

                    Grid item = new Grid
                    {
                        Height = 100,
                        Width = 100,
                        Margin = new Thickness(5),
                        Tag = file,
                        Cursor = Cursors.Hand,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    Wrap_Shortcut.Children.Add(item);

                    RowDefinition row1 = new RowDefinition
                    {
                        Height = new GridLength(50)
                    };
                    item.RowDefinitions.Add(row1);
                    RowDefinition row2 = new RowDefinition
                    {
                        Height = new GridLength(1, GridUnitType.Auto)
                    };
                    item.RowDefinitions.Add(row2);

                    System.Windows.Controls.Image icone = new System.Windows.Controls.Image
                    {
                        Stretch = Stretch.Uniform,
                    };

                    FileAttributes attr = File.GetAttributes(file);

                    //detect whether its a directory or file
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        icone.Source = new BitmapImage(new Uri("/Desktop Container;component/images/directory.png", UriKind.Relative));
                    }
                    else
                    {
                        // RECUPERATION DE L'ICONE DU FICHIER
                        BitmapSource fileIcon;
                        using (System.Drawing.Icon sysicon = System.Drawing.Icon.ExtractAssociatedIcon(file))
                        {
                            // This new call in WPF finally allows us to read/display 32bit Windows file icons!
                            fileIcon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                sysicon.Handle,
                                System.Windows.Int32Rect.Empty,
                                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                        }
                        icone.Source = fileIcon;
                    }

                    item.Children.Add(icone);

                    TextBlock name = new TextBlock
                    {
                        Text = filename,
                        Foreground = new SolidColorBrush(Colors.White),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                    };
                    item.Children.Add(name);
                    Grid.SetRow(name, 1);

                    item.MouseLeftButtonDown += (sender, args) =>
                    {
                        OpenWithDefaultProgram(item.Tag.ToString());
                    };

                    ContextMenu itemOptions = new ContextMenu();
                    MenuItem itemDelete = new MenuItem
                    {
                        Header = "_Supprimer",
                    };
                    itemDelete.Click += (sender, args) =>
                    {
                        Item_Delete(item);
                    };
                    itemOptions.Items.Add(itemDelete);
                    MenuItem itemMoveUp = new MenuItem
                    {
                        Header = "_<- Avancer",
                    };
                    itemMoveUp.Click += (sender, args) =>
                    {
                        Item_MoveUp(item);
                    };
                    itemOptions.Items.Add(itemMoveUp);
                    MenuItem itemMoveDown = new MenuItem
                    {
                        Header = "-_> Reculer",
                    };
                    itemMoveDown.Click += (sender, args) =>
                    {
                        Item_MoveDown(item);
                    };
                    itemOptions.Items.Add(itemMoveDown);

                    item.ContextMenu = itemOptions;

                    item.MouseRightButtonUp += (sender, args) =>
                    {
                        Save_Container();
                    };

                    item.MouseEnter += (sender, args) =>
                    {
                        Hover_Item(item, true);
                    };

                    item.MouseLeave += (sender, args) =>
                    {
                        Hover_Item(item, false);
                    };
                }
                else
                {
                    MessageBox.Show(file + "\r\nFichier/Dossier introuvable\r\nProbablement supprimé ou déplacé", "Fichier manquant", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void MainContainer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount != 2)
                this.DragMove();
        }

        private void Btn_NewContainer_Click(object sender, RoutedEventArgs e)
        {
            MainWindow newContainer = new MainWindow(null, "");
            newContainer.Show();
        }

        private void Btn_CloseContainer_Click(object sender, RoutedEventArgs e)
        {
            MainContainer.Close();
        }

        private void MainContainer_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                AddItem(files);
            }
            Save_Container();
        }

        public static void OpenWithDefaultProgram(string path)
        {
            using Process fileopener = new Process();

            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            fileopener.Start();
        }

        private void Item_Delete(Grid item)
        {
            Wrap_Shortcut.Children.Remove(item);
        }

        private void Item_MoveUp(Grid item)
        {
            int index = Wrap_Shortcut.Children.IndexOf(item);
            if (index != 0)
            {
                Wrap_Shortcut.Children.Remove(item);
                Wrap_Shortcut.Children.Insert(index - 1, item);
            }
        }

        private void Item_MoveDown(Grid item)
        {
            int index = Wrap_Shortcut.Children.IndexOf(item);
            if (index != Wrap_Shortcut.Children.Count)
            {
                Wrap_Shortcut.Children.Remove(item);
                Wrap_Shortcut.Children.Insert(index + 1, item);
            }
        }

        private void MainContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            Container_Border.Background = new SolidColorBrush(Color.FromArgb(0x88, 0, 0, 0));
            Btn_ContainerOptions.Visibility = Visibility.Visible;
        }

        private void MainContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            Container_Border.Background = new SolidColorBrush(Color.FromArgb(0x50, 0, 0, 0));
            Btn_ContainerOptions.Visibility = Visibility.Collapsed;
            Btn_Restart.Visibility = Visibility.Collapsed;
            Btn_NewContainer.Visibility = Visibility.Collapsed;
            Btn_CloseContainer.Visibility = Visibility.Collapsed;
            optionsOpened = false;
            if(!pinned)
                Btn_Pin.Visibility = Visibility.Collapsed;
        }

        private void Hover_Item(Grid item, bool hovering)
        {
            if(hovering)
                item.Background = new SolidColorBrush(Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF));
            else
                item.Background = new SolidColorBrush(Color.FromArgb(0x00, 0, 0, 0));
        }

        public void Save_Container()
        {
            List<string> datas = new List<string>();

            datas.Add(Container_Title.Text);
            datas.Add(containerReduced.ToString());

            if (!containerReduced)
                CONTAINER_HEIGHT = MainContainer.Height;

            int[] sizeContainer = { (int)MainContainer.Width, (int)CONTAINER_HEIGHT };
            datas.Add(sizeContainer[0].ToString() + ";" + sizeContainer[1].ToString());

            string posContainer = MainContainer.PointToScreen(new Point(0, 0)).ToString();
            datas.Add(posContainer);

            var elems = Wrap_Shortcut.Children;
            foreach (var item in elems)
            {
                Grid grid = item as Grid;
                datas.Add(grid.Tag.ToString());
            }

            var json = JsonSerializer.Serialize(datas);
            
            File.WriteAllText(saveDirectory+@"\container"+timestamp+".json", json);
        }

        private void MainContainer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Save_Container();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ContainerReduce();
            }
        }

        private void ContainerReduce(bool initialisation = false)
        {
            if (initialisation) {
                Wrap_Shortcut.Visibility = Visibility.Collapsed;
                MainContainer.Height = 20;
                MainContainer.ResizeMode = ResizeMode.NoResize;
            } else
            {
                if (containerReduced)
                {
                    Wrap_Shortcut.Visibility = Visibility.Visible;
                    MainContainer.Height = CONTAINER_HEIGHT;
                    MainContainer.ResizeMode = ResizeMode.CanResizeWithGrip;
                    containerReduced = false;
                }
                else
                {
                    Wrap_Shortcut.Visibility = Visibility.Collapsed;
                    MainContainer.Height = 20;
                    MainContainer.ResizeMode = ResizeMode.NoResize;
                    containerReduced = true;
                }
            }
        }

        bool optionsOpened = false;
        private void Btn_ContainerOptions_Click(object sender, RoutedEventArgs e)
        {
            if (optionsOpened)
            {
                Btn_Restart.Visibility = Visibility.Collapsed;
                Btn_Pin.Visibility = Visibility.Collapsed;
                Btn_NewContainer.Visibility = Visibility.Collapsed;
                Btn_CloseContainer.Visibility = Visibility.Collapsed;
            } else
            {
                Btn_Restart.Visibility = Visibility.Visible;
                Btn_Pin.Visibility = Visibility.Visible;
                Btn_NewContainer.Visibility = Visibility.Visible;
                Btn_CloseContainer.Visibility = Visibility.Visible;
            }
            optionsOpened = !optionsOpened;
        }

        private void Btn_Restart_Click(object sender, RoutedEventArgs e)
        {
            Save_Container();
            Process.Start(Process.GetCurrentProcess().MainModule.FileName);
            Application.Current.Shutdown();
        }

        private void MainContainer_Closing(object sender, CancelEventArgs e)
        {
            Save_Container();
            if (File.Exists(saveDirectory + @"\container" + timestamp + ".json"))
            {
                if (Wrap_Shortcut.Children.Count == 0)
                    File.Delete(saveDirectory + @"\container" + timestamp + ".json");
            }
        }

        private void Btn_Pin_Click(object sender, RoutedEventArgs e)
        {
            MainContainer.Topmost = !MainContainer.Topmost;
            pinned = MainContainer.Topmost;
        }
    }
}
