using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    /// 
    [StructLayout(LayoutKind.Sequential)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public IntPtr iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };

    class Win32
    {
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0;    // 'Large icon
        public const uint SHGFI_SMALLICON = 0x1;    // 'Small icon

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SHGetFileInfo(string pszPath,
                                    uint dwFileAttributes,
                                    ref SHFILEINFO psfi,
                                    uint cbSizeFileInfo,
                                    uint uFlags);
    }


    public partial class MainWindow : Window
    {
        long timestamp = 0;
        readonly string saveDirectory;
        bool containerReduced = false;
        bool pinned = false;


        RowDefinition ROW_DEFINITION_TITLE = new RowDefinition()
        { Height = GridLength.Auto };
        RowDefinition ROW_DEFINITION_WRAP = new RowDefinition()
        { Height = new GridLength(1, GridUnitType.Star) };

        double CONTAINER_HEIGHT = 400;
        public MainWindow(List<List<string>> save_datas = null, string timestampText = "")
        {
            InitializeComponent();

            saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\DesktopContainer";
            if (!Directory.Exists(saveDirectory))
                Directory.CreateDirectory(saveDirectory);

            if (save_datas != null) // CONTAINER SAUVEGARDE
            {
                timestamp = int.Parse(timestampText);
                var options = save_datas[0];

                // Récupération du nom du container
                Container_Title.Text = options[0];
                MainContainer.Title = options[0];

                // Récupération de l'état (réduit/ouvert) du container
                containerReduced = options[1] == "True";

                // Récupération de la position de la barre de titre
                bottom_titlebar = options[2] == "True";

                // Récupération des dimensions du container
                List<string> sizeContainer = options[3].Split(";").ToList();
                MainContainer.Width = int.Parse(sizeContainer[0]);
                int savedContainerHeight = int.Parse(sizeContainer[1]);
                if (savedContainerHeight > 50)
                    CONTAINER_HEIGHT = savedContainerHeight;
                else
                    CONTAINER_HEIGHT = 400;
                MainContainer.Height = CONTAINER_HEIGHT;

                // Récupération des coordonnées du container
                List<string> posContainer = options[4].Split(";").ToList();
                MainContainer.Left = int.Parse(posContainer[0]);
                MainContainer.Top = int.Parse(posContainer[1]);

                AddItem(save_datas[1].ToArray());

                if (containerReduced)
                {
                    Wrap_Shortcut.Visibility = Visibility.Collapsed;
                    MainContainer.Height = 20;
                    MainContainer.ResizeMode = ResizeMode.NoResize;
                }
                if (bottom_titlebar)
                {
                    Grid_Container.RowDefinitions.Clear();
                    Grid_Container.RowDefinitions.Add(ROW_DEFINITION_WRAP);
                    Grid_Container.RowDefinitions.Add(ROW_DEFINITION_TITLE);
                    TitleBar.SetValue(Grid.RowProperty, 1);
                    Scroll_Container.SetValue(Grid.RowProperty, 0);
                    Container_Border.CornerRadius = new CornerRadius(10, 10, 0, 0);
                    TitleBar.Margin = new Thickness(0, 0, 15, 0);
                    Btn_Invert.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/images/switchTitleTop.png")));
                }

            } else // NOUVEAU CONTAINER
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }


        private void AddItem(string[] files)
        {
            foreach (string file in files)
            {
                if (File.Exists(file) || Directory.Exists(file))
                {
                    string filename;
                    if (File.Exists(file) && Path.GetFileNameWithoutExtension(file) != "")
                        filename = Path.GetFileNameWithoutExtension(file);
                    else if (File.Exists(file))
                        filename = Path.GetFileName(file);
                    else
                        filename = Path.GetFileName(file);

                    Grid item = new()
                    {
                        Height = 100,
                        Width = 100,
                        Margin = new Thickness(5),
                        Tag = file,
                        Cursor = Cursors.Hand,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    Wrap_Shortcut.Children.Add(item);

                    RowDefinition row1 = new()
                    {
                        Height = new GridLength(50)
                    };
                    item.RowDefinitions.Add(row1);
                    RowDefinition row2 = new()
                    {
                        Height = new GridLength(1, GridUnitType.Auto)
                    };
                    item.RowDefinitions.Add(row2);

                    Image icone = new()
                    {
                        Stretch = Stretch.Uniform,
                    };

                    FileAttributes attr = File.GetAttributes(file);

                    // Récupération de l'icône du dossier / fichier
                    IntPtr hImgSmall;    //the handle to the system image list
                    IntPtr hImgLarge;    //the handle to the system image list
                    string fName;        // 'the file name to get icon from
                    SHFILEINFO shinfo = new();

                    hImgSmall = Win32.SHGetFileInfo(file, 0, ref shinfo,
                            (uint)Marshal.SizeOf(shinfo),
                            Win32.SHGFI_ICON |
                            Win32.SHGFI_ICON);
                    var item_icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(shinfo.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                    icone.Source = item_icon;
                    item.Children.Add(icone);

                    TextBlock name = new()
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

                    ContextMenu itemOptions = new();
                    MenuItem itemDelete = new()
                    {
                        Header = "_Supprimer",
                    };
                    itemDelete.Click += (sender, args) =>
                    {
                        Item_Delete(item);
                    };
                    itemOptions.Items.Add(itemDelete);
                    MenuItem itemMoveUp = new()
                    {
                        Header = "_<- Avancer",
                    };
                    itemMoveUp.Click += (sender, args) =>
                    {
                        Item_MoveUp(item);
                    };
                    itemOptions.Items.Add(itemMoveUp);
                    MenuItem itemMoveDown = new()
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

        public static void OpenWithDefaultProgram(string path)
        {
            using Process fileopener = new();

            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            fileopener.Start();
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

        public void Save_Container()
        {
            if (Wrap_Shortcut.Children.Count > 0)
            {
                List<List<string>> save_datas = new();
                List<string> options = new();
                List<string> items = new();

                options.Add(Container_Title.Text);
                options.Add(containerReduced.ToString());
                options.Add(bottom_titlebar.ToString());

                if (!containerReduced)
                    CONTAINER_HEIGHT = MainContainer.Height;

                int[] sizeContainer = { (int)MainContainer.Width, (int)CONTAINER_HEIGHT };
                options.Add(sizeContainer[0].ToString() + ";" + sizeContainer[1].ToString());

                string posContainer = MainContainer.PointToScreen(new Point(0, 0)).ToString();
                options.Add(posContainer);

                var elems = Wrap_Shortcut.Children;
                foreach (var item in elems)
                {
                    Grid grid = item as Grid;
                    items.Add(grid.Tag.ToString());
                }

                save_datas.Add(options);
                save_datas.Add(items);

                var json = JsonSerializer.Serialize(save_datas);

                File.WriteAllText(saveDirectory + @"\container" + timestamp + ".json", json);
            }
            else
                File.Delete(saveDirectory + @"\container" + timestamp + ".json");
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

        bool optionsOpened = false;
        private void Btn_ContainerOptions_Click(object sender, RoutedEventArgs e)
        {
            if (optionsOpened)
            {
                Btn_Restart.Visibility = Visibility.Collapsed;
                Btn_Pin.Visibility = Visibility.Collapsed;
                Btn_Invert.Visibility = Visibility.Collapsed;
                Btn_NewContainer.Visibility = Visibility.Collapsed;
                Btn_CloseContainer.Visibility = Visibility.Collapsed;
            }
            else
            {
                Btn_Restart.Visibility = Visibility.Visible;
                Btn_Pin.Visibility = Visibility.Visible;
                Btn_Invert.Visibility= Visibility.Visible;
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

        private void Btn_Pin_Click(object sender, RoutedEventArgs e)
        {
            MainContainer.Topmost = !MainContainer.Topmost;
            pinned = MainContainer.Topmost;
        }

        bool bottom_titlebar = false;
        private void Btn_Invert_Click(object sender, RoutedEventArgs e)
        {
            bottom_titlebar = !bottom_titlebar;
            Grid_Container.RowDefinitions.Clear();

            if (bottom_titlebar)
            {
                Grid_Container.RowDefinitions.Add(ROW_DEFINITION_WRAP);
                Grid_Container.RowDefinitions.Add(ROW_DEFINITION_TITLE);
                TitleBar.SetValue(Grid.RowProperty, 1);
                Scroll_Container.SetValue(Grid.RowProperty, 0);
                Container_Border.CornerRadius = new CornerRadius(10,10,0,0);
                TitleBar.Margin = new Thickness(0,0,15,0);
                Btn_Invert.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/images/switchTitleTop.png")));
            }
            else
            {
                Grid_Container.RowDefinitions.Add(ROW_DEFINITION_TITLE);
                Grid_Container.RowDefinitions.Add(ROW_DEFINITION_WRAP);
                TitleBar.SetValue(Grid.RowProperty, 0);
                Scroll_Container.SetValue(Grid.RowProperty, 1);
                Container_Border.CornerRadius = new CornerRadius(0, 0, 10, 10);
                TitleBar.Margin = new Thickness(0);
                Btn_Invert.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/images/switchTitleBottom.png")));
            }
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

        private void MainContainer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount != 2)
                this.DragMove();
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
            Btn_Invert.Visibility = Visibility.Collapsed;
            Btn_NewContainer.Visibility = Visibility.Collapsed;
            Btn_CloseContainer.Visibility = Visibility.Collapsed;
            optionsOpened = false;
            if(!pinned)
                Btn_Pin.Visibility = Visibility.Collapsed;
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

        private void ContainerReduce()
        {
            if (containerReduced)
            {
                Wrap_Shortcut.Visibility = Visibility.Visible;
                MainContainer.Height = CONTAINER_HEIGHT;
                MainContainer.ResizeMode = ResizeMode.CanResizeWithGrip;
                if (bottom_titlebar)
                {
                    var location = MainContainer.PointToScreen(new Point(0, 0));
                    MainContainer.Left = location.X;
                    MainContainer.Top = location.Y - MainContainer.Height + 20;
                }
            }
            else
            {
                if (bottom_titlebar)
                {
                    var location = MainContainer.PointToScreen(new Point(0, 0));
                    MainContainer.Left = location.X;
                    MainContainer.Top = location.Y + MainContainer.Height - 20;
                }
                Wrap_Shortcut.Visibility = Visibility.Collapsed;
                MainContainer.Height = 20;
                MainContainer.ResizeMode = ResizeMode.NoResize;
            }
            containerReduced = !containerReduced;
        }

        private static void Hover_Item(Grid item, bool hovering)
        {
            if(hovering)
                item.Background = new SolidColorBrush(Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF));
            else
                item.Background = new SolidColorBrush(Color.FromArgb(0x00, 0, 0, 0));
        }

        private void MainContainer_Closing(object sender, CancelEventArgs e)
        {
            Save_Container();
        }
    }
}
