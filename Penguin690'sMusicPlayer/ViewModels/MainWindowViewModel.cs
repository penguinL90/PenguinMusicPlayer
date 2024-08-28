using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Penguin690_sMusicPlayer.Models;

namespace Penguin690_sMusicPlayer.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        private readonly nint hwnd;

        public MusicPlayer Player;

        private string statusText;
        public string StatusText
        {
            get { return statusText; }
            set
            {
                statusText = value;
                OnPropertyChanged();
            }
        }

        private double totalTime;
        public double TotalTime
        {
            get { return totalTime; }
            set 
            { 
                totalTime = value;
                OnPropertyChanged();
            }
        }

        private double nowTime;
        public double NowTime
        {
            get { return nowTime; }
            set 
            { 
                nowTime = value;
                OnPropertyChanged();
            }
        }

        private TimeSpan currentTimeTimeSpan;
        public TimeSpan CurrentTimeTimeSpan
        {
            get { return currentTimeTimeSpan; }
            set 
            {
                currentTimeTimeSpan = value; 
                OnPropertyChanged(nameof(CurrentTimeString));
            }
        }
        public string CurrentTimeString => CurrentTimeTimeSpan.ToString(@"mm\:ss");

        private TimeSpan totalTimeTimeSpan;
        public TimeSpan TotalTimeTimeSpan
        {
            get { return totalTimeTimeSpan; }
            set 
            { 
                totalTimeTimeSpan = value; 
                OnPropertyChanged(nameof(TotalTimeString));
            }
        }
        public string TotalTimeString => TotalTimeTimeSpan.ToString(@"mm\:ss");

        private Symbol playPauseSymbol = Symbol.Play;
        public Symbol PlayPauseSymbol
        {
            get { return playPauseSymbol; }
            set
            {
                playPauseSymbol = value;
                OnPropertyChanged();
            }
        }

        private string musicName;
        public string MusicName
        {
            get { return musicName; }
            set 
            { 
                musicName = value;
                OnPropertyChanged();
            }
        }

        private MusicFile selectFile;

        public MusicFile SelectFile
        {
            get { return selectFile; }
            set 
            { 
                selectFile = value; 
                OnPropertyChanged();
            }
        }

        private int volume;

        public int Volume
        {
            get { return volume; }
            set
            {
                volume = value;
                OnPropertyChanged();
                Player.SetVolume(value / 100f);
            }
        }


        public RelayCommand AddMusicCommand;
        public RelayCommand PlayMusicCommand;
        public RelayCommand PreviousCommand;
        public RelayCommand NextCommand;
        public MainWindowViewModel(nint _hwnd)
        {
            hwnd = _hwnd;
            Status = new(StatusUpdate);
            Player = new(Status, hwnd);
            Volume = 50;

            Player.TimebarChanged += Player_TimebarChanged;
            Player.ControlStatusChangedEvent += SetControlStatus;
            Player.MusicSet += Player_MusicSet;
            Player.PlayPauseChanged += Player_PlayPauseChanged;

            PlayMusicCommand = new(PlayPause, () => Player.ControlStatus.PlayPause);
            PreviousCommand = new(Player.Previous, () => Player.ControlStatus.Previous);
            NextCommand = new(Player.Next, () => Player.ControlStatus.Next);
            AddMusicCommand = new(Player.PlayList.AddMusic);
        }

        private void Player_PlayPauseChanged(object sender, PlayPauseChangedEventArgs e)
        {
            if (e.Status == PlayStatus.Play)
            {
                PlayPauseSymbol = Symbol.Play;
            }
            else
            {
                PlayPauseSymbol = Symbol.Pause;
            }
        }

        private void Player_MusicSet(object sender, MusicSetEventArgs e)
        {
            TotalTime = e.TotalTime;
            TotalTimeTimeSpan = e.TotalTimeSpanTime;
            MusicName = e.File.ShortPath;
            SelectFile = e.File;
        }

        private void Player_TimebarChanged(object sender, TimebarChangedEventArgs e)
        {
            CurrentTimeTimeSpan = e.CurrentTime;
            NowTime = e.NowTime;
        }

        public void StatusUpdate(string message)
        {
            StatusText = message;
        }

        public void SetMusic(MusicFile file)
        {
            Player.SetAudio(file);
        }

        public void SetControlStatus(object sender, EventArgs e)
        {
            NextCommand.RaiseCanExecuteChanged();
            PreviousCommand.RaiseCanExecuteChanged();
            PlayMusicCommand.RaiseCanExecuteChanged();
        }

        public void PlayPause()
        {
            Player.PlayPause();
        }

        public void SliderPointerIn()
        {
            Player.TimebarChanged -= Player_TimebarChanged;
        }

        public void SliderPointerOut()
        {
            Player.TimebarChanged += Player_TimebarChanged;
        }

        public void SliderPointPress(long pos)
        {
            Player.SetTime(pos);
        }

        public bool NullableBool2Bool(bool? value) => value ?? false;
    }
}