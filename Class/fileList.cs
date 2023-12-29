using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
}
