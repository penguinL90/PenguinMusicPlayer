using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace MusicApp
{
    public partial class MainWindow : Window
    {
        private Player player;
        public ObservableCollection<string> FileList;
        private DispatcherTimer timer;
        private int TimeBarStart = 0;
        private int TimeBarEnd;
        public MainWindow()
        {
            InitializeComponent();
            player = new Player();
            FileList = new();
            Listview.ItemsSource = FileList;
            ControlButtonStatusChange(player.Playable);
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += new EventHandler(SliderTimerChange);
            timer.Tick += new EventHandler(CheckAudioPlayEndOrNext);
            VolumeBar.Value = player.Volume;
            NowPlayTxt.Text = "Nothing play yet";
            VolumeValue.Text = ((int)(player.Volume * 100)).ToString();
            AllTime.Content = TimeSpan.Zero.ToString();
            NowTime.Content = TimeSpan.Zero.ToString();
            this.KeyDown += new KeyEventHandler(Keydown);
            StatusBarUpdate("Ready.");
        }
        private void Keydown(object sender, KeyEventArgs e)
        {
            if (!player.Playable) return;
            if (e.Key != Key.Space) return;
            ControlPlay();
        }
        private void GoToPlay(string path, bool willplay)
        {
            long timetick = player.ReadInAudio(path);
            if (timetick == -1)
            {
                CheckAudioPlayEndOrNext();
                StatusBarUpdate("The audio can't play.");
                return;
            }
            TimeBarEnd = (int)(timetick / player.BytePreSec);
            TimeBar.Maximum = TimeBarEnd;
            AllTime.Content = TimeSpan.FromSeconds(TimeBarEnd).ToString();
            ControlButtonStatusChange(player.Playable);
            NowPlayTxt.Text = player.Path.Split(@"\")[^1];
            if (willplay)
            {
                player.PlayAndPause();
                Control.Content = "Pause";
            }
            else Control.Content = "Play";
        }

        private void SliderTimerChange(object sender, EventArgs e)
        {
            TimeBar.Value = player.NowTime.TotalSeconds;
            NowTime.Content = player.NowTime.ToString();
        }
        private void CheckAudioPlayEndOrNext(object sender, EventArgs e)
        {
            if (player.NowTimeTick >= player.TotalBytes - (player.BytePreSec / 10))
            {
                CheckAudioPlayEndOrNext();
            }
        }
        private void CheckAudioPlayEndOrNext()
        {
            int index = FileList.IndexOf(player.Path);
            if (index == FileList.Count - 1)
            {
                player.Stop();
                FileList.Remove(player.Path);
                ControlButtonStatusChange(player.Playable);
                TimeBar.Value = 0;
                NowPlayTxt.Text = "Everything has played.";
                StatusBarUpdate("Everything has played.");
                NowTime.Content = TimeSpan.Zero.ToString();
                timer.Stop();
            }
            else
            {
                player.Stop();
                string _path = FileList[index + 1];
                FileList.Remove(player.Path);
                index++;
                GoToPlay(_path, true);
                StatusBarUpdate("Next audio");
            }
        }
        private void ControlButtonStatusChange(bool status)
        {
            Forward.IsEnabled = status;
            Backward.IsEnabled = status;
            Control.IsEnabled = status;
            TimeBar.IsEnabled = status;
        }
        private void Backward_Click(object sender, RoutedEventArgs e)
        {
            player.Jump(-10);
            TimeBar.Value = player.NowTime.TotalSeconds;
            NowTime.Content = player.NowTime.ToString();
            StatusBarUpdate("Backward 10 seconds.");
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
                Control.Content = "Pause";
                timer.Start();
                StatusBarUpdate("Play.");
            }
            else
            {
                Control.Content = "Play";
                timer.Stop();
                StatusBarUpdate("Pause.");
            }
        }
        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            player.Jump(10);
            TimeBar.Value = player.NowTime.TotalSeconds;
            NowTime.Content = player.NowTime.ToString();
            StatusBarUpdate("Forward 10 seconds.");
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
                FileList.Add(ofd.FileName);
            }
        }
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
        private void ListViewItem_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                ListViewItem targetlistviewitem = (ListViewItem)sender;
                ListViewItem sourcelistViewItem = (ListViewItem)e.Data.GetData("System.Windows.Controls.ListViewItem");
                if (sourcelistViewItem == targetlistviewitem) return;

                string targetitem = targetlistviewitem.Content.ToString();
                string sourceitem = sourcelistViewItem.Content.ToString();

                int targetindex = FileList.IndexOf(targetitem);
                int sourceindex = FileList.IndexOf(sourceitem);

                Rectangle toprec = (Rectangle)targetlistviewitem.Template.FindName("toprec", targetlistviewitem);
                Rectangle botrec = (Rectangle)targetlistviewitem.Template.FindName("toprec", targetlistviewitem);

                if (targetindex < sourceindex)
                {
                    botrec.Visibility = Visibility.Visible;
                }
                else if (sourceindex < targetindex)
                {
                    toprec.Visibility = Visibility.Visible;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        private void ListViewItem_DragLeave(object sender, DragEventArgs e)
        {
            try
            {
                ListViewItem targetitem = (ListViewItem)sender;
                Rectangle toprec = (Rectangle)targetitem.Template.FindName("toprec", targetitem);
                Rectangle botrec = (Rectangle)targetitem.Template.FindName("botrec", targetitem);
                toprec.Visibility = Visibility.Collapsed;
                botrec.Visibility = Visibility.Collapsed;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private void ListViewItem_Drop(object sender, DragEventArgs e)
        {
            ListViewItem targetlistviewitem = (ListViewItem)sender;
            ListViewItem sourcelistViewItem = (ListViewItem)e.Data.GetData("System.Windows.Controls.ListViewItem");

            string targetitem = targetlistviewitem.Content.ToString();
            string sourceitem = sourcelistViewItem.Content.ToString();

            int targetindex = FileList.IndexOf(targetitem);
            int sourceindex = FileList.IndexOf(sourceitem);

            Rectangle toprec = (Rectangle)targetlistviewitem.Template.FindName("toprec", targetlistviewitem);
            Rectangle botrec = (Rectangle)targetlistviewitem.Template.FindName("toprec", targetlistviewitem);

            toprec.Visibility = Visibility.Collapsed;
            botrec.Visibility = Visibility.Collapsed;

            FileList.Move(targetindex, sourceindex);
        }
        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            player.Stop();
            ListViewItem item = (ListViewItem)sender;
            string itemName = item.Content.ToString();
            GoToPlay(itemName, false);
        }
        private void TimeBar_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            timer.Stop();
        }
        private void TimeBar_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            player.Set((long)TimeBar.Value * player.BytePreSec);
            timer.Start();
        }
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            player.Volume = VolumeBar.Value;
            VolumeValue.Text = ((int)(player.Volume * 100)).ToString();
        }
        private void StatusBarUpdate(string message)
        {
            if (!String.IsNullOrWhiteSpace(message))
            {
                StatusBar.Items.Clear();
                StatusBar.Items.Add(message);
            }
        }

        private void TimeBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }
}