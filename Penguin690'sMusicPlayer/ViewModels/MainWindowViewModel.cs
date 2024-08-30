using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Penguin690_sMusicPlayer.Models;
using Windows.Foundation;
using Windows.Media.Playlists;
using System.Drawing;

namespace Penguin690_sMusicPlayer.ViewModels;

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

    private MusicFile SelectFile;


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

    public double[] _FFTArray;

    private CanvasControl _FFTCanvasControl;

    public DoubleTappedEventHandler ListView_DoubleTapped;
    public PointerEventHandler Slider_PointerEntered;
    public PointerEventHandler Slider_PointerExited;   
    public RangeBaseValueChangedEventHandler Slider_ValueChanged;
    public TypedEventHandler<CanvasControl, CanvasDrawEventArgs> canvasCtrl_Draw;
    public RoutedEventHandler canvasCtrl_Loaded;
    public RoutedEventHandler canvasCtrl_Unloaded;
    public SelectionChangedEventHandler SelectionChanged;

    public RelayCommand AddMusicCommand;
    public RelayCommand PlayMusicCommand;

    public RelayCommand PreviousCommand;
    public RelayCommand NextCommand;

    public RelayCommand DeleteCommand;
    public RelayCommand MoveUpperCommand;
    public RelayCommand MoveLowerCommand;

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
        Player.FFTUpdate += FFTUpdate;

        PlayMusicCommand = new(() => Player.PlayPause(), () => Player.ControlStatus.PlayPause);
        PreviousCommand = new(Player.Previous, () => Player.ControlStatus.Previous);
        NextCommand = new(Player.Next, () => Player.ControlStatus.Next);
        AddMusicCommand = new(Player.PlayList.AddMusic);

        DeleteCommand = new(() => Player.PlayList.Remove(SelectFile), () => SelectFile != null);
        MoveUpperCommand = new(() => Player.PlayList.MoveUpperOne(SelectFile), () => SelectFile != null);
        MoveLowerCommand = new(() => Player.PlayList.MoveLowerOne(SelectFile), () => SelectFile != null);

        canvasCtrl_Loaded = (s, e) =>
        {
            _FFTCanvasControl = s as CanvasControl;
            _FFTCanvasControl.Height = 500;
            _FFTCanvasControl.Width = Player.GetFFTCount() * 10;
            _FFTCanvasControl.Measure(new Windows.Foundation.Size(Player.GetFFTCount() * 10, _FFTCanvasControl.Height));
            _FFTCanvasControl.Arrange(new(0, 0, Player.GetFFTCount() * 10, _FFTCanvasControl.Height));
        };
        ListView_DoubleTapped = (s, e) =>
        {
            SetMusic((s as ListView).SelectedItem as MusicFile);
        };
        Slider_PointerEntered = (s, e) =>
        {
            (s as Slider).ValueChanged += Slider_ValueChanged;
            SliderPointerIn();
        };
        Slider_PointerExited = (s, e) =>
        {
            (s as Slider).ValueChanged -= Slider_ValueChanged;
            SliderPointerOut();
        };
        Slider_ValueChanged = (s, e) =>
        {
            long pos = (long)(s as Slider).Value;
            SliderPointPress(pos);
        };
        canvasCtrl_Draw = (s, e) =>
        {
            CanvasDrawingSession drawer = e.DrawingSession;
            drawer.Clear(Colors.Transparent);
            int fontHeight = 20;

            if (_FFTArray == null) return;
            for (int i = 0; i < _FFTArray.Length; ++i)
            {
                double x = i * 10;
                double height = _FFTArray[i] * 150;
                drawer.FillRectangle(new Rect(x, _FFTCanvasControl.Height - height - fontHeight, 8, height), Colors.AliceBlue);
            }
            int[] fftquartiles = Player.GetFFTQuartiles();
            for (int i = 0; i < fftquartiles.Length; ++i)
            {
                drawer.DrawText(fftquartiles[i].ToString(),
                                new System.Numerics.Vector2((float)(_FFTCanvasControl.Width / 4 * i), (float)(_FFTCanvasControl.Height - fontHeight)),
                                Colors.AliceBlue,
                                new()
                                {
                                    FontFamily = "Consolas",
                                    FontSize = 12,
                                    HorizontalAlignment = (i == 0 
                                                           ? CanvasHorizontalAlignment.Left 
                                                           : (i == fftquartiles.Length - 1) 
                                                           ? CanvasHorizontalAlignment.Right 
                                                           : CanvasHorizontalAlignment.Center)
                                });
            }
        };
        SelectionChanged = (s, e) =>
        {
            if (e.AddedItems.Count > 0)
            {
                SelectFile = e.AddedItems[0] as MusicFile;
            }
            else
            {
                SelectFile = null;
            }
            DeleteCommand.RaiseCanExecuteChanged();
            MoveLowerCommand.RaiseCanExecuteChanged();
            MoveUpperCommand.RaiseCanExecuteChanged();
        };
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

    public void SliderPointerIn() => Player.TimebarChanged -= Player_TimebarChanged;

    public void SliderPointerOut() => Player.TimebarChanged += Player_TimebarChanged;

    public void SliderPointPress(long pos)
    {
        Player.SetTime(pos);
    }

    public bool NullableBool2Bool(bool? value) => value ?? false;

    public void FFTUpdate(object sender, FFTUpdateEventArgs e)
    {
        _FFTArray = e.Frequencies;
        _FFTCanvasControl.Invalidate();
    }
}