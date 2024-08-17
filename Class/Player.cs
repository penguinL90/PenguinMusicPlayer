using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace MusicApp.Class
{
    [SupportedOSPlatform("windows7.0")]
    internal class Player
    {
        private WaveOutEvent _player;
        private AudioFileReader _filereader;
        private bool _playable;
        private bool _isplayed;
        private long _totalbytes;
        private long _bytepersec;
        private Path _path;
        private double _volume;
        public bool Isplayed { get => _isplayed; }
        public long TotalBytes { get => _totalbytes; }
        public long BytePreSec { get => _bytepersec; }
        public bool Playable { get => _playable; }
        public double Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                _volume = value;
                _player.Volume = (float)_volume;
            }
        }
        public TimeSpan NowTime { get => _filereader?.CurrentTime ?? TimeSpan.Zero; }
        public long NowTimeTick { get => _filereader?.Position ?? 0; }
        public Path Path { get => _path; }
        public PlaybackState PlaybackState
        {
            get { return _player.PlaybackState; }
        }
        public Player()
        {
            _path = new("", "");
            _player = new WaveOutEvent();
            _isplayed = false;
            _playable = false;
            _totalbytes = 0;
            _bytepersec = 0;
            _volume = 0.5;
        }
        public long GetNowTick()
        {
            return _filereader.Position;
        }
        public long ReadInAudio(Path path)
        {
            try
            {
                _path = path;
                if (_filereader == null)
                {
                    _filereader = new AudioFileReader(_path.FullPath);
                }
                else
                {
                    _filereader.Dispose();
                    _filereader = new AudioFileReader(_path.FullPath);
                }
                _player.Init(_filereader);
                _playable = true;
                _totalbytes = _filereader.Length;
                _bytepersec = _filereader.WaveFormat.AverageBytesPerSecond;
                return _filereader.Length;
            }
            catch
            {
                return -1;
            }
        }
        public void PlayAndPause()
        {
            if (_playable)
            {
                if (_isplayed)
                {
                    _path.ShowPath = $"[Pause] {_path.ShortPath}";
                    _player.Pause();
                    _isplayed = false;
                }
                else
                {
                    _path.ShowPath = $"[Playing] {_path.ShortPath}";
                    _player.Play();
                    _isplayed = true;
                }
            }
        }
        public void Stop()
        {
            if (_playable)
            {
                _player.Stop();
                _path.ShowPath = _path.ShortPath;
                _isplayed = false;
                _playable = false;
            }
        }
        public void Jump(int sec)
        {
            if (_playable)
            {
                bool oristatus = _isplayed;
                if (oristatus)
                {
                    _player.Pause();
                    _isplayed = false;
                }
                long NowTime = _filereader.Position + _bytepersec * sec;
                if (NowTime < _totalbytes && NowTime > 0)
                {
                    _filereader.Position += _bytepersec * sec;
                }
                else if (NowTime <= 0)
                {
                    _filereader.Position = 0;
                }
                else if (NowTime >= _totalbytes)
                {
                    _filereader.Position = _totalbytes;
                }
                if (oristatus)
                {
                    _player.Play();
                    _isplayed = true;
                }
            }
        }
        public void Set(long tick)
        {
            if (tick < _totalbytes)
            {
                _filereader.Position = tick;
            }
        }
        public void CleanPath()
        {
            _path = new("", "");
        }
    }

    [SupportedOSPlatform("windows7.0")]
    internal class Player2
    {
        public WaveOutEvent waveOutEvent;
        private AudioFileReader? audioFileReader;

        public Player2()
        {
            waveOutEvent = new();
        }

        public float Volume
        {
            get => waveOutEvent.Volume;
            set
            {
                waveOutEvent.Volume = value;
            }
        }

        public void SetAudioPath(FilePath path)
        {
            audioFileReader?.Dispose();
            audioFileReader = new(path.FullPath);
            waveOutEvent.Init(audioFileReader);
        }

        public void CleanPath()
        {
            audioFileReader?.Dispose();
            audioFileReader = null;
        }

        public float GetPlayedTimeRate()
        {
            if (audioFileReader != null)
                return audioFileReader.Position / audioFileReader.Length;
            return -1;
        }

        public long GetLength()
        {
            if (audioFileReader != null)
                return audioFileReader.Length;
            return -1;
        }

        public void PlayAndPause()
        {
            if (audioFileReader == null) return;
            if (waveOutEvent.PlaybackState == PlaybackState.Playing)
                waveOutEvent.Pause();
            else if (waveOutEvent.PlaybackState == PlaybackState.Paused)
                waveOutEvent.Play();
        }
        public void Stop()
        {
            if (audioFileReader == null) return;
            waveOutEvent.Stop();
        }

        public void Jump(long position)
        {
            if (audioFileReader == null) return;
            if (position >= audioFileReader.Length)
                waveOutEvent.Stop();
            if (position >= 0)
            {
                audioFileReader.Position = position;
                return;
            }
        }
    }
}
