using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Penguin690_sMusicPlayer.Models
{
    internal class MusicFileList : ObservableCollection<MusicFile>, IStatusSender
    {
        #region implementation of IStatusSender
        public Status Status { get; }

        public event EventHandler<StatusEventArgs> StatusSendEvent;

        public void StatusRegister()
        {
            Status.Register(this);
        }

        public void StatusUpdate(string message)
        {
            StatusSendEvent?.Invoke(this, new StatusEventArgs(message));
        }
        #endregion

        private readonly nint hwnd;

        public MusicFileList(Status status, nint _hwnd) : base()
        {
            Status = status;
            hwnd = _hwnd;

            StatusRegister();
        }

        public async void AddMusic()
        {
            FileOpenPicker picker = new() 
            { 
                ViewMode = PickerViewMode.List,
                CommitButtonText = "選取音樂", 
                SuggestedStartLocation = PickerLocationId.MusicLibrary
            };
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".aiff");
            picker.FileTypeFilter.Add(".ogg");
            picker.FileTypeFilter.Add(".mp3");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile file = await picker.PickSingleFileAsync();
            if (file is not null)
            {
                MusicFile musicFile = new(file.Path);
                Add(musicFile);
                StatusUpdate($"Add File: {musicFile.ShortPath}");
            }
        }

        public ControlStatus CanGetPreviousAndNext(MusicFile file)
        {
            int index = IndexOf(file);
            bool previous = index > 0 && index < Count;
            bool next = index >= 0 && index < Count - 1;
            return new(previous, false, next);
        }     
    }
}