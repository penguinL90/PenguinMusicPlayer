using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MusicApp.Class
{
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
            PropertyChanged?.DynamicInvoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public override string ToString()
        {
            return ShortPath;
        }
    }
}
