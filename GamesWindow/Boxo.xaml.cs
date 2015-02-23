using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GamesWindow
{
    /// <summary>
    /// Interaction logic for Boxo.xaml
    /// </summary>
    public partial class Boxo : Window
    {
        string RESOURCE_DIRECTORY = Directory.GetCurrentDirectory() + "\\Resources\\";
        DispatcherTimer ringAnimation = new DispatcherTimer();
        int ringCounter = 0;
        MainWindow refMainWindow;

        BitmapImage[] ringImages = new BitmapImage[40];

        public Boxo(MainWindow mainWindow)
        {
            InitializeComponent();

            for (int i = 0; i < 30; i++ )
            {
                ringImages[i] = new BitmapImage(new Uri(RESOURCE_DIRECTORY + "Animation\\loadcursor" + (i + 1) + ".png"));
            }
            refMainWindow = mainWindow;

            ringAnimation.Interval = TimeSpan.FromMilliseconds(30);
            ringAnimation.Tick += ringAnimation_Tick;
            ringAnimation.Start();
            ringAnimation.Stop();

            this.Left = 0;
            this.Top = 150;
        }

        void ringAnimation_Tick(object sender, EventArgs e)
        {
            Rings.Source = ringImages[ringCounter++ % 30];
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            BackGround.Opacity = 1;
            Bubble1.Visibility = System.Windows.Visibility.Hidden;
            Rings.Visibility = System.Windows.Visibility.Visible;
            ringAnimation.Start();
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            BackGround.Opacity = 0.7;
            Bubble1.Visibility = System.Windows.Visibility.Visible;
            Rings.Visibility = System.Windows.Visibility.Hidden;
            ringAnimation.Stop();
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.IsActive == true)
            {
                refMainWindow.Close();
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(this.Top < SystemParameters.FullPrimaryScreenHeight / 2)
            {
                if(this.Top < SystemParameters.FullPrimaryScreenHeight / 7)
                {
                    this.Top = 0;
                    if(this.Left < 0)
                    {
                        this.Left = 0;
                    }
                    else if(SystemParameters.PrimaryScreenWidth < this.Left + this.Width)
                    {
                        this.Left = SystemParameters.FullPrimaryScreenWidth - this.Width;
                    }
                }
                else if(this.Left < SystemParameters.FullPrimaryScreenWidth / 4)
                {
                    this.Left = 0;
                }
                else if(SystemParameters.FullPrimaryScreenWidth - this.Left < SystemParameters.FullPrimaryScreenWidth / 4)
                {
                    this.Left = SystemParameters.FullPrimaryScreenWidth - this.Width;
                }
                else
                {
                    this.Top = 0;
                }
            }
            else
            {
                if (this.Left < SystemParameters.FullPrimaryScreenWidth / 2)
                {
                    this.Left = 0;
                }
                else
                {
                    this.Left = SystemParameters.FullPrimaryScreenWidth - this.Width;
                }
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.Hide();
            refMainWindow.Show();
        }
    }
}
