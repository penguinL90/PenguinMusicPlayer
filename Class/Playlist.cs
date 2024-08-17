using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicApp.Class
{
    internal class fileList : ObservableCollection<Path>
    {
        public fileList() : base() { }
        public int FindPathIndex(Path path)
        {
            int _count = 0;
            foreach (var item in this)
            {
                if (item.ShortPath == path.ShortPath)
                {
                    return _count;
                }
                _count++;
            }
            return -1;
        }
    }

    internal class Playlist : ObservableCollection<FilePath>
    {
        public Playlist() : base() { }
        public void Enqueue(FilePath filePath)
        {
            Add(filePath);
        }
        public FilePath Dequeue()
        {
            if (this.Count == 0) return FilePath.Empty;
            FilePath filePath = this[0];
            RemoveAt(0);
            return filePath;
        }
    }
}
