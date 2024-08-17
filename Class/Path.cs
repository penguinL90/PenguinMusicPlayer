using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MusicApp.Class
{
    internal struct FilePath : IEquatable<FilePath>
    {

        public string FullPath;
        public string ShortPath
        {
            get
            {
                for (int i = FullPath.Length - 1; i >= 0; --i)
                {
                    if (FullPath[i] == '/' || FullPath[i] == '\\')
                        return FullPath[(i + 1)..];
                }
                return "";
            }
        }

        public bool Equals(FilePath other)
        {
            return FullPath.Equals(other.FullPath);
        }

        public static FilePath Empty => new FilePath();
    }

    internal class Path : INotifyPropertyChanged
    {
        private string showPath;
        private string shortPath;
        private string fullPath;
        public event PropertyChangedEventHandler? PropertyChanged;
        public string ShowPath
        {
            get => showPath;
            set
            {
                showPath = value;
                OnPropertyChanged();
            }
        }
        public string ShortPath
        {
            get
            {
                return shortPath;
            }
            set
            {
                shortPath = value;
            }
        }
        public string FullPath { get => fullPath; set => fullPath = value; }
        public Path(string shortPath, string fullPath)
        {
            this.shortPath = shortPath;
            this.fullPath = fullPath;
            showPath = shortPath;
        }
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public override string ToString()
        {
            return ShortPath;
        }
    }
}
