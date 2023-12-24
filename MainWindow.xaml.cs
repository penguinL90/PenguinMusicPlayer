using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using NAudio.Wave;
using System.Runtime.Versioning;
using System.Windows.Media;
using System.Windows.Data;
using System.IO;

namespace MusicApp
{
    [SupportedOSPlatform("windows7.0")]
    public partial class MainWindow : Window
    {
        private Player player;
        public fileList FileList;
        private DispatcherTimer timer;
        private DispatcherTimer statusbartimer;
        private int TimeBarStart = 0;
        private int TimeBarEnd;
        private BitmapImage pauseImg;
        private BitmapImage playImg;
        private bool Oritimerstatus;
#pragma warning disable CS8618
        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            player = new Player();
            FileList = new();
            Listview.ItemsSource = FileList;

            ControlButtonStatusChange(player.Playable);

            this.KeyDown += new KeyEventHandler(Keydown);

            statusbartimer = new();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += SliderTimerChange;
            timer.Tick += CheckAudioPlayEndOrNext;

            VolumeBar.Value = player.Volume;
            NowPlayTxt.Text = "Nothing play yet";
            VolumeValue.Text = ((int)(player.Volume * 100)).ToString();
            AllTime.Content = TimeSpan.Zero.ToString();
            NowTime.Content = TimeSpan.Zero.ToString();
            pauseImg = bitmapInit(@"\imgs\pause.png");
            playImg = bitmapInit(@"\imgs\play.png");

            StatusBarUpdate("Ready.", 2);
        }
        private BitmapImage bitmapInit(string uri)
        {
            try
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(uri, UriKind.Relative);
                bitmapImage.DecodePixelWidth = 42;
                bitmapImage.EndInit();
                return bitmapImage;
            }
            catch
            {
                throw;
            }
        }
        //Time and Key Event
        private void Keydown(object sender, KeyEventArgs e)
        {
            if (!player.Playable) return;
            if (e.Key != Key.Space) return;
            ControlPlay();
        }
        private void SliderTimerChange(object? sender, EventArgs e)
        {
            TimeBar.Value = player.NowTime.TotalSeconds;
            NowTime.Content = player.NowTime.ToString();
        }
        private void CheckAudioPlayEndOrNext(object? sender, EventArgs e)
        {
            if (player.PlaybackState == PlaybackState.Stopped)
            {
                CheckAudioPlayEndOrNext();
            }
        }
        //Player Ctrl
        private void GoToPlay(string path, bool willplay)
        {
            timer.Stop();

            string previoudPath = player.Path;
            long timetick = player.ReadInAudio(path);
            if (timetick == -1)
            {
                CheckAudioPlayEndOrNext();
                StatusBarUpdate("The audio can't play.", 2);
                return;
            }
            int previousIndex;
            if (previoudPath == String.Empty)
            {
                previousIndex = -1;
            }
            else
            {
                previousIndex = FileList.FindFullPathIndex(previoudPath);
            }


            TimeBar.Value = 0;
            TimeBarEnd = (int)(timetick / player.BytePreSec);
            TimeBar.Maximum = TimeBarEnd;

            int index = FileList.FindFullPathIndex(path);
            ListViewColorChanged(previousIndex, index);

            NowTime.Content = TimeSpan.Zero.ToString();
            AllTime.Content = TimeSpan.FromSeconds(TimeBarEnd).ToString();
            NowPlayTxt.Text = player.Path.Split(@"\")[^1];

            ControlButtonStatusChange(player.Playable);
            CheckFileListButtonStatus(player.Path);
            if (willplay)
            {
                ControlPlay();
            }
            else controlImg.Source = pauseImg;
        }
        private void CheckAudioPlayEndOrNext()
        {
            int index = FileList.FindFullPathIndex(player.Path);
            if (index == FileList.Count - 1)
            {
                player.Stop();
                ListViewColorChanged(index, -1);
                ControlButtonStatusChange(player.Playable);
                TimeBar.Value = 0;
                NowPlayTxt.Text = "Every audio is played.";
                StatusBarUpdate("Every audio is played.", -1);
                NowTime.Content = TimeSpan.Zero.ToString();
                AllTime.Content = TimeSpan.Zero.ToString();
                timer.Stop();
            }
            else
            {
                player.Stop();
                string _path = FileList[index + 1].FullPath;
                GoToPlay(_path, true);
                StatusBarUpdate("Next audio", -1);
            }
        }
        //Button
        private void ControlButtonStatusChange(bool status)
        {
            Forward.IsEnabled = status;
            Backward.IsEnabled = status;
            Control.IsEnabled = status;
            TimeBar.IsEnabled = status;
        }
        private void Backward_Click(object sender, RoutedEventArgs e)
        {
            player.Stop();
            ControlButtonStatusChange(player.Playable);
            GoToPlay(FileList[FileList.FindFullPathIndex(player.Path) - 1].FullPath, true);
        }
        private void Control_Click(object sender, RoutedEventArgs e)
        {
            ControlPlay();
        }
        private void ControlPlay()
        {
            player.PlayAndPause();
            if (player.Isplayed)
            {
                FileList[FileList.FindFullPathIndex(player.Path)].ShortPath = $"[Playing] {FileList[FileList.FindFullPathIndex(player.Path)].ShortPath}";
                controlImg.Source = playImg;
                timer.Start();
                StatusBarUpdate("Play.", -1);
            }
            else
            {
                FileList[FileList.FindFullPathIndex(player.Path)].ShortPath = $"{FileList[FileList.FindFullPathIndex(player.Path)].ShortPath.Substring(10)}";
                controlImg.Source = pauseImg;
                timer.Stop();
                StatusBarUpdate("Pause.", -1);
            }
        }
        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            CheckAudioPlayEndOrNext();
        }
        private void AddAudio_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.Filter = "(AudioFile)*.mp3;*.wav|*.mp3;*.wav";
            ofd.Multiselect = false;
            bool? result = ofd.ShowDialog();
            if (result == true)
            {
                foreach (var item in FileList)
                {
                    if (ofd.FileName == item.FullPath)
                    {
                        StatusBarUpdate("This audio file is already in the list.", 2);
                        return;
                    }
                }
                Path path = new(ofd.FileName.Split(@"\")[^1], ofd.FileName);
                FileList.Add(path);
                StatusBarUpdate($"Add file {ofd.FileName}", 2);
                ControlButtonStatusChange(player.Playable);
                CheckFileListButtonStatus(player.Path);
            }
        }
        //ListView
        private void ListViewItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    ListViewItem sourcelistViewItem = (ListViewItem)sender;
                    DragDrop.DoDragDrop(Listview, sourcelistViewItem, DragDropEffects.Move);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        private void ListViewItem_Drop(object sender, DragEventArgs e)
        {
            ListViewItem targetlistviewitem = (ListViewItem)sender;
            ListViewItem sourcelistViewItem = (ListViewItem)e.Data.GetData("System.Windows.Controls.ListViewItem");

            string targetitemfull = ((Path)targetlistviewitem.Content).FullPath;
            string sourceitemfull = ((Path)sourcelistViewItem.Content).FullPath;

            int targetindex = FileList.FindFullPathIndex(targetitemfull);
            int sourceindex = FileList.FindFullPathIndex(sourceitemfull);

            FileList.Move(targetindex, sourceindex);

            ControlButtonStatusChange(player.Playable);
            CheckFileListButtonStatus(player.Path);
        }
        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            player.Stop();
            ListViewItem item = (ListViewItem)sender;
            string itemName = ((Path)item.Content).FullPath;
            GoToPlay(itemName, true);
        }
        private void ListViewColorChanged(int previousIndex, int index)
        {
            if (index == -1)
            {
                ListViewItem listViewItemPre = (ListViewItem)Listview.ItemContainerGenerator.ContainerFromIndex(previousIndex);
                Border borderPre = (Border)listViewItemPre.Template.FindName("border", listViewItemPre);
                borderPre.SetBinding(BackgroundProperty, new Binding { RelativeSource = RelativeSource.TemplatedParent, Path = new PropertyPath("Background") });
                return;
            }
            if (previousIndex != -1)
            {
                ListViewItem listViewItemPre = (ListViewItem)Listview.ItemContainerGenerator.ContainerFromIndex(previousIndex);
                Border borderPre = (Border)listViewItemPre.Template.FindName("border", listViewItemPre);
                borderPre.SetBinding(BackgroundProperty, new Binding { RelativeSource = RelativeSource.TemplatedParent, Path = new PropertyPath("Background") });
            }
            ListViewItem listViewItem = (ListViewItem)Listview.ItemContainerGenerator.ContainerFromIndex(index);
            Border border = (Border)listViewItem.Template.FindName("border", listViewItem);
            border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFAAAAAA"));
        }
        private ListViewItem listViewItem;
        private void ListViewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            deleteFile.Visibility = Visibility.Visible;
            listViewItem = (ListViewItem)sender;
            Border bd = (Border)listViewItem.Template.FindName("border", listViewItem);
            double delPosY = listViewItem.TranslatePoint(new Point(0, 0), Listview).Y + 32;
            deleteFile.Margin = new Thickness(0, delPosY, 5, 0);
            if (((Path)listViewItem.Content).FullPath != player.Path)
            {
                bd.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA6A6A6"));
            }
        }
        private void ListViewItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!deleteFile.IsMouseOver)
            {
                listViewItem = (ListViewItem)sender;
                Border bd = (Border)listViewItem.Template.FindName("border", listViewItem);
                deleteFile.Visibility= Visibility.Collapsed;
                if (((Path)listViewItem.Content).FullPath != player.Path)
                {
                    bd.SetBinding(BackgroundProperty, new Binding { RelativeSource = RelativeSource.TemplatedParent, Path = new PropertyPath("Background") });
                }
            }
        }
        private void deleteFile_Click(object sender, RoutedEventArgs e)
        {
            deleteFile.Visibility = Visibility.Collapsed;
            int index = Listview.ItemContainerGenerator.IndexFromContainer(listViewItem);
            string oriPath = ((Path)listViewItem.Content).FullPath;
            FileList.RemoveAt(index);
            if (oriPath == player.Path)
            {
                timer.Stop();
                player.Stop();
                player.CleanPath();
                if (FileList.Count > 0 && index != FileList.Count)
                {
                    GoToPlay(FileList[index].FullPath, true);
                }
                else
                {
                    TimeBar.Value = 0;
                    ControlButtonStatusChange(player.Playable);
                    NowPlayTxt.Text = "Every audio is played.";
                    StatusBarUpdate("Every audio is played.", -1);
                    NowTime.Content = TimeSpan.Zero.ToString();
                    AllTime.Content = TimeSpan.Zero.ToString();
                }
            }
            else
            {
                CheckFileListButtonStatus(player.Path);
            }
        }
        //Slider
        private void TimeBar_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            if (timer.IsEnabled)
            {
                timer.Stop();
                Oritimerstatus = true;
            }
            else Oritimerstatus = false;
        }
        private void TimeBar_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            player.Set((long)TimeBar.Value * player.BytePreSec);
            if (Oritimerstatus)
            {
                timer.Start();
            }
        }
        private void TimeBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Slider bar = (Slider)sender;
            double barWidth = bar.ActualWidth;
            double mousePosX = e.GetPosition(bar).X;
            double timePos = (mousePosX / barWidth);
            double barPos = (bar.Value / bar.Maximum);
            double Tolerance = 7 / barWidth;
            if (mousePosX >= 0 && mousePosX <= barWidth && Math.Abs(timePos - barPos) > Tolerance)
            {

                if (bar.Name == "TimeBar")
                {
                    long timePosLong = (long)(timePos * player.TotalBytes);
                    player.Set(timePosLong);
                    TimeBar.Value = player.NowTime.TotalSeconds;
                    NowTime.Content = player.NowTime.ToString();
                }
                else if (bar.Name == "VolumeBar")
                {
                    bar.Value = timePos;
                }
            }
        }
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            player.Volume = VolumeBar.Value;
            VolumeValue.Text = ((int)(player.Volume * 100)).ToString();
        }
        //Others
        private void StatusBarUpdate(string message, int Lefttime)
        {
            if (string.IsNullOrEmpty(message)) return;
            switch (Lefttime)
            {
                case -1:
                    if (statusbartimer.IsEnabled)
                    {
                        statusbartimer.Stop();
                    }
                    _update(message);
                    break;
                case > 0:
                    statusbartimer.Stop();
                    statusbartimer.Interval = TimeSpan.FromSeconds(Lefttime);
                    statusbartimer.Tick += StatusClear;
                    _update(message);
                    statusbartimer.Start();
                    break;
                default:
                    break;
            }
            void _update(string message)
            {
                StatusBar.Items.Clear();
                StatusBar.Items.Add(message);
            }
        }
        private void StatusClear(object? sender, EventArgs e)
        {
            StatusBar.Items.Clear();
            statusbartimer.Stop();
        }
        private void VersionInfo_Click(object sender, RoutedEventArgs e)
        {
            Window window = new VersionWindows();
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = this.Left + this.Width / 2 - window.Width / 2;
            window.Top = this.Top + this.Height / 2 - window.Height / 2;
            window.ShowDialog();
        }
        private void CheckFileListButtonStatus(string? path)
        {
            if (FileList.Count < 1)
            {
                Backward.IsEnabled = false;
                Forward.IsEnabled = false;
                return;
            }
            if (path != null)
            {
                int _index = FileList.FindFullPathIndex(path);
                if (_index == 0)
                {
                    Backward.IsEnabled = false;
                    return;
                }
            }
        }
    }
}