using NAudio.Wave;
using System;
using System.Reflection;
using System.Runtime.Versioning;
namespace MusicApp
{

    [SupportedOSPlatform("windows7.0")]
    class Player
    {
        private WaveOutEvent _player;
        private AudioFileReader _filereader;
        private bool _playable;
        private bool _isplayed;
        private long _totalbytes;
        private long _bytepersec;
        private string _path;
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
        public TimeSpan NowTime
        {
            get
            {
                TimeSpan chace = _filereader?.CurrentTime ?? TimeSpan.Zero;
                return chace;
            }
        }
        public long NowTimeTick
        {
            get
            {
                long cache = _filereader?.Position ?? 0;
                return cache;
            }
        }
        public string Path { get => _path; }
        public Player()
        {
            _player = new WaveOutEvent();
            _isplayed = false;
            _playable = false;
            _totalbytes = 0;
            _bytepersec = 0;
            _volume = 0.5;
            _path = String.Empty;
        }
        public long GetNowTick()
        {
            return _filereader.Position;
        }
        public long ReadInAudio(string path)
        {
            try
            {
                _path = path;
                if (_filereader == null)
                {
                    _filereader = new AudioFileReader(_path);
                }
                else
                {
                    _filereader.Dispose();
                    _filereader = new AudioFileReader(_path);
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
                    _player.Pause();
                    _isplayed = false;
                }
                else
                {
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
        public PlaybackState PlaybackState
        {
            get { return _player.PlaybackState; }
        }
        public void CleanPath()
        {
            _path = String.Empty;
        }   
    }
}
