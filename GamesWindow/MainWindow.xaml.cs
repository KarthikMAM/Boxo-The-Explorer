﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Drawing=System.Drawing;

namespace GamesWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Boxo boxoWindow;

        Stack<string> FoldersBack = new Stack<string>();
        Stack<string> FoldersNext = new Stack<string>();

        static string RESOURCES_DIRECTORY = Environment.CurrentDirectory + @"\Resources\";
        static string LIBRARIES_DIRECTORY = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Kappspot\Boxo The Explorer\Libraries\";
        static int NO_OF_COLUMNS = 5;
        static int INTIAL_UPDATE_COUNT = 35;

        class IconButtons
        {
            static int SIZE_MULTIPLIER = 50;
            static int MARGIN = 10;
            public static int TOTAL_ICON_SPACE = MARGIN + SIZE_MULTIPLIER;
            public static MainWindow MainWindowReferance { get; set; }
            
            static bool ThumbnailCallBack()
            { 
                return false; 
            }
            static Drawing.Image.GetThumbnailImageAbort CallBackDelegate = new Drawing.Image.GetThumbnailImageAbort(ThumbnailCallBack);
            static Dictionary<string, BitmapFrame> IconDictionary = new Dictionary<string, BitmapFrame>();
            static Drawing.Icon IconSource = null;
            static BitmapFrame IconFrame = null;
            static int DECODE_SIZE = (int)(SIZE_MULTIPLIER * 1.5);
            static MemoryStream IconSaveStream = null;
            static PngBitmapDecoder IconDecoder = null;
            static BitmapImage ImageIcon = null;
            
            public Image IconImage { get; set; }
            public string LinkPath { get; set; }

            public void SetIconSource()
            {
                string fileExtension = Path.GetExtension(LinkPath).ToLower();

                if (IconDictionary.TryGetValue(fileExtension, out IconFrame) == true)
                {
                    IconImage.Source = IconFrame;
                }
                else
                {
                    switch (fileExtension)
                    {
                        case ".jpg":
                        case ".png":
                        case ".bmp":
                            try
                            {
                                ImageIcon = new BitmapImage();

                                ImageIcon.BeginInit();

                                ImageIcon.UriSource = new Uri(LinkPath);
                                ImageIcon.DecodePixelWidth = DECODE_SIZE;

                                ImageIcon.EndInit();

                                IconImage.Source = ImageIcon;
                            }
                            catch
                            {
                                IconSource = new Drawing.Icon(Drawing.SystemIcons.Question, SIZE_MULTIPLIER, SIZE_MULTIPLIER);
                                goto case "iconextraction";
                            }
                            break;
                        case "":
                            IconSource = new Drawing.Icon(RESOURCES_DIRECTORY + "folder open.ico", DECODE_SIZE, DECODE_SIZE);
                            goto case "iconextraction";
                        default:
                            try
                            {
                                IconSource = Drawing.Icon.ExtractAssociatedIcon(LinkPath);
                            }
                            catch
                            {
                                IconSource = new Drawing.Icon(Drawing.SystemIcons.Question, SIZE_MULTIPLIER, SIZE_MULTIPLIER);
                            }
                            goto case "iconextraction";
                        case "iconextraction":
                            IconSaveStream = new MemoryStream();

                            IconSource.ToBitmap().Save(IconSaveStream, Drawing.Imaging.ImageFormat.Png);

                            IconDecoder = new PngBitmapDecoder(IconSaveStream, BitmapCreateOptions.None, BitmapCacheOption.None);
                            
                            IconImage.Source = IconDecoder.Frames[0];
                            if (fileExtension != ".exe" && fileExtension != ".lnk" && fileExtension != ".url" && fileExtension != ".png" && fileExtension != ".jpg" && fileExtension != ".bmp" && fileExtension != ".ico")
                            {
                                IconDictionary.Add(fileExtension, IconDecoder.Frames[0]);
                            }
                            break;
                    }
                }
            }

            public IconButtons(string linkPath, int rowIndex, int columnIndex)
            {
                IconImage = new Image();
                IconImage.Name = "Button" + rowIndex + columnIndex;

                var iconMargin = IconImage.Margin;
                iconMargin.Left = columnIndex * (SIZE_MULTIPLIER + MARGIN) + MARGIN;
                iconMargin.Top = rowIndex * (SIZE_MULTIPLIER + MARGIN) + MARGIN;
                IconImage.Margin = iconMargin;

                IconImage.Height = SIZE_MULTIPLIER;
                IconImage.Width = SIZE_MULTIPLIER;
                IconImage.HorizontalAlignment = HorizontalAlignment.Left;
                IconImage.VerticalAlignment = VerticalAlignment.Top;

                LinkPath = linkPath;
                IconImage.Stretch = Stretch.Fill;

                SetIconSource();

                RenderOptions.SetBitmapScalingMode(IconImage, BitmapScalingMode.HighQuality);

                IconImage.MouseLeftButtonDown += IconImage_MouseLeftButtonDown;
                IconImage.MouseEnter += IconImage_MouseEnter;
                IconImage.MouseLeave += IconImage_MouseLeave;
                IconImage.MouseRightButtonDown += IconImage_MouseRightButtonDown;

                MainWindowReferance.ButtonGrid.Children.Add(IconImage);
            }

            void IconImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
            {
                if (MainWindowReferance.FoldersBack.Count == 1)
                {
                    string deleteEntry = MainWindowReferance.Category.Content.ToString();
                    MainWindowReferance.DeleteEntry(MainWindowReferance.DisplayMessage("Do you want to remove the file \"" + Path.GetFileNameWithoutExtension(LinkPath) + "\" from this library?", "Delete Conformation", MessageBoxButton.YesNo), LinkPath, LIBRARIES_DIRECTORY + deleteEntry + ".boxolibrary");
                    MainWindowReferance.InitializeGrid(LIBRARIES_DIRECTORY + deleteEntry + ".boxolibrary");
                }
            }

            void IconImage_MouseLeave(object sender, MouseEventArgs e)
            {
                MainWindowReferance.FileLabel.Text = "";
                MainWindowReferance.Highlighter.Visibility = Visibility.Hidden;
            }

            void IconImage_MouseEnter(object sender, MouseEventArgs e)
            {
                var Margin = IconImage.Margin;
                Margin.Left -= (MARGIN / 2);
                Margin.Top -= (MARGIN / 2);
                MainWindowReferance.Highlighter.Margin = Margin;
                MainWindowReferance.Highlighter.Visibility = Visibility.Visible;
                MainWindowReferance.FileLabel.Text = Path.GetFileNameWithoutExtension(LinkPath);
            }

            void IconImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            {
                if (e.ClickCount == 2)
                {
                    try
                    {
                        switch (Path.GetExtension(LinkPath))
                        {
                            case "":
                                if (MainWindowReferance.AlternateOpening.IsChecked == false)
                                {
                                    MainWindowReferance.FoldersBack.Push(LinkPath);
                                    MainWindowReferance.InitializeGrid(LinkPath);
                                    break;
                                }
                                else
                                {
                                    goto default;
                                }
                            default:
                                ProcessStartInfo App = new ProcessStartInfo();
                                App.UseShellExecute = true;
                                App.WorkingDirectory = Path.GetDirectoryName(LinkPath);
                                App.FileName = Path.GetFileName(LinkPath);
                                Process.Start(App);
                                break;
                        }
                    }
                    catch
                    {
                        if (MainWindowReferance.FoldersBack.Count == 1)
                        {
                            string deleteEntry = MainWindowReferance.Category.Content.ToString();
                            MainWindowReferance.DeleteEntry(MainWindowReferance.DisplayMessage("The file or folder is not avilable anymore \n\nDo you want to remove the file \"" + Path.GetFileNameWithoutExtension(LinkPath) + "\" from this library?", "Delete Conformation", MessageBoxButton.YesNo), LinkPath, LIBRARIES_DIRECTORY + deleteEntry + ".boxolibrary");
                            MainWindowReferance.InitializeGrid(LIBRARIES_DIRECTORY + deleteEntry + ".boxolibrary");
                        }
                    }
                }
            }
        }

        //List<IconButtons> iconGrid;

        SpeechSynthesizer HelpAgent = new SpeechSynthesizer();

        int noOfItems = 0, currentItem = 0;
        
        DispatcherTimer updateTimer = new DispatcherTimer(DispatcherPriority.Normal);
        DispatcherTimer waitTimer = new DispatcherTimer(DispatcherPriority.Normal);

        Queue<string> libraryEntries = new Queue<string>();
        BitmapImage[] loadCursorImage = new BitmapImage[30];

        public MainWindow()
        {
            InitializeComponent();

            HelpAgent.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Teen);

            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Kappspot\MiniMetro"))
            {
                string userName = "";
                foreach (char i in Environment.UserName)
                {
                    if (i != '.')
                    {
                        userName += i;
                    }
                    else
                    {
                        userName += " ";
                    }
                }
                HelpAgent.SpeakAsync("Hello " + userName);
            }

            for (int i = 0; i < 30; i++)
            {
                loadCursorImage[i] = new BitmapImage(new Uri(RESOURCES_DIRECTORY + @"\Animation\loadCursor" + (i + 1) + ".png"));
            }
            updateTimer.Interval = TimeSpan.FromMilliseconds(1);
            updateTimer.Tick += updateTimer_Tick;

            waitTimer.Interval = TimeSpan.FromMilliseconds(20);
            waitTimer.Tick += waitTimer_Tick;

            InitalizeWindow();

            this.Hide();

            boxoWindow = new Boxo(this);
            boxoWindow.Show();
        }

        int i = 0;
        void waitTimer_Tick(object sender, EventArgs e)
        {
            LoadCursor.Source = loadCursorImage[i++];
            i %= 30;
        }
        
        /// <summary>
        ///     This is used to give the user an AudioVisual Error Message
        /// </summary>
        /// <param name="textToSpeak"> The Error Message</param>
        /// <param name="caption"> The title of the error message</param>
        /// <param name="buttons"> The type of response expected from the user</param>
        /// <returns>The response the user gives the UI</returns>
        /// <remarks>
        ///     Cancel all the voice messages being played now
        ///     Speak the test asynchronously
        ///     Display the message and get the user response
        ///     Return that response
        /// </remarks>
        public MessageBoxResult DisplayMessage(string textToSpeak, string caption, MessageBoxButton buttons)
        {
            HelpAgent.SpeakAsyncCancelAll();
            HelpAgent.SpeakAsync(textToSpeak);
            return MessageBox.Show(textToSpeak, caption, buttons);
        }

        /// <summary>
        ///     This initializes the various features of the Window like the List of libraries
        ///     This is used when a change is made to the library list like deletion.
        ///     This is also used for initializing the library for the first time
        /// </summary>
        /// <remarks>
        ///     Set the content of the library label and the category label to "Libraries" and "Categories"
        ///     Clear the library list
        ///     Read the library items from the database file and add them to the list
        ///     Also add the AddNew(+) library item
        /// </remarks>
        public void InitalizeWindow()
        {
            //Set the content of the library label and the category label to "Libraries" and "Categories"
            LibraryLabel.Content = "Libraries";
            Category.Content = "Category";

            //Clear the library list
            Libraries.Items.Clear();

            //Read the library items from the database file and add them to the list
            foreach (string library in ReadEntries(LIBRARIES_DIRECTORY + "Libraries.boxolibrary"))
            {
                if (library != "")
                {
                    Libraries.Items.Add(library);
                }
            }

            //Also add the AddNew(+) library item
            Libraries.Items.Add(AddNew);
        }

        /// <summary>
        ///     This is used to Read the entries of the database files and return them as a string[]
        /// </summary>
        /// <param name="filePath">The full path where the database file exists</param>
        /// <returns>The entries in the database file</returns>
        /// <remarks>
        ///     Read the entries from the database file using a StreamReader
        /// </remarks>
        public List<string> ReadEntries(string filePath)
        {
            //This will be returned to the other functions for further utilization
            //This contains the entries stored in the database file
            List<string> results = new List<string>();

            try
            {
                //Create an interface for reading the database file
                //Read the file which contains one database entry each
                //Add each of these entries to the results
                //Close the Interfaces
                FileStream streamedFile = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read);
                StreamReader reader = new StreamReader(streamedFile);
                while (reader.EndOfStream != true)
                {
                    results.Add(reader.ReadLine());
                }
                reader.Close();
                streamedFile.Close();
            }
            catch
            {

            }

            //Return the results
            return results;
        }

        /// <summary>
        ///     This will delete the entry that is specified in "deleteEntry" of "filePath"
        /// </summary>
        /// <param name="UserChoice">MessageBoxResult that the user provides indicating his consent</param>
        /// <param name="deleteEntry">The entry to be deleted</param>
        /// <param name="filePath">The path where the entry is stored</param>
        /// <remarks>
        ///     If the user wishes for the entry to be deleted then delete it.
        ///     Open the file and read all of its contents
        ///     Open the file for writing the new contents to the file
        ///     If the entry is not the entry to be deleted then write it otherwise write it to the file
        /// </remarks>
        public void DeleteEntry(MessageBoxResult UserChoice, string deleteEntry, string filePath)
        {
            try
            {
                //If the user wishes for the entry to be deleted then delete it.
                if (UserChoice == MessageBoxResult.Yes)
                {
                    //This is the buffer that will be used to hold the items of the database
                    List<string> dataEntryBuffer = new List<string>();

                    //Open the file and read all of its contents
                    FileStream streamedFile = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
                    StreamReader reader = new StreamReader(streamedFile);
                    while (reader.EndOfStream != true)
                    {
                        dataEntryBuffer.Add(reader.ReadLine());
                    }
                    reader.Close();

                    //Open the file for writing the new contents to the file
                    streamedFile = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);

                    StreamWriter writer = new StreamWriter(streamedFile);
                    foreach (string i in dataEntryBuffer)
                    {
                        //If the entry is not the entry to be deleted then write it.
                        if (i != "" && i != deleteEntry)
                        {
                            writer.WriteLine(i);
                        }
                    }
                    writer.Flush();

                    streamedFile.Close();

                    //Reinitialize the window to update the changes to the UI
                    InitalizeWindow();
                }
            }
            catch
            {

            }
        }

        /// <summary>
        ///     This function will add the given entries to the database files and update the same
        /// </summary>
        /// <param name="addEntries">Specifies the entries to be added</param>
        /// <param name="filePath">Specifies the library path</param>
        /// <remarks>
        ///     This will search the file whether the entry to be added exists or not
        ///     If the entry does not exist, then it should be added otherwise an error message will be displayed
        /// </remarks>
        public void AddEntry(string[] addEntries, string filePath)
        {
            //This will try and add each entry to the database database file
            try
            {
                foreach (string addEntry in addEntries)
                {
                    //Open the library to add the data items
                    FileStream streamedFile = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read);
                    StreamReader reader = new StreamReader(streamedFile);

                    bool entryAlreadyExists = false;

                    //Read each entry from the start of the file and check whether the entry already exists or not
                    reader.ReadLine();
                    while (reader.EndOfStream != true)
                    {
                        if (reader.ReadLine() == addEntry)
                        {
                            entryAlreadyExists = true;
                        }
                    }
                    reader.Close();
                    streamedFile.Close();

                    //If the entry does not exist then add that entry otherwise Display an error message
                    if (entryAlreadyExists == false)
                    {
                        streamedFile = new FileStream(filePath, FileMode.Append, FileAccess.Write); ;
                        StreamWriter writer = new StreamWriter(streamedFile);
                        writer.WriteLine(addEntry);
                        writer.Flush();
                        streamedFile.Close();
                    }
                    else
                    {
                        DisplayMessage(" Sorry! But This entry already exists...", "Redundant entry", MessageBoxButton.OK);
                    }
                }
            }
            catch
            {

            }
        }

        public void InitializeGrid(string locatorURL)
        {
            Scroller.ScrollToHome();
            updateTimer.Stop();
            waitTimer.Stop();
            libraryEntries.Clear();

            IconButtons.MainWindowReferance = this;
            //iconGrid = new List<IconButtons>();
            ButtonGrid.Children.Clear();
            ButtonGrid.Children.Add(Highlighter);

            Category.Content = Path.GetFileNameWithoutExtension(locatorURL);

            if (Path.GetExtension(locatorURL) == "")
            {
                foreach (string file in Directory.GetFiles(locatorURL))
                {
                    if (File.GetAttributes(file) != FileAttributes.Hidden)
                        libraryEntries.Enqueue(file);
                }
                foreach (string directory in Directory.GetDirectories(locatorURL))
                {
                    libraryEntries.Enqueue(directory);
                }
            }
            else
            {

                FileStream windowDataFile = new FileStream(locatorURL, FileMode.OpenOrCreate, FileAccess.Read);
                StreamReader windowData = new StreamReader(windowDataFile);

                string filePath;
                while ((filePath = windowData.ReadLine()) != null)
                {
                    if (filePath != "")
                    {
                        libraryEntries.Enqueue(filePath);
                    }
                }

                windowData.Close();
                windowDataFile.Close();
            }

            currentItem = -1;
            updateTimer.Start();
            waitTimer.Start();
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (updateTimer.IsEnabled == false && libraryEntries.Count > 0)
            {
                LoadCursor.Visibility = Visibility.Visible;
                updateTimer.Start();
                waitTimer.Start();
            }
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            //indi.Content = Scroller.ViewportHeight + "   " + Scroller.ActualHeight + "   " + Scroller.ScrollableHeight + "   " + Scroller.ExtentHeight;

            noOfItems = INTIAL_UPDATE_COUNT + (int)(Scroller.VerticalOffset / IconButtons.TOTAL_ICON_SPACE) * NO_OF_COLUMNS;

            if (currentItem == -1)
            {
                ButtonGrid.Height = IconButtons.TOTAL_ICON_SPACE * libraryEntries.Count / NO_OF_COLUMNS + IconButtons.TOTAL_ICON_SPACE;
                if (ButtonGrid.Height < 330)
                {
                    ButtonGrid.Height = 330;
                }
                GC.Collect();
                noOfItems = INTIAL_UPDATE_COUNT;
                LoadCursor.Visibility = Visibility.Visible;
                ButtonGrid.Children.Clear();
                ButtonGrid.Children.Add(Highlighter);
                currentItem++;
            }
            if (currentItem < noOfItems && libraryEntries.Count > 0)
            {
                new IconButtons(libraryEntries.Dequeue(), currentItem / NO_OF_COLUMNS, currentItem % NO_OF_COLUMNS);
                currentItem++;
            }
            else
            {
                LoadCursor.Visibility = Visibility.Hidden;
                updateTimer.Stop();
                waitTimer.Stop();
                GC.Collect();
            }
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch ((sender as Image).Name)
            {
                case "Close":
                    this.Close();
                    break;
                case "Prev":
                    if (FoldersBack.Count > 1)
                    {
                        FoldersNext.Push(FoldersBack.Pop());
                        InitializeGrid(FoldersBack.Peek());
                    }
                    break;
                case "Next":
                    if (FoldersNext.Count > 0)
                    {
                        FoldersBack.Push(FoldersNext.Pop());
                        InitializeGrid(FoldersBack.Peek());
                    }
                    break;
                case "DeleteContents":
                    if (Libraries.SelectedIndex >= 0 && Libraries.SelectedItem.GetType().ToString() == "System.String")
                    {
                        string selectedLibrary = Libraries.SelectedItem.ToString();
                        MessageBoxResult userChoice = DisplayMessage("Do you want to delete the library \"" + selectedLibrary + "\" ?", "Delete Confirmation", MessageBoxButton.YesNo);
                        if(userChoice == MessageBoxResult.Yes)
                        {
                            DeleteEntry(userChoice, Libraries.SelectedItem.ToString(), LIBRARIES_DIRECTORY + "Libraries.boxolibrary");
                            File.Delete(LIBRARIES_DIRECTORY + selectedLibrary + ".boxolibrary");
                        }

                        FoldersBack.Clear();
                        FoldersNext.Clear();
                        InitalizeWindow();
                        ButtonGrid.Children.Clear();
                        ButtonGrid.Children.Add(Highlighter);
                    }
                    break;
                case "Minimize":
                    this.Hide();
                    boxoWindow.Show();
                    break;
                case "Toggle":
                    Process.Start("shell:::{3080F90E-D7AD-11D9-BD98-0000947B0257}");
                    break;
                case "MuteOff":
                    MuteOff.Source = new BitmapImage(new Uri(RESOURCES_DIRECTORY + "btn_volume_P.png"));
                    MuteOff.Visibility = Visibility.Hidden;
                    MuteOn.Visibility = Visibility.Visible;
                    HelpAgent.Volume = 0;
                    break;
                case "MuteOn":
                    MuteOn.Source = new BitmapImage(new Uri(RESOURCES_DIRECTORY + "btn_mute_P.png"));
                    HelpAgent.Volume = 100;
                    MuteOn.Visibility = Visibility.Hidden;
                    MuteOff.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            Image buttonImage = sender as Image;
            string buttonSource = buttonImage.Source.ToString();
            buttonSource = buttonSource.Substring(0, buttonSource.Length - 5) + "H" + ".png";
            buttonImage.Source = new BitmapImage(new Uri(buttonSource));
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            Image buttonImage = sender as Image;
            string buttonSource = buttonImage.Source.ToString();
            buttonSource = buttonSource.Substring(0, buttonSource.Length - 5) + "P" + ".png";
            buttonImage.Source = new BitmapImage(new Uri(buttonSource));
        }

        private void IconWindow_Drop(object sender, DragEventArgs e)
        {
            if (Libraries.SelectedIndex > -1 && Category.Content.ToString() == Libraries.SelectedItem.ToString())
            {
                string[] addEntries = (string[])e.Data.GetData(DataFormats.FileDrop);
                AddEntry(addEntries, LIBRARIES_DIRECTORY + Libraries.SelectedItem.ToString() + ".boxolibrary");
                InitializeGrid(LIBRARIES_DIRECTORY + Libraries.SelectedItem.ToString() + ".boxolibrary");
            }
            else
            {
                DisplayMessage("You cannot add items here.. Please go back to any one of the libraries and add the item", "Cannot add item", MessageBoxButton.OK);
            }
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try { this.DragMove(); }
            catch { }
        }

        private void Libraries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Libraries.SelectedIndex >= 0 && Libraries.SelectedItem.GetType().ToString() == "System.String")
            {
                try
                {
                    LibraryLabel.Content = Libraries.SelectedItem.ToString();
                    FoldersBack.Clear();
                    FoldersNext.Clear();
                    Category.Content = (sender as ListBox).SelectedItem.ToString();
                    FoldersBack.Push(LIBRARIES_DIRECTORY + Libraries.SelectedItem.ToString() + ".boxolibrary");
                    InitializeGrid(LIBRARIES_DIRECTORY + Libraries.SelectedItem.ToString() + ".boxolibrary");
                }
                catch
                {

                }
            }
            else
            {
                if (Libraries.SelectedIndex >= 0)
                {
                    LibraryLabel.Content = "Add a New Library";
                }
            }
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            new AddItemsWindow().Show();

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.IsActive == true)
            {
                boxoWindow.Close();
            }
        }

        private void AddNewButton_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            LibraryLabel.Content = "Add a New Library";
            try
            {
                if (e.Key == Key.Enter && AddNewButton.Text != "" && AddNewButton.Text != "+")
                {
                    AddEntry(new string[] { AddNewButton.Text }, LIBRARIES_DIRECTORY + "Libraries.boxolibrary");
                    InitalizeWindow();
                    AddNewButton.FontSize = 26;
                    AddNewButton.FontWeight = FontWeights.ExtraBold;
                }
            }
            catch
            {
                DisplayMessage("Enter a valid Library Name", "InvalidateArrange Library", MessageBoxButton.OK);
            }
        }

        private void AddNewButton_LostFocus(object sender, RoutedEventArgs e)
        {
            AddNewButton.Text = "+";
            AddNewButton.FontSize = 26;
            AddNewButton.FontWeight = FontWeights.ExtraBold;
        }

        private void AddNewButton_GotFocus(object sender, RoutedEventArgs e)
        {
            LibraryLabel.Content = "Add a New Library";
            AddNewButton.Clear();
            AddNewButton.FontSize = 15;
            AddNewButton.FontWeight = FontWeights.Normal;
        }
    }
}

//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Data.SqlClient;
//using System.Diagnostics;
//using System.IO;
//using System.Speech.Synthesis;
//using System.Windows;
//using Controls;
//using Input;
//using Media;
//using Media.Imaging;

//namespace GamesWindow
//{
//    /// <summary>
//    /// Interaction logic for MainWindow.xaml
//    /// </summary>
//    public partial class MainWindow : Window
//    {
//        Boxo boxoWindow;

//        Stack<string> FoldersBack = new Stack<string>();
//        Stack<string> FoldersNext = new Stack<string>();
//        static string RESOURCES_DIRECTORY = Directory.GetCurrentDirectory() + "\\Resources\\";
//        SpeechSynthesizer HelpAgent = new SpeechSynthesizer();

//        class IconButtons
//        {
//            static double WIDTH_MULTIPLIER = 50;
//            static double HEIGHT_MULTIPLIER = 50;
//            static double MARGIN = 10;

//            public static MainWindow MainWindowReferance = null;

//            Image IconImage;

//            public string LinkPath { get; set; }
//            public string FileName { get; set; }
//            public string Extension { get; set; }

//            public void SetIconSource()
//            {
//                try
//                {
//                    switch (Extension)
//                    {

//                        case ".jpg":
//                        case ".png":
//                        case ".bmp":
//                            IconImage.Source = new BitmapImage(new Uri(LinkPath));
//                            break;
//                        case "":
//                            {
//                                MemoryStream IconSaveStream = new MemoryStream();
//                                Drawing.Icon IconSource = new Drawing.Icon(RESOURCES_DIRECTORY + "folder open.ico" , 200, 200);
//                                Drawing.Bitmap icon = IconSource.ToBitmap();
//                                icon.Save(IconSaveStream, Drawing.Imaging.ImageFormat.Png);
//                                PngBitmapDecoder IconDecoder = new PngBitmapDecoder(IconSaveStream, BitmapCreateOptions.None, BitmapCacheOption.None);
//                                IconImage.Source = IconDecoder.Frames[0];
//                                break;
//                            }
//                        default:
//                        case ".exe":
//                            try
//                            {
//                                MemoryStream IconSaveStream = new MemoryStream();
//                                Drawing.Icon IconSource = Drawing.Icon.ExtractAssociatedIcon(LinkPath);
//                                Drawing.Bitmap icon = IconSource.ToBitmap();
//                                icon.Save(IconSaveStream, Drawing.Imaging.ImageFormat.Png);
//                                PngBitmapDecoder IconDecoder = new PngBitmapDecoder(IconSaveStream, BitmapCreateOptions.None, BitmapCacheOption.None);
//                                IconImage.Source = IconDecoder.Frames[0];
//                            }
//                            catch
//                            {
//                                MemoryStream IconSaveStream = new MemoryStream();
//                                Drawing.Icon IconSource = new Drawing.Icon(Drawing.SystemIcons.Question, 200, 200);
//                                Drawing.Bitmap icon = IconSource.ToBitmap();
//                                icon.Save(IconSaveStream, Drawing.Imaging.ImageFormat.Png);
//                                PngBitmapDecoder IconDecoder = new PngBitmapDecoder(IconSaveStream, BitmapCreateOptions.None, BitmapCacheOption.None);
//                                IconImage.Source = IconDecoder.Frames[0];
//                            }
//                            break;
//                    }
//                }
//                catch
//                {

//                }
//            }

//            public IconButtons(string linkPath, string linkName, int rowIndex, int columnIndex)
//            {
//                IconImage = new Image();
//                IconImage.Name = "Button" + rowIndex + columnIndex;

//                var Margin = IconImage.Margin;
//                Margin.Left = columnIndex * (WIDTH_MULTIPLIER + MARGIN) + MARGIN;
//                Margin.Top = rowIndex * (HEIGHT_MULTIPLIER + MARGIN) + MARGIN;
//                IconImage.Margin = Margin;

//                IconImage.Height = HEIGHT_MULTIPLIER;
//                IconImage.Width = WIDTH_MULTIPLIER;
//                IconImage.HorizontalAlignment = HorizontalAlignment.Left;
//                IconImage.VerticalAlignment = VerticalAlignment.Top;

//                LinkPath = linkPath;
//                FileName = linkName;
//                Extension = Path.GetExtension(linkPath).ToLower();

//                SetIconSource();

//                RenderOptions.SetBitmapScalingMode(IconImage, BitmapScalingMode.HighQuality);
//                IconImage.Stretch = Stretch.Fill;

//                IconImage.MouseLeftButtonDown += t_MouseLeftButtonDown;
//                IconImage.MouseEnter += IconImage_MouseEnter;
//                IconImage.MouseLeave += IconImage_MouseLeave;
//                IconImage.MouseRightButtonDown += IconImage_MouseRightButtonDown;

//                MainWindowReferance.ButtonGrid.Children.Add(IconImage);
//            }

//            void IconImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
//            {
//                if (MainWindowReferance.FoldersBack.Count == 1)
//                {
//                    MainWindowReferance.DeleteEntry(MainWindowReferance.DisplayMessage("Do you want to remove the file \"" + Path.GetFileNameWithoutExtension(LinkPath) + "\" from this library?", "Delete Conformation", MessageBoxButton.YesNo), LinkPath, LIBRARIES_DIRECTORY + MainWindowReferance.Category.Content.ToString() + ".boxolibrary");
//                    MainWindowReferance.InitializeGrid(RESOURCES_DIRECTORY + MainWindowReferance.Category.Content.ToString() + ".boxolibrary");
//                }
//            }

//            void IconImage_MouseLeave(object sender, MouseEventArgs e)
//            {
//                MainWindowReferance.FileLabel.Text = "";
//                MainWindowReferance.Highlighter.Visibility = Visibility.Hidden;
//            }

//            void IconImage_MouseEnter(object sender, MouseEventArgs e)
//            {
//                var Margin = IconImage.Margin;
//                Margin.Left -= 5;
//                Margin.Top -= 5;
//                MainWindowReferance.Highlighter.Margin = Margin;
//                MainWindowReferance.Highlighter.Visibility = Visibility.Visible;
//                MainWindowReferance.FileLabel.Text = Path.GetFileNameWithoutExtension(LinkPath);
//            }

//            void t_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
//            {
//                if (e.ClickCount == 2)
//                {
//                    try
//                    {
//                        switch (Path.GetExtension(LinkPath))
//                        {
//                            case "":
//                                if (MainWindowReferance.AlternateOpening.IsChecked == false)
//                                {
//                                    MainWindowReferance.FoldersBack.Push(LinkPath);
//                                    MainWindowReferance.InitializeGrid(LinkPath);
//                                    break;
//                                }
//                                else
//                                {
//                                    goto default;
//                                }
//                            default:
//                                ProcessStartInfo App = new ProcessStartInfo();
//                                App.UseShellExecute = true;
//                                App.WorkingDirectory = Path.GetDirectoryName(LinkPath);
//                                App.FileName = Path.GetFileName(LinkPath);
//                                Process.Start(App);
//                                break;
//                        }
//                    }
//                    catch
//                    {
//                        MainWindowReferance.DeleteEntry(MainWindowReferance.DisplayMessage("The file orfolder is not working anymore, probably because it does not exist anymore..\n\n\tDo you want to remove the file \"" + Path.GetFileNameWithoutExtension(LinkPath) + "\" from this library?", "Delete Conformation", MessageBoxButton.YesNo), LinkPath, LIBRARIES_DIRECTORY + MainWindowReferance.Category.Content.ToString() + ".boxolibrary");
//                        MainWindowReferance.InitializeGrid(RESOURCES_DIRECTORY + MainWindowReferance.Category.Content.ToString() + ".boxolibrary");
//                    }
//                }
//            }
//        }

//        static SqlConnection dataBaseConnection = new SqlConnection("Data Source=(LocalDB)\\v11.0;AttachDbFilename=" + "\"G:\\My App Creations\\C# Projects\\Utility Apps\\Widgets\\GamesWIndow\\GamesWindow\\FilesSystemDataBase.mdf\";Integrated Security=True;Connect Timeout=30");
//        SqlDataAdapter library = new SqlDataAdapter("SELECT * FROM Library", dataBaseConnection);
//        SqlDataAdapter files = new SqlDataAdapter("SELECT * FROM Files", dataBaseConnection);
//        SqlCommandBuilder queryBuilder = new SqlCommandBuilder();

//        DataSet explorerData = new DataSet();

//        static int NO_OF_COLUMNS = 5;
//        List<IconButtons> iconGrid;

//        public MainWindow()
//        {
//            InitializeComponent();

//            boxoWindow = new Boxo(this);
//            boxoWindow.Show();
//            this.Hide();

//            HelpAgent.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Teen);
//            string userName = "";
//            foreach(char i in Environment.UserName)
//            {
//                if(i != '.')
//                {
//                    userName += i;
//                }
//                else
//                {
//                    userName += " ";
//                }
//            }
//            HelpAgent.Speak("Hello " + userName);

//            InitalizeWindow();
//        }

//        public MessageBoxResult DisplayMessage(string textToSpeak, string caption, MessageBoxButton buttons)
//        {
//            HelpAgent.SpeakAsyncCancelAll();
//            HelpAgent.SpeakAsync(textToSpeak);
//            return MessageBox.Show(textToSpeak, caption, buttons);
//        }

//        public void InitalizeWindow()
//        {
//            library.Fill(explorerData, "library");
//            files.Fill(explorerData, "files");

//            Libraries.Items.Remove(AddNew);
//            Libraries.Items.Clear();

//            foreach (DataRow libraryItem in explorerData.Tables["library"].Rows)
//            {
//                Libraries.Items.Add(libraryItem[0]);
//            }

//            Libraries.Items.Add(AddNew);
//        }

//        public List<string> ReadEntries(string filePath, string libraryName)
//        {
//            List<string> results = new List<string>();


//            FileStream streamedFile = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read);
//            StreamReader reader = new StreamReader(streamedFile);
//            while(reader.EndOfStream != true)
//            {
//                results.Add(reader.ReadLine());
//            }
//            reader.Close();
//            streamedFile.Close();

//            return results;
//        }

//        public void InitializeGrid(string libraryName)
//        {
//            IconButtons.MainWindowReferance = this;
//            iconGrid = new List<IconButtons>();
//            ButtonGrid.Children.Clear();
//            ButtonGrid.Children.Add(Highlighter);

//            if (Path.GetExtension(libraryName) == "")
//            {
//                int filesFoldersCount = 0;

//                foreach (string file in Directory.GetFiles(libraryName))
//                {
//                    iconGrid.Add(new IconButtons(file, Path.GetFileNameWithoutExtension(file), filesFoldersCount / NO_OF_COLUMNS, filesFoldersCount % NO_OF_COLUMNS));
//                    filesFoldersCount++;
//                }
//                foreach (string directory in Directory.GetDirectories(libraryName))
//                {
//                    iconGrid.Add(new IconButtons(directory, Path.GetFileNameWithoutExtension(directory), filesFoldersCount / NO_OF_COLUMNS, filesFoldersCount % NO_OF_COLUMNS));
//                    filesFoldersCount++;
//                }
//            }
//            else
//            {
//                DataRow[] fileData = explorerData.Tables["files"].Select("LibraryName='" + libraryName + "'");
//                for (int i = 0; i < fileData.Length; i++)
//                {
//                    iconGrid.Add(new IconButtons(fileData[i][0].ToString(), fileData[i][1].ToString(), i / NO_OF_COLUMNS, i % NO_OF_COLUMNS));
//                }
//            }
//        }

//        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
//        {
//            switch ((sender as Image).Name)
//            {
//                case "Close":
//                    this.Close();
//                    break;
//                case "Prev":
//                    if (FoldersBack.Count > 1)
//                    {
//                        FoldersNext.Push(FoldersBack.Pop());
//                        InitializeGrid(FoldersBack.Peek());
//                    }
//                    break;
//                case "Next":
//                    if (FoldersNext.Count > 0)
//                    {
//                        FoldersBack.Push(FoldersNext.Pop());
//                        InitializeGrid(FoldersBack.Peek());
//                    }
//                    break;
//                case "DeleteContents":
//                    if (Libraries.SelectedIndex >= 0 && Libraries.SelectedItem.GetType().ToString() == "System.String")
//                    {
//                        string selectedLibrary = Libraries.SelectedItem.ToString();
//                        DeleteEntry(DisplayMessage("Do you want to delete the library \"" + selectedLibrary + "\" ?", "Delete Confirmation", MessageBoxButton.YesNo), Libraries.SelectedItem.ToString(), @"G:\My App Creations\C# Projects\Utility Apps\Widgets\GamesWIndow\GamesWindow\bin\Debug\Resources\Libraries.boxolibrary");
//                        File.Delete(RESOURCES_DIRECTORY + selectedLibrary +".boxolibrary");
//                        InitalizeWindow();
//                    }
//                    break;
//                case "Minimize":
//                    this.Hide();
//                    boxoWindow.Show();
//                    break;
//                case "Toggle":
//                    Process.Start("shell:::{3080F90E-D7AD-11D9-BD98-0000947B0257}");
//                    break;
//                case "MuteOff":
//                    MuteOff.Source = new BitmapImage(new Uri(RESOURCES_DIRECTORY + "btn_volume_P.png"));
//                    MuteOff.Visibility = Visibility.Hidden;
//                    MuteOn.Visibility = Visibility.Visible;
//                    HelpAgent.Volume = 0;
//                    break;
//                case "MuteOn":
//                    MuteOn.Source = new BitmapImage(new Uri(RESOURCES_DIRECTORY + "btn_mute_P.png"));
//                    HelpAgent.Volume = 100;
//                    MuteOn.Visibility = Visibility.Hidden;
//                    MuteOff.Visibility = Visibility.Visible;
//                    break;
//            }
//        }

//        public void DeleteEntry(MessageBoxResult UserChoice, string deleteEntry, string fileSource)
//        {
//            try
//            {
//                if (UserChoice == MessageBoxResult.Yes)
//                {
//                    if (fileSource == "library")
//                    {
//                        explorerData.Tables["library"].Rows.Remove(explorerData.Tables["library"].Rows.Find(deleteEntry));
//                    }
//                    else
//                    {
//                        explorerData.Tables["files"].Rows.Remove(explorerData.Tables["files"].Rows.Find(new object[] { deleteEntry, fileSource }));
//                    }
//                }
//            }
//            catch
//            {

//            }
//        }

//        private void Image_MouseEnter(object sender, MouseEventArgs e)
//        {
//            Image buttonImage = sender as Image;
//            string buttonSource = buttonImage.Source.ToString();
//            buttonSource = buttonSource.Substring(0, buttonSource.Length - 5) + "H" + ".png";
//            buttonImage.Source = new BitmapImage(new Uri(buttonSource));
//        }

//        private void Image_MouseLeave(object sender, MouseEventArgs e)
//        {
//            Image buttonImage = sender as Image;
//            string buttonSource = buttonImage.Source.ToString();
//            buttonSource = buttonSource.Substring(0, buttonSource.Length - 5) + "P" + ".png";
//            buttonImage.Source = new BitmapImage(new Uri(buttonSource));
//        }

//        private void IconWindow_Drop(object sender, DragEventArgs e)
//        {
//            if(Libraries.SelectedIndex > -1 && Category.Content.ToString() == Libraries.SelectedItem.ToString())
//            {
//                foreach (var filePath in (string[])e.Data.GetData(DataFormats.FileDrop))
//                {
//                    explorerData.Tables["files"].Rows.Add(new Object[] { Path.GetFileNameWithoutExtension(filePath), filePath, Category.Content.ToString() });
//                }
//                InitializeGrid(RESOURCES_DIRECTORY + Category.Content.ToString() + ".boxolibrary");
//            }
//            else
//            {
//                DisplayMessage("You cannot add items here.. Please go back to any one of the libraries and add the item", "Cannot add item", MessageBoxButton.OK);
//            }
//        }

//        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
//        {
//            try { this.DragMove(); }
//            catch { }
//        }

//        private void Libraries_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (Libraries.SelectedIndex >= 0 && Libraries.SelectedItem.GetType().ToString() != "Controls.ListBoxItem")
//            {
//                try
//                {
//                    FoldersBack.Clear();
//                    FoldersNext.Clear();
//                    Category.Content = (sender as ListBox).SelectedItem.ToString();
//                    FoldersBack.Push(Libraries.SelectedItem.ToString());
//                    InitializeGrid(Libraries.SelectedItem.ToString());
//                }
//                catch
//                {
//                    //AddEntry(new string[]{Libraries.SelectedItem.ToString()}, RESOURCES_DIRECTORY + Libraries.SelectedItem.ToString() + ".boxolibrary");
//                }
//            }
//        }

//        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
//        {
//            new AddItemsWindow().Show();
//        }

//        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
//        {
//            UpdateDataBase();
//            if (this.IsActive == true)
//            {
//                boxoWindow.Close();
//            }
//        }

//        private void AddNewButton_PreviewKeyDown(object sender, KeyEventArgs e)
//        {
//            try
//            {
//                if (e.Key == Key.Enter && AddNewButton.Text != "" && AddNewButton.Text != "+")
//                {
//                    explorerData.Tables["library"].Rows.Add(new Object[] { AddNewButton.Text });
//                    UpdateDataBase();
//                    InitalizeWindow();
//                    AddNewButton.FontSize = 26;
//                    AddNewButton.FontWeight = FontWeights.ExtraBold;
//                }
//            }
//            catch
//            {
//                DisplayMessage("Enter a valid Library Name", "InvalidateArrange Library", MessageBoxButton.OK);
//            }
//        }

//        private void AddNewButton_LostFocus(object sender, RoutedEventArgs e)
//        {
//            AddNewButton.Text = "+";
//            AddNewButton.FontSize = 26;
//            AddNewButton.FontWeight = FontWeights.ExtraBold;
//        }

//        private void AddNewButton_GotFocus(object sender, RoutedEventArgs e)
//        {
//            AddNewButton.Clear();
//            AddNewButton.FontSize = 15;
//            AddNewButton.FontWeight = FontWeights.Normal;
//        }

//        public void UpdateDataBase()
//        {
//            queryBuilder.DataAdapter = library;
//            library.UpdateCommand = queryBuilder.GetUpdateCommand();
//            library.DeleteCommand = queryBuilder.GetDeleteCommand();
//            library.InsertCommand = queryBuilder.GetInsertCommand();
//            library.Update(explorerData.Tables["library"]);

//            //queryBuilder.DataAdapter = files;
//            //files.UpdateCommand = queryBuilder.GetUpdateCommand();
//            //files.DeleteCommand = queryBuilder.GetDeleteCommand();
//            //files.InsertCommand = queryBuilder.GetInsertCommand();
//            //files.Update(explorerData.Tables["files"]);
//        }
//    }
//}

//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.Diagnostics;
//using System.IO;
//using System.Speech.Synthesis;
//using System.Windows;
//using Controls;
//using Input;
//using Media;
//using Media.Imaging;
//using Threading;

//namespace GamesWindow
//{
//    /// <summary>
//    /// Interaction logic for MainWindow.xaml
//    /// </summary>
//    public partial class MainWindow : Window
//    {
//        Boxo boxoWindow;

//        Stack<string> FoldersBack = new Stack<string>();
//        Stack<string> FoldersNext = new Stack<string>();
//        static string RESOURCES_DIRECTORY = Environment.CurrentDirectory + @"\Resources\";
//        static string LIBRARIES_DIRECTORY = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Kappspot\Boxo The Explorer\Libraries\";
//        SpeechSynthesizer HelpAgent = new SpeechSynthesizer();


//        class IconButtons
//        {
//            static double SIZE_MULTIPLIER = 50;
//            static double MARGIN = 10;

//            public static MainWindow MainWindowReferance = null;

//            public Image IconImage { get; set; }
//            public string LinkPath { get; set; }
//            public string FileName { get; set; }

//            static bool ThumbnailCallBack()
//            {return false;}

//            Drawing.Image.GetThumbnailImageAbort CallBackDelegate = new Drawing.Image.GetThumbnailImageAbort(ThumbnailCallBack);

//            public void SetIconSource()
//            {
//                try
//                {
//                    switch (Path.GetExtension(LinkPath).ToLower())
//                    {
//                        case ".jpg":
//                        case ".png":
//                        case ".bmp":
//                            /**
//                             * Drawing.Bitmap image = new Drawing.Bitmap(LinkPath);
//                             * MemoryStream imageStream = new MemoryStream();
//                             * image.GetThumbnailImage(50, 50, CallBackDelegate, IntPtr.Zero).Save(imageStream, Drawing.Imaging.ImageFormat.Png);
//                             * PngBitmapDecoder o = new PngBitmapDecoder(imageStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
//                             * IconImage.Source = o.Frames[0];
//                             */

//                            BitmapImage ImageIcon = new BitmapImage();

//                            ImageIcon.BeginInit();
//                            ImageIcon.UriSource = new Uri(LinkPath);
//                            ImageIcon.DecodePixelWidth = 50;
//                            ImageIcon.EndInit();

//                            IconImage.Source = ImageIcon;
//                            break;
//                        case "":
//                            {
//                                MemoryStream IconSaveStream = new MemoryStream();
//                                Drawing.Icon IconSource = new Drawing.Icon(RESOURCES_DIRECTORY + "folder open.ico", 100, 100);
//                                Drawing.Bitmap icon = IconSource.ToBitmap();
//                                icon.Save(IconSaveStream, Drawing.Imaging.ImageFormat.Png);
//                                PngBitmapDecoder IconDecoder = new PngBitmapDecoder(IconSaveStream, BitmapCreateOptions.None, BitmapCacheOption.None);
//                                IconImage.Source = IconDecoder.Frames[0];
//                                break;
//                            }
//                        default:
//                        case ".exe":
//                            try
//                            {
//                                MemoryStream IconSaveStream = new MemoryStream();
//                                Drawing.Icon IconSource = Drawing.Icon.ExtractAssociatedIcon(LinkPath);
//                                Drawing.Bitmap icon = IconSource.ToBitmap();
//                                icon.Save(IconSaveStream, Drawing.Imaging.ImageFormat.Png);
//                                PngBitmapDecoder IconDecoder = new PngBitmapDecoder(IconSaveStream, BitmapCreateOptions.None, BitmapCacheOption.None);
//                                IconImage.Source = IconDecoder.Frames[0];

//                                //Drawing.Icon icon = Drawing.Icon.ExtractAssociatedIcon(Directory.GetCurrentDirectory() + @"\Resources\" + Category + @"\" + fileName + @"\" + fileName + ".exe");
//                                //MemoryStream IconSaveStream = new MemoryStream();
//                                //Drawing.Bitmap IconImage = icon.ToBitmap();
//                                //IconImage.Save(IconSaveStream, Drawing.Imaging.ImageFormat.Png);

//                                //BitmapImage IconDecoder = new BitmapImage();
//                                //IconDecoder.BeginInit();
//                                //IconSaveStream.Seek(0, SeekOrigin.Begin);
//                                //IconDecoder.StreamSource = IconSaveStream;
//                                //IconDecoder.EndInit();
//                                //IconImage.Source = IconDecoder;
//                            }
//                            catch
//                            {
//                                MemoryStream IconSaveStream = new MemoryStream();
//                                Drawing.Icon IconSource = new Drawing.Icon(Drawing.SystemIcons.Question, 200, 200);
//                                Drawing.Bitmap icon = IconSource.ToBitmap();
//                                icon.Save(IconSaveStream, Drawing.Imaging.ImageFormat.Png);
//                                PngBitmapDecoder IconDecoder = new PngBitmapDecoder(IconSaveStream, BitmapCreateOptions.None, BitmapCacheOption.None);
//                                IconImage.Source = IconDecoder.Frames[0];
//                            }
//                            break;
//                    }
//                }
//                catch
//                {

//                }
//            }

//            public IconButtons(string linkPath, int rowIndex, int columnIndex)
//            {
//                IconImage = new Image();
//                IconImage.Name = "Button" + rowIndex + columnIndex;

//                var iconMargin = IconImage.Margin;
//                iconMargin.Left = columnIndex * (SIZE_MULTIPLIER + MARGIN) + MARGIN;
//                iconMargin.Top = rowIndex * (SIZE_MULTIPLIER + MARGIN) + MARGIN;
//                IconImage.Margin = iconMargin;

//                IconImage.Height = SIZE_MULTIPLIER;
//                IconImage.Width = SIZE_MULTIPLIER;
//                IconImage.HorizontalAlignment = HorizontalAlignment.Left;
//                IconImage.VerticalAlignment = VerticalAlignment.Top;

//                LinkPath = linkPath;
//                IconImage.Stretch = Stretch.Fill;

//                //t = new BitmapImage();
//                //t.DecodePixelHeight = 1;
//                //t.DecodePixelWidth = 1;

//                SetIconSource();

//                RenderOptions.SetBitmapScalingMode(IconImage, BitmapScalingMode.HighQuality);

//                IconImage.MouseLeftButtonDown += IconImage_MouseLeftButtonDown;
//                IconImage.MouseEnter += IconImage_MouseEnter;
//                IconImage.MouseLeave += IconImage_MouseLeave;
//                IconImage.MouseRightButtonDown += IconImage_MouseRightButtonDown;

//                MainWindowReferance.ButtonGrid.Children.Add(IconImage);
//            }
            
//            void IconImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
//            {
//                if (MainWindowReferance.FoldersBack.Count == 1)
//                {
//                    MainWindowReferance.DeleteEntry(MainWindowReferance.DisplayMessage("Do you want to remove the file \"" + Path.GetFileNameWithoutExtension(LinkPath) + "\" from this library?", "Delete Conformation", MessageBoxButton.YesNo), LinkPath, LIBRARIES_DIRECTORY + MainWindowReferance.Category.Content.ToString() + ".boxolibrary");
//                    MainWindowReferance.InitializeGrid(LIBRARIES_DIRECTORY + MainWindowReferance.Category.Content.ToString() + ".boxolibrary");
//                }
//            }

//            void IconImage_MouseLeave(object sender, MouseEventArgs e)
//            {
//                MainWindowReferance.FileLabel.Text = "";
//                MainWindowReferance.Highlighter.Visibility = Visibility.Hidden;
//            }

//            void IconImage_MouseEnter(object sender, MouseEventArgs e)
//            {
//                var Margin = IconImage.Margin;
//                Margin.Left -= 5;
//                Margin.Top -= 5;
//                MainWindowReferance.Highlighter.Margin = Margin;
//                MainWindowReferance.Highlighter.Visibility = Visibility.Visible;
//                MainWindowReferance.FileLabel.Text = Path.GetFileNameWithoutExtension(LinkPath);
//            }

//            void IconImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
//            {
//                if (e.ClickCount == 2)
//                {
//                    try
//                    {
//                        switch (Path.GetExtension(LinkPath))
//                        {
//                            case "":
//                                if (MainWindowReferance.AlternateOpening.IsChecked == false)
//                                {
//                                    MainWindowReferance.FoldersBack.Push(LinkPath);
//                                    MainWindowReferance.InitializeGrid(LinkPath);
//                                    break;
//                                }
//                                else
//                                {
//                                    goto default;
//                                }
//                            default:
//                                ProcessStartInfo App = new ProcessStartInfo();
//                                App.UseShellExecute = true;
//                                App.WorkingDirectory = Path.GetDirectoryName(LinkPath);
//                                App.FileName = Path.GetFileName(LinkPath);
//                                Process.Start(App);
//                                break;
//                        }
//                    }
//                    catch
//                    {
//                        if (MainWindowReferance.FoldersBack.Count == 1)
//                        {
//                            MainWindowReferance.DeleteEntry(MainWindowReferance.DisplayMessage("The file or folder is not avilable anymore \n\nDo you want to remove the file \"" + Path.GetFileNameWithoutExtension(LinkPath) + "\" from this library?", "Delete Conformation", MessageBoxButton.YesNo), LinkPath, LIBRARIES_DIRECTORY + MainWindowReferance.Category.Content.ToString() + ".boxolibrary");
//                            MainWindowReferance.InitializeGrid(LIBRARIES_DIRECTORY + MainWindowReferance.Category.Content.ToString() + ".boxolibrary");
//                        }
//                    }
//                }
//            }
//        }

//        //class Deque<T>
//        //{
//        //    List<T> list = new List<T>();

//        //    Deque(int capacity)
//        //    {
//        //        list.Capacity = capacity;
//        //    }

//        //    public void PushFront(T objectPushed)
//        //    {
//        //        if (list.Count < list.Capacity)
//        //        {
//        //            list.Insert(0, objectPushed);
//        //        }
//        //        else
//        //        {
//        //            PopFront();
//        //            list.Insert(0, objectPushed);
//        //        }
//        //    }

//        //    public void PushBack(T objectPushed)
//        //    {
//        //        if (list.Count < list.Capacity)
//        //        {
//        //            list.Insert(list.Count, objectPushed);
//        //        }
//        //        else
//        //        {
//        //            PopFront();
//        //            list.Insert(list.Count, objectPushed);
//        //        }
//        //    }

//        //    public T PopFront()
//        //    {
//        //        if (list.Count > 0)
//        //        {
//        //            T objectPopped = list[0];
//        //            list.RemoveAt(0);
//        //            return objectPopped;
//        //        }
//        //        else
//        //        {
//        //            return default(T);
//        //        }
//        //    }

//        //    public T PopBack()
//        //    {
//        //        if (list.Count > 0)
//        //        {
//        //            T objectPopped = list[list.Count - 1];
//        //            list.RemoveAt(list.Count - 1);
//        //            return objectPopped;
//        //        }
//        //        else
//        //        {
//        //            return default(T);
//        //        }
//        //    }
//        //}
        
//        static int NO_OF_COLUMNS = 5;
//        List<IconButtons> iconGrid;

//        int noOfItems = 0, currentItem = 0;
//        DispatcherTimer updateTimer = new DispatcherTimer();
//        Queue<string> libraryEntries = new Queue<string>();
//        BitmapImage[] loadCursorImage = new BitmapImage[30];

//        public MainWindow()
//        {
//            InitializeComponent();

//            HelpAgent.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Teen);

//            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Kappspot\MiniMetro"))
//            {
//                string userName = "";
//                foreach (char i in Environment.UserName)
//                {
//                    if (i != '.')
//                    {
//                        userName += i;
//                    }
//                    else
//                    {
//                        userName += " ";
//                    }
//                }
//                HelpAgent.SpeakAsync("Hello " + userName);
//            }

//            boxoWindow = new Boxo(this);
//            boxoWindow.Show();


//            for (int i = 0; i < 30; i++ )
//            {
//                loadCursorImage[i] = new BitmapImage(new Uri(RESOURCES_DIRECTORY + @"\Animation\loadCursor" + (i+1) + ".png"));
//            }
//            updateTimer.Interval = TimeSpan.FromMilliseconds(5);
//            updateTimer.Tick += updateTimer_Tick;

//            InitalizeWindow();

//            this.Hide();
//        }

//        public MessageBoxResult DisplayMessage(string textToSpeak, string caption, MessageBoxButton buttons)
//        {
//            HelpAgent.SpeakAsyncCancelAll();
//            HelpAgent.SpeakAsync(textToSpeak);
//            return MessageBox.Show(textToSpeak, caption, buttons);
//        }

//        public void InitalizeWindow()
//        {
//            Libraries.Items.Clear();

//            foreach (string library in ReadEntries(LIBRARIES_DIRECTORY + "Libraries.boxolibrary"))
//            {
//                if (library != "")
//                {
//                    Libraries.Items.Add(library);
//                }
//            }

//            Libraries.Items.Add(AddNew);
//        }

//        public List<string> ReadEntries(string filePath)
//        {
//            List<string> results = new List<string>();

//            try
//            {
//                FileStream streamedFile = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read);
//                StreamReader reader = new StreamReader(streamedFile);
//                while (reader.EndOfStream != true)
//                {
//                    results.Add(reader.ReadLine());
//                }
//                reader.Close();
//                streamedFile.Close();
//            }
//            catch(Exception e)
//            {
//                MessageBox.Show(e.Message);
//            }
//            return results;
//        }

//        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
//        {
//            if (updateTimer.IsEnabled == false && libraryEntries.Count >0)
//            {
//                LoadCursor.Visibility = Visibility.Visible;
//                updateTimer.Start();
//            }
//        }

//        private void updateTimer_Tick(object sender, EventArgs e)
//        {
//            //indi.Content = noOfItems + "   " + currentItem + "   " + libraryEntries.Count;
//            noOfItems = 35 + (int)((Scroller.VerticalOffset - 5) / 50) * 5;
//            if(currentItem == -1)
//            {
//                noOfItems = 45;
//                LoadCursor.Visibility = Visibility.Visible;
//                ButtonGrid.Children.Clear();
//                ButtonGrid.Children.Add(Highlighter);
//                currentItem++;
//            }
//            if (currentItem < noOfItems && libraryEntries.Count > 0)
//            {
//                iconGrid.Add(new IconButtons(libraryEntries.Dequeue(), currentItem / NO_OF_COLUMNS, currentItem % NO_OF_COLUMNS));
//                LoadCursor.Source = loadCursorImage[currentItem % 30];
//                currentItem++;
//            }
//            else
//            {
//                LoadCursor.Visibility = Visibility.Hidden;
//                updateTimer.Stop();
//            }
//        }

//        public void InitializeGrid(string locatorURL)
//        {
//            Scroller.ScrollToHome();
//            updateTimer.Stop();
//            libraryEntries.Clear();

//            IconButtons.MainWindowReferance = this;
//            iconGrid = new List<IconButtons>();
//            ButtonGrid.Children.Clear();
//            ButtonGrid.Children.Add(Highlighter);

//            Category.Content = Path.GetFileNameWithoutExtension(locatorURL);

//            if (Path.GetExtension(locatorURL) == "")
//            {
//                foreach (string file in Directory.GetFiles(locatorURL))
//                {
//                    libraryEntries.Enqueue(file);
//                    //iconGrid.Add(new IconButtons(file, filesFoldersCount / NO_OF_COLUMNS, filesFoldersCount % NO_OF_COLUMNS));
//                    //filesFoldersCount++;
//                }
//                foreach (string directory in Directory.GetDirectories(locatorURL))
//                {
//                    libraryEntries.Enqueue(directory);
//                    //iconGrid.Add(new IconButtons(directory, filesFoldersCount / NO_OF_COLUMNS, filesFoldersCount % NO_OF_COLUMNS));
//                    //filesFoldersCount++;
//                }
//            }
//            else
//            {

//                FileStream windowDataFile = new FileStream(locatorURL, FileMode.Open, FileAccess.Read);
//                StreamReader windowData = new StreamReader(windowDataFile);

//                windowData.ReadLine();

//                string filePath;
//                //for (int i = 0; windowData.EndOfStream != true; i++)
//                //{
//                //    for (int j = 0; j < NO_OF_COLUMNS; j++)
//                //    {
//                //        while ((filePath = windowData.ReadLine()) != null)
//                //        {
//                //            if (filePath != "")
//                //            {
//                //                iconGrid.Add(new IconButtons(filePath, i, j));
//                //                break;
//                //            }
//                //        }
//                //    }
//                //}

//                while ((filePath = windowData.ReadLine()) != null)
//                {
//                    if (filePath != "")
//                    {
//                        libraryEntries.Enqueue(filePath);
//                    }
//                }
//                windowData.Close();
//                windowDataFile.Close();
//            }

//            currentItem = -1;
//            updateTimer.Start();
//        }

//        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
//        {
//            switch ((sender as Image).Name)
//            {
//                case "Close":
//                    this.Close();
//                    break;
//                case "Prev":
//                    if (FoldersBack.Count > 1)
//                    {
//                        FoldersNext.Push(FoldersBack.Pop());
//                        InitializeGrid(FoldersBack.Peek());
//                    }
//                    break;
//                case "Next":
//                    if (FoldersNext.Count > 0)
//                    {
//                        FoldersBack.Push(FoldersNext.Pop());
//                        InitializeGrid(FoldersBack.Peek());
//                    }
//                    break;
//                case "DeleteContents":
//                    if (Libraries.SelectedIndex >= 0 && Libraries.SelectedItem.GetType().ToString() == "System.String")
//                    {
//                        string selectedLibrary = Libraries.SelectedItem.ToString();
//                        DeleteEntry(DisplayMessage("Do you want to delete the library \"" + selectedLibrary + "\" ?", "Delete Confirmation", MessageBoxButton.YesNo), Libraries.SelectedItem.ToString(), LIBRARIES_DIRECTORY + "Libraries.boxolibrary");
//                        File.Delete(LIBRARIES_DIRECTORY + selectedLibrary + ".boxolibrary");
                        
//                        FoldersBack.Clear();
//                        FoldersNext.Clear();
//                        InitalizeWindow();
//                        iconGrid = new List<IconButtons>();
//                        ButtonGrid.Children.Clear();
//                        ButtonGrid.Children.Add(Highlighter);
//                    }
//                    break;
//                case "Minimize":
//                    this.Hide();
//                    boxoWindow.Show();
//                    break;
//                case "Toggle":
//                    Process.Start("shell:::{3080F90E-D7AD-11D9-BD98-0000947B0257}");
//                    break;
//                case "MuteOff":
//                    MuteOff.Source = new BitmapImage(new Uri(RESOURCES_DIRECTORY + "btn_volume_P.png"));
//                    MuteOff.Visibility = Visibility.Hidden;
//                    MuteOn.Visibility = Visibility.Visible;
//                    HelpAgent.Volume = 0;
//                    break;
//                case "MuteOn":
//                    MuteOn.Source = new BitmapImage(new Uri(RESOURCES_DIRECTORY + "btn_mute_P.png"));
//                    HelpAgent.Volume = 100;
//                    MuteOn.Visibility = Visibility.Hidden;
//                    MuteOff.Visibility = Visibility.Visible;
//                    break;
//            }
//        }

//        public void DeleteEntry(MessageBoxResult UserChoice, string deleteEntry, string filePath)
//        {
//            try
//            {
//                if (UserChoice == MessageBoxResult.Yes)
//                {
//                    List<string> windowData = new List<string>();

//                    FileStream streamedFile = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
//                    StreamReader reader = new StreamReader(streamedFile);
//                    while (reader.EndOfStream != true)
//                    {
//                        windowData.Add(reader.ReadLine());
//                    }
//                    reader.Close();

//                    streamedFile = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);

//                    StreamWriter writer = new StreamWriter(streamedFile);
//                    foreach (string i in windowData)
//                    {
//                        if (i != "" && i != deleteEntry)
//                        {
//                            writer.WriteLine(i);
//                        }
//                    }
//                    writer.Flush();

//                    streamedFile.Close();

//                    InitalizeWindow();
//                }
//            }
//            catch
//            {

//            }
//        }

//        public void AddEntry(string[] addEntries, string filePath)
//        {
//            try
//            {
//                foreach (string addEntry in addEntries)
//                {
//                    FileStream streamedFile = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read);
//                    StreamReader reader = new StreamReader(streamedFile);

//                    bool entryAlreadyExists = false;
//                    reader.ReadLine();
//                    while (reader.EndOfStream != true)
//                    {
//                        if (reader.ReadLine() == addEntry)
//                        {
//                            entryAlreadyExists = true;
//                        }
//                    }
//                    reader.Close();
//                    streamedFile.Close();

//                        if (entryAlreadyExists == false)
//                        {
//                            streamedFile = new FileStream(filePath, FileMode.Append, FileAccess.Write); ;
//                            StreamWriter writer = new StreamWriter(streamedFile);
//                            writer.WriteLine(addEntry);
//                            writer.Flush();
//                            streamedFile.Close();
//                        }
//                        else
//                        {
//                            DisplayMessage(" Sorry! But This entry already exists...", "Redundant entry", MessageBoxButton.OK);
//                        }
//                }
//            }
//            catch
//            {

//            }
//        }

//        private void Image_MouseEnter(object sender, MouseEventArgs e)
//        {
//            Image buttonImage = sender as Image;
//            string buttonSource = buttonImage.Source.ToString();
//            buttonSource = buttonSource.Substring(0, buttonSource.Length - 5) + "H" + ".png";
//            buttonImage.Source = new BitmapImage(new Uri(buttonSource));
//        }

//        private void Image_MouseLeave(object sender, MouseEventArgs e)
//        {
//            Image buttonImage = sender as Image;
//            string buttonSource = buttonImage.Source.ToString();
//            buttonSource = buttonSource.Substring(0, buttonSource.Length - 5) + "P" + ".png";
//            buttonImage.Source = new BitmapImage(new Uri(buttonSource));
//        }

//        private void IconWindow_Drop(object sender, DragEventArgs e)
//        {
//            if (Libraries.SelectedIndex > -1 && Category.Content.ToString() == Libraries.SelectedItem.ToString())
//            {
//                string[] addEntries = (string[])e.Data.GetData(DataFormats.FileDrop);
//                AddEntry(addEntries, LIBRARIES_DIRECTORY + Libraries.SelectedItem.ToString() + ".boxolibrary");
//                InitializeGrid(LIBRARIES_DIRECTORY + Category.Content.ToString() + ".boxolibrary");
//            }
//            else
//            {
//                DisplayMessage("You cannot add items here.. Please go back to any one of the libraries and add the item", "Cannot add item", MessageBoxButton.OK);
//            }
//        }

//        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
//        {
//            try { this.DragMove(); }
//            catch { }
//        }

//        private void Libraries_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (Libraries.SelectedIndex >= 0 && Libraries.SelectedItem.GetType().ToString() != "Controls.ListBoxItem")
//            {
//                try
//                {
//                    LibraryLabel.Content = Libraries.SelectedItem.ToString();
//                    FoldersBack.Clear();
//                    FoldersNext.Clear();
//                    Category.Content = (sender as ListBox).SelectedItem.ToString();
//                    FoldersBack.Push(LIBRARIES_DIRECTORY + Libraries.SelectedItem.ToString() + ".boxolibrary");
//                    InitializeGrid(LIBRARIES_DIRECTORY + Libraries.SelectedItem.ToString() + ".boxolibrary");
//                }
//                catch
//                {
//                    AddEntry(new string[] { Libraries.SelectedItem.ToString() }, LIBRARIES_DIRECTORY + Libraries.SelectedItem.ToString() + ".boxolibrary");
//                }
//            }
//            else
//            {
//                if(Libraries.SelectedIndex >= 0)
//                {
//                    LibraryLabel.Content = "Add a New Library";
//                }
//            }
//        }

//        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
//        {
//            new AddItemsWindow().Show();

//        }

//        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
//        {
//            if (this.IsActive == true)
//            {
//                boxoWindow.Close();
//            }
//        }

//        private void AddNewButton_PreviewKeyDown(object sender, KeyEventArgs e)
//        {
//            LibraryLabel.Content = "Add a New Library";
//            try
//            {
//                if (e.Key == Key.Enter && AddNewButton.Text != "" && AddNewButton.Text != "+")
//                {
//                    AddEntry(new string[] { AddNewButton.Text }, LIBRARIES_DIRECTORY + "Libraries.boxolibrary");
//                    InitalizeWindow();
//                    AddNewButton.FontSize = 26;
//                    AddNewButton.FontWeight = FontWeights.ExtraBold;
//                }
//            }
//            catch
//            {
//                DisplayMessage("Enter a valid Library Name", "InvalidateArrange Library", MessageBoxButton.OK);
//            }
//        }

//        private void AddNewButton_LostFocus(object sender, RoutedEventArgs e)
//        {
//            AddNewButton.Text = "+";
//            AddNewButton.FontSize = 26;
//            AddNewButton.FontWeight = FontWeights.ExtraBold;
//        }

//        private void AddNewButton_GotFocus(object sender, RoutedEventArgs e)
//        {
//            LibraryLabel.Content = "Add a New Library";
//            AddNewButton.Clear();
//            AddNewButton.FontSize = 15;
//            AddNewButton.FontWeight = FontWeights.Normal;
//        }
//    }
//}



////using System;
////using System.Diagnostics;
////using 
////using System.Windows;
////using Controls;
////using Input;
////using Media;
////using Media.Imaging;

////namespace GamesWindow
////{
////    /// <summary>
////    /// Interaction logic for MainWindow.xaml
////    /// </summary>
////    public partial class MainWindow : Window
////    {
////        class IconButtons
////        {
////            static double WIDTH_MULTIPLIER = 50;
////            static double HEIGHT_MULTIPLIER = 50;
////            static double MARGIN = 10;

////            public static MainWindow MainWindowReferance = null;

////            public static string DataLocation { get; set; }

////            Image IconImage;

////            public string DirectoryName { get; set; }
////            public string FileName { get; set; }


////            public IconButtons(string name, string directory, int rowIndex, int columnIndex)
////            {
////                IconImage = new Image();
////                IconImage.Name = "Button" + rowIndex + columnIndex;

////                var Margin = IconImage.Margin;
////                Margin.Left = columnIndex * (WIDTH_MULTIPLIER + MARGIN) + MARGIN;
////                Margin.Top = rowIndex * (HEIGHT_MULTIPLIER + MARGIN) + MARGIN;
////                IconImage.Margin = Margin;

////                IconImage.Height = HEIGHT_MULTIPLIER;
////                IconImage.Width = WIDTH_MULTIPLIER;
////                IconImage.HorizontalAlignment = HorizontalAlignment.Left;
////                IconImage.VerticalAlignment = VerticalAlignment.Top;

////                FileName = name;
////                DirectoryName = directory;
////                MemoryStream IconSaveStream = new MemoryStream();
////                Drawing.Bitmap icon = Drawing.Icon.ExtractAssociatedIcon(directory + FileName).ToBitmap();
////                icon.Save(IconSaveStream, Drawing.Imaging.ImageFormat.Png);
////                PngBitmapDecoder IconDecoder = new PngBitmapDecoder(IconSaveStream, BitmapCreateOptions.None, BitmapCacheOption.None);
////                IconImage.Source = IconDecoder.Frames[0];

////                //Drawing.Icon icon = Drawing.Icon.ExtractAssociatedIcon(Directory.GetCurrentDirectory() + @"\Resources\" + Category + @"\" + fileName + @"\" + fileName + ".exe");
////                //MemoryStream IconSaveStream = new MemoryStream();
////                //Drawing.Bitmap IconImage = icon.ToBitmap();
////                //IconImage.Save(IconSaveStream, Drawing.Imaging.ImageFormat.Png);

////                //BitmapImage IconDecoder = new BitmapImage();
////                //IconDecoder.BeginInit();
////                //IconSaveStream.Seek(0, SeekOrigin.Begin);
////                //IconDecoder.StreamSource = IconSaveStream;
////                //IconDecoder.EndInit();
////                //IconImage.Source = IconDecoder;

////                RenderOptions.SetEdgeMode(IconImage, EdgeMode.Aliased);
////                RenderOptions.SetBitmapScalingMode(IconImage, BitmapScalingMode.HighQuality);
////                IconImage.Stretch = Stretch.Fill;

////                IconImage.MouseLeftButtonDown += t_MouseLeftButtonDown;
////                IconImage.MouseEnter += IconImage_MouseEnter;
////                IconImage.MouseLeave += IconImage_MouseLeave;

////                FileName = name;

////                ButtonGrid.Children.Add(IconImage);
////            }

////            void IconImage_MouseLeave(object sender, MouseEventArgs e)
////            {
////                HighLighter.Visibility = Visibility.Hidden;
////            }

////            void IconImage_MouseEnter(object sender, MouseEventArgs e)
////            {
////                var Margin = IconImage.Margin;
////                Margin.Left -= 5;
////                Margin.Top -= 5;
////                HighLighter.Margin = Margin;
////                HighLighter.Visibility = Visibility.Visible;
////                FileLabel.Content = FileName;
////            }

////            void t_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
////            {
////                ProcessStartInfo App = new ProcessStartInfo();
////                App.UseShellExecute = true;
////                App.WorkingDirectory = DirectoryName;
////                App.FileName = FileName;
////                Process.Start(App);
////            }
////        }

////        static int NO_OF_COLUMNS = 4, NO_OF_ROWS = 4;
////        IconButtons[,] iconGrid = new IconButtons[NO_OF_ROWS, NO_OF_COLUMNS];

////        public MainWindow()
////        {
////            string locatorURL = @"Resources\Games.boxolibrary";

////            InitializeComponent();

////            IconButtons.ButtonGrid = IconWindow;
////            IconButtons.FileLabel = FileLabel;
////            IconButtons.HighLighter = Highlighter;
////            FileStream windowDataFile = new FileStream(locatorURL, FileMode.Open, FileAccess.Read);
////            StreamReader windowData = new StreamReader(windowDataFile);
////            IconButtons.Category = windowData.ReadLine();
////            IconWindow.Children.Clear();
////            IconWindow.Children.Add(Highlighter);
////            Category.Content = IconButtons.Category;
////            for (int i = 0; windowData.EndOfStream != true; i++)
////            {
////                for (int j = 0; j < NO_OF_COLUMNS; j++)
////                {
////                    if (windowData.EndOfStream != true)
////                    {
////                        iconGrid[i, j] = new IconButtons(windowData.ReadLine(), windowData.ReadLine(), i, j);
////                    }
////                    else
////                    {
////                        iconGrid[i, j] = null;
////                    }
////                }
////            }
////            windowData.Close();
////            windowDataFile.Close();
////        }

////        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
////        {
////            this.DragMove();
////        }

////        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
////        {
////            this.Close();
////        }

////        private void Image_MouseEnter(object sender, MouseEventArgs e)
////        {
////            Image buttonImage = sender as Image;
////            string buttonSource = buttonImage.Source.ToString();
////            buttonSource = buttonSource.Substring(0, buttonSource.Length - 5) + "H" + ".png";
////            buttonImage.Source = new BitmapImage(new Uri(buttonSource));
////        }

////        private void Image_MouseLeave(object sender, MouseEventArgs e)
////        {
////            Image buttonImage = sender as Image;
////            string buttonSource = buttonImage.Source.ToString();
////            buttonSource = buttonSource.Substring(0, buttonSource.Length - 5) + "P" + ".png";
////            buttonImage.Source = new BitmapImage(new Uri(buttonSource));
////        }

////        private void IconWindow_Drop(object sender, DragEventArgs e)
////        {
////            foreach (var i in (sender as string))
////                if (Category.Content != Category)
////                {
////                    FileStream windowDataFile = new FileStream(IconButtons.DataLocation, FileMode.Append, FileAccess.Read);
////                    StreamWriter windowData = new StreamWriter(windowDataFile);

////                    string directoryName = "";
////                    string fileName = "";


////                }
////        }
////    }
////}

////using System;
////using System.Diagnostics;
////using 
////using System.Windows;
////using Controls;
////using Input;
////using Media;
////using Media.Imaging;

////namespace GamesWindow
////{
////    /// <summary>
////    /// Interaction logic for MainWindow.xaml
////    /// </summary>
////    public partial class MainWindow : Window
////    {
////            static double WIDTH_MULTIPLIER = 50;
////            static double HEIGHT_MULTIPLIER = 50;
////            static double MARGIN = 10;
////            public static string DataLocation;


////            static int NO_OF_COLUMNS = 4, NO_OF_ROWS = 4;

////            class Icons
////            {
////                public string FileName { get; set; }
////                public string Directory { get; set; }
////                public Image IconImage { get; set; }
////            }
////            Icons[,] iconButtons = new Icons[NO_OF_ROWS, NO_OF_COLUMNS];

////            public void CreateIcon(string fileName, string directoryPath, int rowIndex, int columnIndex)
////            {
////                Image IconImage = new Image();
////                IconImage.Name = "Button" + rowIndex + columnIndex;

////                var Margin = IconImage.Margin;
////                Margin.Left = columnIndex * (WIDTH_MULTIPLIER + MARGIN) + MARGIN;
////                Margin.Top = rowIndex * (HEIGHT_MULTIPLIER + MARGIN) + MARGIN;
////                IconImage.Margin = Margin;

////                IconImage.Height = HEIGHT_MULTIPLIER;
////                IconImage.Width = WIDTH_MULTIPLIER;
////                IconImage.HorizontalAlignment = HorizontalAlignment.Left;
////                IconImage.VerticalAlignment = VerticalAlignment.Top;

////                Drawing.Icon icon = Drawing.Icon.ExtractAssociatedIcon(directoryPath + fileName);
////                MemoryStream IconSaveStream = new MemoryStream();
////                Drawing.Bitmap IconImage = icon.ToBitmap();
////                IconImage.Save(IconSaveStream, Drawing.Imaging.ImageFormat.Png);
////                PngBitmapDecoder IconDecoder = new PngBitmapDecoder(IconSaveStream, BitmapCreateOptions.None, BitmapCacheOption.None);
////                IconImage.Source = IconDecoder.Frames[0];

////                //Drawing.Icon icon = Drawing.Icon.ExtractAssociatedIcon(Directory.GetCurrentDirectory() + @"\Resources\" + Category + @"\" + fileName + @"\" + fileName + ".exe");
////                //MemoryStream IconSaveStream = new MemoryStream();
////                //Drawing.Bitmap IconImage = icon.ToBitmap();
////                //IconImage.Save(IconSaveStream, Drawing.Imaging.ImageFormat.Png);

////                //BitmapImage IconDecoder = new BitmapImage();
////                //IconDecoder.BeginInit();
////                //IconSaveStream.Seek(0, SeekOrigin.Begin);
////                //IconDecoder.StreamSource = IconSaveStream;
////                //IconDecoder.EndInit();
////                //IconImage.Source = IconDecoder;

////                RenderOptions.SetEdgeMode(IconImage, EdgeMode.Aliased);
////                RenderOptions.SetBitmapScalingMode(IconImage, BitmapScalingMode.HighQuality);
////                IconImage.Stretch = Stretch.Fill;

////                IconImage.MouseLeftButtonDown += t_MouseLeftButtonDown;
////                IconImage.MouseEnter += IconImage_MouseEnter;
////                IconImage.MouseLeave += IconImage_MouseLeave;

////                IconButtons.Children.Add(IconImage);

////                iconButtons[rowIndex, columnIndex] = new Icons();
////                iconButtons[rowIndex, columnIndex].Directory = directoryPath;
////                iconButtons[rowIndex, columnIndex].FileName = fileName;
////                iconButtons[rowIndex, columnIndex].IconImage = IconImage;
////            }

////            void IconImage_MouseLeave(object sender, MouseEventArgs e)
////            {
////                Highlighter.Visibility = Visibility.Hidden;
////            }

////            void IconImage_MouseEnter(object sender, MouseEventArgs e)
////            {
////                var Margin = (sender as Image).Margin;
////                Margin.Left -= 5;
////                Margin.Top -= 5;
////                Highlighter.Margin = Margin;
////                Highlighter.Visibility = Visibility.Visible;
////                FileLabel.Content = iconButtons[;
////            }

////            void t_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
////            {
////                ProcessStartInfo App = new ProcessStartInfo();
////                App.UseShellExecute = true;
////                App.WorkingDirectory = DirectoryName;
////                App.FileName = FileName;
////                Process.Start(App);
////            }

////        public MainWindow()
////        {
////            string locatorURL = @"Resources\Games.boxolibrary";

////            InitializeComponent();

////            FileStream windowDataFile = new FileStream(locatorURL, FileMode.Open, FileAccess.Read);
////            StreamReader windowData = new StreamReader(windowDataFile);
////            for (int i = 0; i < NO_OF_ROWS; i++ )
////            {
////                for(int j=0; j<NO_OF_COLUMNS; j++)
////                {

////                }
////            }
////                IconButtons.Children.Add(Highlighter);
////            Category.Content = windowData.ReadLine();
////            for (int i = 0; windowData.EndOfStream != true; i++)
////            {
////                for (int j = 0; j < NO_OF_COLUMNS; j++)
////                {
////                    if (windowData.EndOfStream != true)
////                    {
////                        CreateIcon(windowData.ReadLine(), windowData.ReadLine(), i, j);
////                    }
////                }
////            }
////            windowData.Close();
////            windowDataFile.Close();
////        }

////        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
////        {
////            this.DragMove();
////        }

////        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
////        {
////            this.Close();
////        }

////        private void Image_MouseEnter(object sender, MouseEventArgs e)
////        {
////            Image buttonImage = sender as Image;
////            string buttonSource = buttonImage.Source.ToString();
////            buttonSource = buttonSource.Substring(0, buttonSource.Length - 5) + "H" + ".png";
////            buttonImage.Source = new BitmapImage(new Uri(buttonSource));
////        }

////        private void Image_MouseLeave(object sender, MouseEventArgs e)
////        {
////            Image buttonImage = sender as Image;
////            string buttonSource = buttonImage.Source.ToString();
////            buttonSource = buttonSource.Substring(0, buttonSource.Length - 5) + "P" + ".png";
////            buttonImage.Source = new BitmapImage(new Uri(buttonSource));
////        }

////        private void IconWindow_Drop(object sender, DragEventArgs e)
////        {
////            foreach (var i in (sender as string))
////                if (Category.Content != Category)
////                {
////                    FileStream windowDataFile = new FileStream(Directory.GetCurrentDirectory() + Category.Content.ToString() + ".boxolibrary" , FileMode.Append, FileAccess.Read);
////                    StreamWriter windowData = new StreamWriter(windowDataFile);

////                    string directoryName = "";
////                    string fileName = "";


////                }
////        }
////    }
////}