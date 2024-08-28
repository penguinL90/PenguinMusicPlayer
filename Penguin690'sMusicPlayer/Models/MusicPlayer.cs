using Microsoft.UI.Xaml;
using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Threading;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System.Threading.Tasks;
using System.Linq;
using MathNet.Numerics;
using System.Collections.Generic;

namespace Penguin690_sMusicPlayer.Models;

internal class AudioFileReader2(MusicFile file) : AudioFileReader(file.FullPath)
{
    public MusicFile musicFile = file;
}

#nullable enable
#pragma warning disable CS8618
internal class MusicPlayer : IStatusSender
{
    public MusicFileList PlayList;

    public ControlStatus ControlStatus;

    public event EventHandler ControlStatusChangedEvent;

    public event EventHandler<TimebarChangedEventArgs> TimebarChanged;

    public event EventHandler<MusicSetEventArgs> MusicSet;

    public event EventHandler<PlayPauseChangedEventArgs> PlayPauseChanged;

    public event EventHandler<FFTUpdateEventArgs> FFTUpdate;

    private WaveOutEvent Player;

    private AudioFileReader2? AudioFileReader;

    private readonly object _lock = new object();

    private readonly int timerIntervalMillisec = 16;

    private DispatcherTimer timer;

    private FFTWaver fftwaver;

    public MusicPlayer(Status status, nint hwnd)
    {
        Status = status;

        Player = new();
        PlayList = new(status, hwnd);
        fftwaver = new();
        timer = new()
        {
            Interval = TimeSpan.FromMilliseconds(timerIntervalMillisec)
        };
        timer.Tick += PlaybackStopped;
        ControlStatus = ControlStatus.ALLFalse;

        StatusRegister();
    }

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

    public void OnControlStatusChanged()
    {
        ControlStatusChangedEvent?.Invoke(this, new());
    }

    public void OnMusicSet()
    {
        MusicSet?.Invoke(this, new(AudioFileReader!.TotalTime, AudioFileReader!.Length, AudioFileReader!.musicFile));
    }

    public void OnPlayPauseChanged(PlayStatus status)
    {
        PlayPauseChanged?.Invoke(this, new(status));
    }

    #region essential controls

    public void SetVolume(double volume)
    {
        Player.Volume = (float)volume;
    }

    /// <summary>
    /// 設定播放器的音樂
    /// </summary>
    /// <param name="file">音樂檔案</param>
    /// <returns> <c>true</c> 為成功 <c>false</c> 為失敗</returns>
    public bool SetAudio(MusicFile file)
    {
        if (AudioFileReader != null)
        {
            AudioFileReader.musicFile.PlayStatus = PlayStatus.NotPlaying;
            AudioFileReader.Dispose();
        }
        lock (_lock)
        {
            try
            {
                AudioFileReader = new(file);
                fftwaver.SetMusic(file);
                Stop();
                Player.Init(AudioFileReader);
                Play();
                ControlStatus = new(true, true, true);
                StatusUpdate($"Set audio: {file.ShortPath}");
                OnMusicSet();
                return true;
            }
            catch
            {
                AudioFileReader = null;
                ControlStatus = new(false, false, false);
                StatusUpdate("This file can't be played");
                return false;
            }
            finally
            {
                OnControlStatusChanged();
            } 
        }
    }

    /// <summary>
    /// 偵測音樂停止
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PlaybackStopped(object? sender, object e)
    {
        if (Monitor.TryEnter(_lock))
        {
            try
            {
                if (Player.PlaybackState == PlaybackState.Stopped)
                {
                    Next();
                }
            }
            finally
            {
                Monitor.Exit(_lock);
            } 
        }
    }

    /// <summary>
    /// 播放開始 / 暫停 介面
    /// </summary>
    public bool? PlayPause()
    {
        if (AudioFileReader == null)
        { 
            StatusUpdate("Nothing can be played");
            return null;
        }
        else if (Player.PlaybackState != PlaybackState.Playing)
        {
            Play();
            
            return true;
        }
        else
        {
            Pause();
            
            return false;
        }
    }

    /// <summary>
    /// 開始播放
    /// </summary>
    private void Play()
    {
        if (!timer.IsEnabled) timer.Start();
        Player.Play();
        timer.Tick += OnTimebarChanged;
        timer.Tick += FFT;
        OnPlayPauseChanged(PlayStatus.Pause);
        AudioFileReader!.musicFile.PlayStatus = PlayStatus.Play;
    }

    /// <summary>
    /// 暫停播放
    /// </summary>
    private void Pause()
    {
        Player.Pause();
        timer.Tick -= FFT;
        OnPlayPauseChanged(PlayStatus.Play);
        AudioFileReader!.musicFile.PlayStatus = PlayStatus.Pause;
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    private void Stop()
    {
        if (AudioFileReader == null)
        {
            StatusUpdate("Nothing can be stopped");
            return;
        }
        Player.Stop();
        OnPlayPauseChanged(PlayStatus.NotPlaying);
        timer.Tick -= OnTimebarChanged;
        timer.Tick -= FFT;
        AudioFileReader.musicFile.PlayStatus = PlayStatus.NotPlaying;
    }

    /// <summary>
    /// 跳至下一首歌 如無法播放則跳至能夠播放的歌曲或是直接到播放清單的底
    /// </summary>
    public void Next()
    {
        if (AudioFileReader == null) return;
        int index = PlayList.IndexOf(AudioFileReader.musicFile);
        do
        {
            if (++index >= PlayList.Count)
            {
                if (AudioFileReader != null) AudioFileReader.Position = 0;

                StatusUpdate("Play ended");
                OnPlayPauseChanged(PlayStatus.NotPlaying);

                timer.Stop();
                Stop();

                ControlStatus.Next = false;
                OnControlStatusChanged();

                return;
            }
        }
        while (!SetAudio(PlayList[index]));
    }

    /// <summary>
    /// 跳至上一首歌 如無法播放則跳至能夠播放的歌曲或是直接到播放清單的頭
    /// </summary>
    public void Previous()
    {
        if (AudioFileReader == null) return;
        int index = PlayList.IndexOf(AudioFileReader.musicFile);
        do
        {
            if (index == 0 && AudioFileReader != null)
            {
                AudioFileReader.Position = 0;
                return;
            }
            else if (index == 0)
            {
                StatusUpdate("Nothing can't be played");
                timer.Stop();
                Stop();
            }
            --index;
        }
        while (!SetAudio(PlayList[index]));
    }

    #endregion

    #region timebar control

    private void OnTimebarChanged(object? sender, object e)
    {
        TimeSpan currentTimeSpan = TimeSpan.FromSeconds(AudioFileReader!.Position / (double)AudioFileReader.WaveFormat.AverageBytesPerSecond);
        TimebarChanged?.Invoke(this, new(currentTimeSpan, AudioFileReader!.Position));
    }

    public void SetTime(long time)
    {
        if (AudioFileReader == null) return;
        if (time >= 0 && time <= AudioFileReader.Length)
        {
            AudioFileReader.Position = time;
        }
    }

    #endregion

    #region FFT

    private async void FFT(object? sender, object e)
    {
        await Task.Run(() =>
        {
            FFTUpdate?.Invoke(null, new(fftwaver.CountFFT(AudioFileReader!.Position)));
        });
    }



    #endregion
}

internal record struct ControlStatus(bool Previous, bool PlayPause, bool Next)
{
    public static ControlStatus ALLFalse => new(false, false, false);
}

internal class TimebarChangedEventArgs(TimeSpan currentTime, long nowTime) : EventArgs
{
    public TimeSpan CurrentTime = currentTime;
    public long NowTime = nowTime;
}

internal class MusicSetEventArgs(TimeSpan totalTimeSpanTime, long totalTime, MusicFile file) : EventArgs
{
    public TimeSpan TotalTimeSpanTime = totalTimeSpanTime;
    public long TotalTime = totalTime;
    public MusicFile File = file;
}

internal class PlayPauseChangedEventArgs(PlayStatus status) : EventArgs
{
    public PlayStatus Status = status;
}

internal class FFTUpdateEventArgs(double[] frequencies) : EventArgs
{
    public double[] Frequencies = frequencies;
}