using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Penguin690_sMusicPlayer.Models
{
    internal class MusicFile : INotifyPropertyChanged, IEquatable<MusicFile>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string FullPath { get; set; }

        public string ShortPath { get; set; }

        public string ShowName => ToString();

        private PlayStatus playStatus;

        public PlayStatus PlayStatus
        {
            get => playStatus;
            set 
            {
                playStatus = value;
                OnPropertyChanged(nameof(ShowName));
            }
        }

        public MusicFile(string path)
        {
            FullPath = path;
            ShortPath = Path.GetFileName(path);
            PlayStatus = PlayStatus.NotPlaying;
        }

        public override string ToString()
        {
            string playStatus = PlayStatus switch
            {
                PlayStatus.Play => "[Playing]",
                PlayStatus.Pause => "[Pause]",
                _ => ""
            };
            return playStatus + ShortPath;
        }

        public bool Equals(MusicFile other)
        {
            return FullPath.Equals(other.FullPath);
        }
    }

    internal enum PlayStatus
    {
        Play,
        Pause,
        NotPlaying,
    }
}