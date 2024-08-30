using Penguin690_sMusicPlayer.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Penguin690_sMusicPlayer.ViewModels;

internal abstract class ViewModelBase : INotifyPropertyChanged
{
    public Status Status { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}