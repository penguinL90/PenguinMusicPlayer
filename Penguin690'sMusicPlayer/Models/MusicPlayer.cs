using Microsoft.UI.Xaml;
using NAudio.Wave;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Penguin690_sMusicPlayer.Models;

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

    private AudioFileReader2? AudioFileReader;

    private readonly WaveOutEvent Player;

    private readonly object _lock = new object();

    private readonly int PlayerTimerIntervalMillisec = 30;

    public readonly DispatcherTimer PlayerTimer;

    private readonly FFTWaver fftwaver;

    private bool TimebarChangedStatus;
    private void TimebarChangedController(bool tofc)
    {
        switch (tofc, TimebarChangedStatus)
        {
            case (false, true):
                PlayerTimer.Tick -= OnTimebarChanged;
                TimebarChangedStatus = false;
                break;
            case (true, false):
                PlayerTimer.Tick += OnTimebarChanged;
                TimebarChangedStatus = true;
                break;
        }
    }

    private bool PlaybackStoppedStatus;
    private void PlaybackStoppedController(bool tofc)
    {
        switch (tofc, PlaybackStoppedStatus)
        {
            case (false, true):
                PlayerTimer.Tick -= PlaybackStopped;
                PlaybackStoppedStatus = false;
                break;
            case (true, false):
                PlayerTimer.Tick += PlaybackStopped;
                PlaybackStoppedStatus = true;
                break;
        }
    }

    public MusicPlayer(Status status, nint hwnd)
    {
        Status = status;

        Player = new();
        PlayList = new(status, hwnd);
        fftwaver = new(status);
        PlayerTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(PlayerTimerIntervalMillisec)
        };
        PlayerTimer.Tick += FFT;
        PlayerTimer.Start();
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

    #region event invoke
    public void OnControlStatusChanged() =>
        ControlStatusChangedEvent?.Invoke(this, new());

    public void OnMusicSet() => 
        MusicSet?.Invoke(this, new(AudioFileReader!.TotalTime, AudioFileReader!.Length, AudioFileReader!.musicFile));

    public void OnMusicClear() =>
        MusicSet?.Invoke(this, new(TimeSpan.Zero, 0, null));

    public void OnPlayPauseChanged(PlayStatus status) => 
        PlayPauseChanged?.Invoke(this, new(status));
    #endregion

    #region essential controls

    public void SetVolume(double volume) => Player.Volume = (float)volume;

    /// <summary>
    /// 設定播放器的音樂
    /// </summary>
    /// <param name="file">音樂檔案</param>
    /// <returns> <c>true</c> 為成功 <c>false</c> 為失敗</returns>
    public async Task<bool> SetAudio(MusicFile file)
    {
        if (AudioFileReader != null)
        {
            AudioFileReader.musicFile.PlayStatus = PlayStatus.NotPlaying;
            AudioFileReader.Dispose();
        }
        if (Monitor.TryEnter(_lock))
        {
            try
            {
                Stop();
                await Task.Run(() =>
                {
                    AudioFileReader = new(file);
                    fftwaver.SetMusic(file);
                    Player.Init(AudioFileReader);
                });
                Play();
                StatusUpdate($"Set audio: {file.ShortPath}");
                OnMusicSet();
                return true;
            }
            catch (Exception ex) 
            {
                AudioFileReader = null;
                ControlStatus = new(false, false, false);
                OnControlStatusChanged();
                StatusUpdate($"This file can't be played, Error: {ex.Message}");
                return false;
            }
            finally 
            { 
                Monitor.Exit(_lock);
            }
        }
        StatusUpdate($"Thread blocked, cannot play");
        return false;
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
    public void PlayPause()
    {
        if (AudioFileReader == null)
            StatusUpdate("Nothing can be played");
        else if (Player.PlaybackState != PlaybackState.Playing)
            Play();
        else
            Pause();
    }

    /// <summary>
    /// 開始播放
    /// </summary>
    private void Play()
    {
        if (AudioFileReader == null) return;

        ControlStatus = new(true, true, true);
        OnControlStatusChanged();

        Player.Play();

        TimebarChangedController(true);
        PlaybackStoppedController(true);

        OnPlayPauseChanged(PlayStatus.Pause);
        AudioFileReader!.musicFile.PlayStatus = PlayStatus.Play;
    }

    /// <summary>
    /// 暫停播放
    /// </summary>
    private void Pause()
    {
        if (AudioFileReader == null) return;

        TimebarChangedController(false);

        Player.Pause();

        OnPlayPauseChanged(PlayStatus.Play);
        AudioFileReader!.musicFile.PlayStatus = PlayStatus.Pause;
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    private void Stop()
    {
        if (AudioFileReader == null) return;

        TimebarChangedController(false);
        PlaybackStoppedController(false);

        Player.Stop();

        OnPlayPauseChanged(PlayStatus.NotPlaying);
        AudioFileReader.musicFile.PlayStatus = PlayStatus.NotPlaying;
    }

    /// <summary>
    /// 跳至下一首歌 如無法播放則跳至能夠播放的歌曲或是直接到播放清單的底
    /// </summary>
    public async void Next()
    {
        if (AudioFileReader == null) return;
        int index = PlayList.IndexOf(AudioFileReader.musicFile);
        do
        {
            if (++index >= PlayList.Count)
            {
                if (AudioFileReader != null) AudioFileReader.Position = 0;
                    
                ControlStatus.Next = false;
                OnControlStatusChanged();

                StatusUpdate("Play ended!");
                OnPlayPauseChanged(PlayStatus.NotPlaying);

                Stop();

                return;
            }
        }
        while (! await SetAudio(PlayList[index]));
    }

    /// <summary>
    /// 跳至上一首歌 如無法播放則跳至能夠播放的歌曲或是直接到播放清單的頭
    /// </summary>
    public async void Previous()
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
                StatusUpdate("Play ended!");
                OnPlayPauseChanged(PlayStatus.NotPlaying);

                Stop();
            }
            --index;
        }
        while (! await SetAudio(PlayList[index]));
    }

    public void Remove(MusicFile music)
    {
        if (AudioFileReader != null && AudioFileReader.musicFile == music)
        {
            Stop();
            AudioFileReader.Dispose();
            fftwaver.Dispose();
            AudioFileReader = null;
            ControlStatus = new(false, false, false);
            OnControlStatusChanged();
            OnMusicClear();
        }
        PlayList.Remove(music);
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
            AudioFileReader.Position = time;
    }

    #endregion

    #region FFT

    private async void FFT(object? sender, object e)
    {
        await Task.Run(() =>
        {
            double[]? fftresults;
            if ( AudioFileReader != null && AudioFileReader.musicFile.PlayStatus == PlayStatus.Play)
            {
                fftresults = fftwaver.CountFFT(AudioFileReader!.Position);
            }
            else
            {
                fftresults = null;
            }
            FFTUpdate?.Invoke(null, new(fftresults));
        });
    }

    public int GetFFTCount() => fftwaver.selectFrequenciesCount;

    public int[] GetFFTQuartiles() => fftwaver.FrequencyQuartiles;

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

internal class MusicSetEventArgs(TimeSpan totalTimeSpanTime, long totalTime, MusicFile? file) : EventArgs
{
    public TimeSpan TotalTimeSpanTime = totalTimeSpanTime;
    public long TotalTime = totalTime;
    public MusicFile? File = file;
}

internal class PlayPauseChangedEventArgs(PlayStatus status) : EventArgs
{
    public PlayStatus Status = status;
}

internal class AudioFileReader2(MusicFile file) : AudioFileReader(file.FullPath)
{
    public MusicFile musicFile = file;
}

internal class FFTUpdateEventArgs(double[]? frequencies = null) : EventArgs
{
    public double[]? Frequencies = frequencies;
}