﻿using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

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
        // Hiding the Container from Alt+Tab
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private const int GWL_EX_STYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000, WS_EX_TOOLWINDOW = 0x00000080;
        private void MainContainer_Loaded(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(this).Handle;
            SetWindowLong(helper, GWL_EX_STYLE, (GetWindowLong(helper, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);
        }
        // =================================

        long timestamp = 0;
        readonly string saveDirectory;
        bool containerReduced = false;
        bool pinned = false;
        string linkedDir = "";
        Brush container_color = (SolidColorBrush)new BrushConverter().ConvertFrom("#4C202020");
        bool posAnchoredRight, posAnchoredBottom = false;
        int positionHori, positionVert;


        readonly RowDefinition ROW_DEFINITION_TITLE = new()
        { Height = GridLength.Auto };
        readonly RowDefinition ROW_DEFINITION_PALETTE = new()
        { Height = GridLength.Auto };
        readonly RowDefinition ROW_DEFINITION_WRAP = new()
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

                // Récupération de la couleur d'arrière plan
                container_color = (SolidColorBrush)new BrushConverter().ConvertFrom(options[3]);

                // Récupération des dimensions du container
                List<string> sizeContainer = options[4].Split(";").ToList();
                MainContainer.Width = int.Parse(sizeContainer[0]);
                int savedContainerHeight = int.Parse(sizeContainer[1]);
                if (savedContainerHeight > 50)
                    CONTAINER_HEIGHT = savedContainerHeight;
                else
                    CONTAINER_HEIGHT = 400;
                MainContainer.Height = CONTAINER_HEIGHT;

                // Récupération des coordonnées du container
                string[] posContainer = options[5].Split(",");
                int posX = int.Parse(posContainer[0]);
                int posY = int.Parse(posContainer[1]);
                if (posX < 0)
                {
                    posX = (int)SystemParameters.PrimaryScreenWidth + posX - (int)MainContainer.Width;
                    posAnchoredRight = true;
                }
                if (posY < 0)
                {
                    posY = (int)SystemParameters.PrimaryScreenHeight + posY;
                    if (!containerReduced)
                        posY -= (int)MainContainer.Height;
                    else
                        posY -= 20;
                    posAnchoredBottom = true;
                }
                MainContainer.Left = posX;
                MainContainer.Top = posY;

                var directories = save_datas[2];
                if (directories.Count > 0)
                {
                    string directory = directories[0];
                    Link_Container(true, directory);
                    
                    if (Directory.Exists(directory))
                    {
                        AddItem(Directory.GetDirectories(directory));
                        AddItem(Directory.GetFiles(directory));
                    }
                }

                AddItem(save_datas[1].ToArray());

                if (containerReduced)
                {
                    Wrap_Shortcut.Visibility = Visibility.Collapsed;
                    MainContainer.Height = 20;
                    MainContainer.ResizeMode = ResizeMode.NoResize;
                    Btn_Color.Visibility = Visibility.Collapsed;
                    Color_Palette.Visibility = Visibility.Collapsed;
                }
                if (bottom_titlebar)
                {
                    Grid_Container.RowDefinitions.Clear();
                    Grid_Container.RowDefinitions.Add(ROW_DEFINITION_WRAP);
                    Grid_Container.RowDefinitions.Add(ROW_DEFINITION_PALETTE);
                    Grid_Container.RowDefinitions.Add(ROW_DEFINITION_TITLE);
                    TitleBar.SetValue(Grid.RowProperty, 2);
                    Color_Palette.SetValue(Grid.RowProperty, 1);
                    Scroll_Container.SetValue(Grid.RowProperty, 0);
                    Container_Border.CornerRadius = new CornerRadius(10, 10, 0, 0);
                    TitleBar.Margin = new Thickness(0, 0, 15, 0);
                    Btn_Invert.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/images/switchTitleTop.png")));
                }

                Container_Border.Background = container_color;

                Btn_Settings.IsEnabled = true;
            }
            else
            {// NOUVEAU CONTAINER
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                Empty_Container_Text.Visibility = Visibility.Visible;
            }
        }

        private void AddItem(string[] files)
        {
            foreach (string file in files)
            {
                if ((File.Exists(file) && Path.GetFileName(file) != "desktop.ini")|| Directory.Exists(file) )
                {
                    Empty_Container_Text.Visibility = Visibility.Collapsed;

                    string filename;
                    if (File.Exists(file) && Path.GetFileNameWithoutExtension(file) != "")
                        filename = Path.GetFileNameWithoutExtension(file);
                    else if (File.Exists(file))
                        filename = Path.GetFileName(file);
                    else
                        filename = Path.GetFileName(file);

                    Border roundedBorder = new()
                    {
                        CornerRadius = new CornerRadius(10),
                        Margin = new Thickness(2),
                        Cursor = Cursors.Hand,
                        Tag = file,
                        ToolTip = file,
                        Background = Brushes.Transparent,
                    };
                    Wrap_Shortcut.Children.Add(roundedBorder);

                    Grid item = new()
                    {
                        MaxHeight = 100,
                        Width = 100,
                        Margin = new Thickness(5),
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    roundedBorder.Child = item;

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

                    roundedBorder.MouseLeftButtonDown += (sender, args) =>
                    {
                        OpenWithDefaultProgram(roundedBorder.Tag.ToString());
                    };

                    ContextMenu itemOptions = new();
                    MenuItem itemDelete = new()
                    {
                        Header = "_Delete",
                        Icon = new Image
                        {
                            Source = new BitmapImage(new Uri("pack://application:,,,/images/delete.png")),
                            Height = 15,
                            Width = 15,
                        }
                    };
                    itemDelete.Click += (sender, args) =>
                    {
                        Item_Delete(roundedBorder);
                    };
                    itemOptions.Items.Add(itemDelete);
                    MenuItem itemMoveUp = new()
                    {
                        Header = "Move _Left",
                        Icon = new Image
                        {
                            Source = new BitmapImage(new Uri("pack://application:,,,/images/moveLeft.png")),
                            Height = 15,
                            Width = 15,
                        }
                    };
                    itemMoveUp.Click += (sender, args) =>
                    {
                        Item_MoveUp(roundedBorder);
                    };
                    itemOptions.Items.Add(itemMoveUp);
                    MenuItem itemMoveDown = new()
                    {
                        Header = "Move _Right",
                        Icon = new Image
                        {
                            Source = new BitmapImage(new Uri("pack://application:,,,/images/moveRight.png")),
                            Height = 15,
                            Width = 15,
                        }
                    };
                    itemMoveDown.Click += (sender, args) =>
                    {
                        Item_MoveDown(roundedBorder);
                    };
                    itemOptions.Items.Add(itemMoveDown);
                    MenuItem deleteAll = new()
                    {
                        Header = "_Empty Container",
                        Icon = new Image
                        {
                            Source = new BitmapImage(new Uri("pack://application:,,,/images/deleteAll.png")),
                            Height = 15,
                            Width = 15,
                        }
                    };
                    deleteAll.Click += (sender, args) =>
                    {
                        Delete_All();
                    };
                    itemOptions.Items.Add(deleteAll);


                    roundedBorder.ContextMenu = itemOptions;

                    roundedBorder.MouseRightButtonUp += (sender, args) =>
                    {
                        Save_Container();
                    };

                    roundedBorder.MouseEnter += (sender, args) =>
                    {
                        Hover_Item(roundedBorder, true);
                    };

                    roundedBorder.MouseLeave += (sender, args) =>
                    {
                        Hover_Item(roundedBorder, false);
                    };
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
                List<string> directories = new();

                options.Add(Container_Title.Text);
                options.Add(containerReduced.ToString());
                options.Add(bottom_titlebar.ToString());
                options.Add(container_color.ToString());

                if (!containerReduced)
                    CONTAINER_HEIGHT = MainContainer.Height;

                int[] sizeContainer = { (int)MainContainer.Width, (int)CONTAINER_HEIGHT };
                options.Add(sizeContainer[0].ToString() + ";" + sizeContainer[1].ToString());

                if (!posAnchoredRight)
                    positionHori = (int)MainContainer.PointToScreen(new Point(0, 0)).X;
                else
                    positionHori = -(int)(SystemParameters.PrimaryScreenWidth - MainContainer.PointToScreen(new Point(MainContainer.Width, 0)).X);
                if (!posAnchoredBottom)
                    positionVert = (int)MainContainer.PointToScreen(new Point(0, 0)).Y;
                else
                    positionVert = -(int)(SystemParameters.PrimaryScreenHeight - MainContainer.PointToScreen(new Point(0, MainContainer.Height)).Y);
                
                string posContainer = positionHori + "," + positionVert;
                options.Add(posContainer);

                if(linkedDir == "")
                {
                    var elems = Wrap_Shortcut.Children;
                    foreach (var item in elems)
                    {
                        Border border = item as Border;
                        items.Add(border.Tag.ToString());
                    }
                } else
                {
                    directories.Add(linkedDir);
                }

                save_datas.Add(options);
                save_datas.Add(items);
                save_datas.Add(directories);

                var json = JsonSerializer.Serialize(save_datas);

                File.WriteAllText(saveDirectory + @"\container" + timestamp + ".json", json);

                Btn_Settings.IsEnabled = true;
            }
            else
            {
                File.Delete(saveDirectory + @"\container" + timestamp + ".json");
                Btn_Settings.IsEnabled = false;
            }
                
        }

        private void Item_Delete(Border item)
        {
            Wrap_Shortcut.Children.Remove(item);
            if (Wrap_Shortcut.Children.Count == 0)
                Empty_Container_Text.Visibility = Visibility.Visible;
            Link_Container(false);
        }
        private void Item_MoveUp(Border item)
        {
            int index = Wrap_Shortcut.Children.IndexOf(item);
            if (index != 0)
            {
                Wrap_Shortcut.Children.Remove(item);
                Wrap_Shortcut.Children.Insert(index - 1, item);
            }
        }
        private void Item_MoveDown(Border item)
        {
            int index = Wrap_Shortcut.Children.IndexOf(item);
            if (index != Wrap_Shortcut.Children.Count)
            {
                Wrap_Shortcut.Children.Remove(item);
                Wrap_Shortcut.Children.Insert(index + 1, item);
            }
        }
        private void Delete_All()
        {
            Wrap_Shortcut.Children.Clear();
            Empty_Container_Text.Visibility = Visibility.Visible;
            Link_Container(false);
        }

        bool optionsOpened = false;
        private async void Btn_ContainerOptions_Click(object sender, RoutedEventArgs e)
        {
            optionsOpened = !optionsOpened;
            if (optionsOpened)
            {
                Btn_Settings.Visibility = Visibility.Visible;
                Btn_Pin.Visibility = Visibility.Visible;
                Btn_Invert.Visibility = Visibility.Visible;
                Btn_NewContainer.Visibility = Visibility.Visible;
                Btn_CloseContainer.Visibility = Visibility.Visible;
                if(!containerReduced)
                    Btn_Color.Visibility = Visibility.Visible;

                await Task.Delay(10000);

                Options_Auto_Collapse();
            }
            else
            {
                Btn_Settings.Visibility = Visibility.Collapsed;
                Btn_Color.Visibility = Visibility.Collapsed;
                Btn_Invert.Visibility = Visibility.Collapsed;
                Btn_NewContainer.Visibility = Visibility.Collapsed;
                Btn_CloseContainer.Visibility = Visibility.Collapsed;
                Color_Palette.Visibility = Visibility.Collapsed;
                if (!pinned)
                    Btn_Pin.Visibility = Visibility.Collapsed;
            }
        }

        private void Options_Auto_Collapse()
        {
            if (optionsOpened)
            {
                Btn_Settings.Visibility = Visibility.Collapsed;
                Btn_Color.Visibility = Visibility.Collapsed;
                Btn_Invert.Visibility = Visibility.Collapsed;
                Btn_NewContainer.Visibility = Visibility.Collapsed;
                Btn_CloseContainer.Visibility = Visibility.Collapsed;
                if (!pinned)
                    Btn_Pin.Visibility = Visibility.Collapsed;

                optionsOpened = !optionsOpened;

                if(!MainContainer.IsMouseOver)
                    Btn_ContainerOptions.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            ContainerSettings settings = new(timestamp)
            {
                Owner = this
            };
            settings.ShowDialog();
        }

        private void Btn_Pin_Click(object sender, RoutedEventArgs e)
        {
            MainContainer.Topmost = !MainContainer.Topmost;
            pinned = MainContainer.Topmost;
            if (!optionsOpened)
                Btn_Pin.Visibility = Visibility.Collapsed;
        }

        bool bottom_titlebar = false;
        private void Btn_Invert_Click(object sender, RoutedEventArgs e)
        {
            bottom_titlebar = !bottom_titlebar;
            Grid_Container.RowDefinitions.Clear();

            if (bottom_titlebar)
            {
                Grid_Container.RowDefinitions.Add(ROW_DEFINITION_WRAP);
                Grid_Container.RowDefinitions.Add(ROW_DEFINITION_PALETTE);
                Grid_Container.RowDefinitions.Add(ROW_DEFINITION_TITLE);
                TitleBar.SetValue(Grid.RowProperty, 2);
                Color_Palette.SetValue(Grid.RowProperty, 1);
                Scroll_Container.SetValue(Grid.RowProperty, 0);
                Empty_Container_Text.SetValue(Grid.RowProperty, 0);
                Container_Border.CornerRadius = new CornerRadius(10,10,0,0);
                TitleBar.Margin = new Thickness(0,0,15,0);
                Btn_Invert.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/images/switchTitleTop.png")));
            }
            else
            {
                Grid_Container.RowDefinitions.Add(ROW_DEFINITION_TITLE);
                Grid_Container.RowDefinitions.Add(ROW_DEFINITION_PALETTE);
                Grid_Container.RowDefinitions.Add(ROW_DEFINITION_WRAP);
                TitleBar.SetValue(Grid.RowProperty, 0);
                Color_Palette.SetValue(Grid.RowProperty, 1);
                Scroll_Container.SetValue(Grid.RowProperty, 2);
                Empty_Container_Text.SetValue(Grid.RowProperty, 2);
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

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount != 2)
            {
                MainContainer.ResizeMode = ResizeMode.NoResize;
                DragMove();
            }
        }

        private void TitleBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!containerReduced)
                MainContainer.ResizeMode = ResizeMode.CanResizeWithGrip;
        }

        private void MainContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            Btn_ContainerOptions.Visibility = Visibility.Visible;
            if (!containerReduced)
                MainContainer.ResizeMode = ResizeMode.CanResizeWithGrip;

            var color = (Color)ColorConverter.ConvertFromString(Container_Border.Background.ToString());
            color.ScA = 0.7f;
            Container_Border.Background = new SolidColorBrush(color);
        }

        private void MainContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!optionsOpened)
            {
                Btn_ContainerOptions.Visibility = Visibility.Collapsed;
            }
            if (containerReduced)
                MainContainer.ResizeMode = ResizeMode.NoResize;
            Container_Border.Background = container_color;
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
                if (optionsOpened)
                    Btn_Color.Visibility = Visibility.Visible;
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
                Btn_Color.Visibility = Visibility.Collapsed;
                Color_Palette.Visibility = Visibility.Collapsed;
                MainContainer.Height = 20;
                MainContainer.ResizeMode = ResizeMode.NoResize;
            }
            containerReduced = !containerReduced;
        }

        private static void Hover_Item(Border border_item, bool hovering)
        {
            if(hovering)
            {
                ColorAnimation ca = new(Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF), new Duration(TimeSpan.FromMilliseconds(250)));
                border_item.Background = new SolidColorBrush(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF));
                border_item.Background.BeginAnimation(SolidColorBrush.ColorProperty, ca);
            }
                
            else
            {
                ColorAnimation ca = new(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), new Duration(TimeSpan.FromMilliseconds(250)));
                border_item.Background = new SolidColorBrush(Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF));
                border_item.Background.BeginAnimation(SolidColorBrush.ColorProperty, ca);
            }
        }

        private void Btn_Color_Click(object sender, RoutedEventArgs e)
        {
            if (Color_Palette.Visibility == Visibility.Collapsed)
                Color_Palette.Visibility = Visibility.Visible;
            else
                Color_Palette.Visibility = Visibility.Collapsed;
        }

        private void Set_Container_Background(Button button)
        {
            var color = (Color)ColorConverter.ConvertFromString(button.Background.ToString());
            color.ScA = 0.5f;
            Container_Border.Background = new SolidColorBrush(color);
            container_color = Container_Border.Background;

            Save_Container();
        }

        private void Btn_Color1_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Set_Container_Background(button);
        }

        private void Btn_Color2_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Set_Container_Background(button);
        }

        private void Btn_Color3_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Set_Container_Background(button);
        }

        private void Btn_Color4_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Set_Container_Background(button);
        }

        private void Btn_Color5_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Set_Container_Background(button);
        }

        private void Btn_Color6_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Set_Container_Background(button);
        }

        private void Btn_Color7_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Set_Container_Background(button);
        }

        private void Btn_Color8_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Set_Container_Background(button);
        }

        private void Btn_Color9_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Set_Container_Background(button);
        }

        private void Btn_Color10_Click(object sender, RoutedEventArgs e)
        { 
            Button button = sender as Button;
            Set_Container_Background(button);
        }

        private void Btn_Color11_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Set_Container_Background(button);
        }

        private void ContextMenu_CloseApp_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Empty_Container_Text_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CommonOpenFileDialog dialog = new()
            {
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string path = dialog.FileName;
                if (Directory.Exists(path))
                {
                    Link_Container(true, path);
                    AddItem(Directory.GetDirectories(path));
                    AddItem(Directory.GetFiles(path));

                    string name = Path.GetFileName(path);
                    Container_Title.Text = Path.GetFileName(name);
                    MainContainer.Title = Path.GetFileName(name);
                }
            }
        }

        private void Link_Container(bool link, string path = "")
        {
            if (link)
            {
                linkedDir = path;
                Icon_LinkedContainer.Visibility = Visibility.Visible;
                Icon_LinkedContainer.ToolTip = "The contents of this Container is linked to a folder (" + path + ")";
            } else
            {
                linkedDir = "";
                Icon_LinkedContainer.Visibility = Visibility.Collapsed;
            }
            Save_Container();
        }
    }
}
