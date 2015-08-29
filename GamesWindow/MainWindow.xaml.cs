using System;
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
        public MessageBoxResult DisplayMessage(string textToSpeak, string caption, MessageBoxButton buttons)
        {
            //Stop all the async speaking and start speaking the new text
            //At the same time show the new message in a message box
            HelpAgent.SpeakAsyncCancelAll();
            HelpAgent.SpeakAsync(textToSpeak);
            return MessageBox.Show(textToSpeak, caption, buttons);
        }

        /// <summary>
        ///     This initializes the various features of the Window like the List of libraries
        ///     This is used when a change is made to the library list like deletion.
        ///     This is also used for initializing the library for the first time
        /// </summary>
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
        /// This function will add the given entries to the database files and update the same
        /// </summary>
        /// <param name="addEntries">Specifies the entries to be added</param>
        /// <param name="filePath">Specifies the library path</param>
        /// <remarks>
        /// This will search the file whether the entry to be added exists or not
        /// If the entry does not exist, then it should be added otherwise an error message will be displayed
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

        /// <summary>
        /// This will initialize the grid to display the icons of the items
        /// </summary>
        /// <param name="locatorURL"></param>
        public void InitializeGrid(string locatorURL)
        {
            //Stop any previous updations and clear all the data used in the previous windows
            //scroll to the top of the grid
            Scroller.ScrollToHome();
            updateTimer.Stop();
            waitTimer.Stop();
            libraryEntries.Clear();

            //Set the mainwindows referance as this object
            //Remove all the elements in the grid except for the Highlighter icon
            IconButtons.MainWindowReferance = this;
            ButtonGrid.Children.Clear();
            ButtonGrid.Children.Add(Highlighter);

            //Set the Category label
            //Here category label is the parent folder
            Category.Content = Path.GetFileNameWithoutExtension(locatorURL);

            if (Path.GetExtension(locatorURL) == "")
            {
                //If the URL refers to a folder get the files and folders
                //within that folder and add it to the library entries list
                foreach (string file in Directory.GetFiles(locatorURL))
                {
                    if (File.GetAttributes(file) != FileAttributes.Hidden) { libraryEntries.Enqueue(file); }
                }
                foreach (string directory in Directory.GetDirectories(locatorURL)) { libraryEntries.Enqueue(directory); }
            }
            else
            {
                //If the URL refers to a library Flatfile database 

                //Open the library file for reading
                FileStream windowDataFile = new FileStream(locatorURL, FileMode.OpenOrCreate, FileAccess.Read);
                StreamReader windowData = new StreamReader(windowDataFile);

                //Reading all the content of the library file
                //and adding it to the list
                string filePath;
                while ((filePath = windowData.ReadLine()) != null)
                {
                    //Select only valid entries
                    if (filePath != "") { libraryEntries.Enqueue(filePath); }
                }

                //Close the opened file
                windowData.Close();
                windowDataFile.Close();
            }

            //Reset the update counter and start updating
            //Start the waiting animation
            currentItem = -1;
            updateTimer.Start();
            waitTimer.Start();
        }

        /// <summary>
        /// This will start updating the grid with more items as they are brought into view
        /// </summary>
        /// <param name="sender">The object Scrollviewer</param>
        /// <param name="e">The Scroll event args</param>
        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            //If we have elements in the queue waiting to be updated into the grid
            //Start the deferred update timer
            if (updateTimer.IsEnabled == false && libraryEntries.Count > 0)
            {
                LoadCursor.Visibility = Visibility.Visible;
                updateTimer.Start();
                waitTimer.Start();
            }
        }

        /// <summary>
        /// This timer is used to produce the deferred update 
        /// of the grid elements
        /// </summary>
        /// <param name="sender">The timer object</param>
        /// <param name="e">The event args</param>
        private void updateTimer_Tick(object sender, EventArgs e)
        {
            //The current no of items that should be in the grid
            //This is determined by the vertical offset of the scroll viewer
            noOfItems = INTIAL_UPDATE_COUNT + (int)(Scroller.VerticalOffset / IconButtons.TOTAL_ICON_SPACE) * NO_OF_COLUMNS;

            //If this is the first item to be added to the grid
            if (currentItem == -1)
            {
                //Reset the grid
                ButtonGrid.Height = IconButtons.TOTAL_ICON_SPACE * libraryEntries.Count / NO_OF_COLUMNS + IconButtons.TOTAL_ICON_SPACE;
                if (ButtonGrid.Height < 330) { ButtonGrid.Height = 330; }

                //Collect the garbage from the parent
                GC.Collect();

                //Set the number of the items to be updated
                //And all oher data for initial updates
                noOfItems = INTIAL_UPDATE_COUNT;
                LoadCursor.Visibility = Visibility.Visible;
                ButtonGrid.Children.Clear();
                ButtonGrid.Children.Add(Highlighter);
                currentItem++;
            }
            if (currentItem < noOfItems && libraryEntries.Count > 0)
            {
                //Deque an item from the wait queue
                //Create a button and add it to the grid - done within icon buttons
                //Update the currentItem count
                new IconButtons(libraryEntries.Dequeue(), currentItem / NO_OF_COLUMNS, currentItem % NO_OF_COLUMNS);
                currentItem++;
            }
            else
            {
                //If there are no items to be updated then
                //Stop this timer and also the wait animation timer
                //Collect the garbage if any
                LoadCursor.Visibility = Visibility.Hidden;
                updateTimer.Stop();
                waitTimer.Stop();
                GC.Collect();
            }
        }

        /// <summary>
        /// Set the operations to be performed for the various image buttons
        /// </summary>
        /// <param name="sender">The image button object that was clicked</param>
        /// <param name="e">The mouse click event args</param>
        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch ((sender as Image).Name)
            {
                case "Close":
                    //Close the window
                    this.Close();
                    break;
                case "Prev":
                    //If there is a parent folder previously opened 
                    //and recorded in the back stack
                    //Navigate to it
                    if (FoldersBack.Count > 1)
                    {
                        FoldersNext.Push(FoldersBack.Pop());
                        InitializeGrid(FoldersBack.Peek());
                    }
                    break;
                case "Next":
                    //If there is a child folder previously opened 
                    //and stored in the next stack
                    //Navigate to it
                    if (FoldersNext.Count > 0)
                    {
                        FoldersBack.Push(FoldersNext.Pop());
                        InitializeGrid(FoldersBack.Peek());
                    }
                    break;
                case "DeleteContents":
                    //If we have clicked the delete icon
                    //If a valid library item is clicked then do the following
                    if (Libraries.SelectedIndex >= 0 && Libraries.SelectedItem.GetType().ToString() == "System.String")
                    {
                        //Get the selected library to be deleted and get the user confirmation;
                        //If the user confirms the delete,
                        //delete the contents of the library file and then delete its entry in the index
                        string selectedLibrary = Libraries.SelectedItem.ToString();
                        MessageBoxResult userChoice = DisplayMessage("Do you want to delete the library \"" + selectedLibrary + "\" ?", "Delete Confirmation", MessageBoxButton.YesNo);
                        if(userChoice == MessageBoxResult.Yes)
                        {
                            DeleteEntry(userChoice, Libraries.SelectedItem.ToString(), LIBRARIES_DIRECTORY + "Libraries.boxolibrary");
                            File.Delete(LIBRARIES_DIRECTORY + selectedLibrary + ".boxolibrary");
                        }

                        //Reset the data and Ui parameters
                        FoldersBack.Clear();
                        FoldersNext.Clear();
                        InitalizeWindow();
                        ButtonGrid.Children.Clear();
                        ButtonGrid.Children.Add(Highlighter);
                    }
                    break;
                case "Minimize":
                    //Hide when asked to minimize
                    this.Hide();
                    boxoWindow.Show();
                    break;
                case "Toggle":
                    //Start the shell script that will switch between different windows
                    Process.Start("shell:::{3080F90E-D7AD-11D9-BD98-0000947B0257}");
                    break;
                case "MuteOff":
                    //If mute is turned off update the corresponding buttons image
                    //Set the volume to 0 to prevent the updates from the voice assistant
                    MuteOff.Source = new BitmapImage(new Uri(RESOURCES_DIRECTORY + "btn_volume_P.png"));
                    MuteOff.Visibility = Visibility.Hidden;
                    MuteOn.Visibility = Visibility.Visible;
                    HelpAgent.Volume = 0;
                    break;
                case "MuteOn":
                    //If the mute is turned on update the corresponding buttons image
                    //Set the volume to full and allow the voice assistant to be heard
                    MuteOn.Source = new BitmapImage(new Uri(RESOURCES_DIRECTORY + "btn_mute_P.png"));
                    HelpAgent.Volume = 100;
                    MuteOn.Visibility = Visibility.Hidden;
                    MuteOff.Visibility = Visibility.Visible;
                    break;
            }
        }

        /// <summary>
        /// Highlight the buttons when the mouse enters it
        /// </summary>
        /// <param name="sender">The image button that the mouse enters</param>
        /// <param name="e">The mouse event args</param>
        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            //Set the highlight buttom image for the image button
            Image buttonImage = sender as Image;
            string buttonSource = buttonImage.Source.ToString();
            buttonSource = buttonSource.Substring(0, buttonSource.Length - 5) + "H" + ".png";
            buttonImage.Source = new BitmapImage(new Uri(buttonSource));
        }

        /// <summary>
        /// Unhighlight the mouse button when the mouse leaves it
        /// </summary>
        /// <param name="sender">The image that was left</param>
        /// <param name="e">The event args</param>
        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            //Set the normal image file for the image button
            Image buttonImage = sender as Image;
            string buttonSource = buttonImage.Source.ToString();
            buttonSource = buttonSource.Substring(0, buttonSource.Length - 5) + "P" + ".png";
            buttonImage.Source = new BitmapImage(new Uri(buttonSource));
        }

        /// <summary>
        /// Add the dropped item to the content list 
        /// when the item is dropped in a valid place
        /// </summary>
        /// <param name="sender">The Sender</param>
        /// <param name="e">The event args</param>
        private void IconWindow_Drop(object sender, DragEventArgs e)
        {
            //If it is a valid drop place then add it to the library dropped
            //Otherwise display a warning message
            if (Libraries.SelectedIndex > -1 && Category.Content.ToString() == Libraries.SelectedItem.ToString())
            {
                string[] addEntries = (string[])e.Data.GetData(DataFormats.FileDrop);
                AddEntry(addEntries, LIBRARIES_DIRECTORY + Libraries.SelectedItem.ToString() + ".boxolibrary");
                InitializeGrid(LIBRARIES_DIRECTORY + Libraries.SelectedItem.ToString() + ".boxolibrary");
            }
            else { DisplayMessage("You cannot add items here.. Please go back to any one of the libraries and add the item", "Cannot add item", MessageBoxButton.OK); }
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