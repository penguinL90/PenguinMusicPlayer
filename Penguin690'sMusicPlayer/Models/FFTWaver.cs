using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics;
using NAudio.Wave;
using System.IO;
using System.Diagnostics;

namespace Penguin690_sMusicPlayer.Models
{
    internal class FFTWaver : IDisposable
    {
        private readonly int maxFrequency = 2500;
        private readonly int minFrequency = 65;
        public readonly int selectFrequenciesCount = 25;
        private int sampleCount = 2048;
        private int frequencySapn;
        private int bytePreSamplePreChannel;
        private int sampleRate;
        private int normalizeConst;
        private int channels;
        private double[] window;
        private Complex[] readInComplexs;
        private Func<double> readFunction;

        private MemoryStream stream;
        private BinaryReader reader;

        public FFTWaver()
        {
            frequencySapn = (maxFrequency - minFrequency) / (selectFrequenciesCount - 1);
            window = Window.Hamming(sampleCount);
        }

        public int SelectFrequenciesCount { get; set; }

        public void SetMusic(MusicFile file)
        {
            stream?.Dispose();
            reader?.Dispose();

            AudioFileReader _stream = new(file.FullPath);

            stream = new();
            _stream.CopyTo(stream);

            reader = new(stream);

            sampleRate = _stream.WaveFormat.SampleRate;
            channels = _stream.WaveFormat.Channels;
            bytePreSamplePreChannel = _stream.WaveFormat.BitsPerSample / 8 / channels;

            normalizeConst = (int)Math.Pow(2, _stream.WaveFormat.BitsPerSample / channels - 8);
            switch (bytePreSamplePreChannel)
            {
                case 1:
                    readFunction = () => reader.ReadByte();
                    break;
                case 2:
                    readFunction = () => reader.ReadInt16();
                    break;
                case 4:
                    readFunction = () => reader.ReadInt32();
                    break;
                default:
                    return;
            }
            _stream.Dispose();
        }

        public double[] CountFFT(long pos)
        {
            double[] outputFrequencies = new double[selectFrequenciesCount];
            ReadData(pos);
            Fourier.Forward(readInComplexs);
            for (int i = 0; i < selectFrequenciesCount; i++)
            {
                double selectFrequency = minFrequency + frequencySapn * i;
                int fftIndex = (int)(selectFrequency * sampleCount / sampleRate);
                fftIndex = Math.Min(fftIndex, sampleRate / 2);
                outputFrequencies[i] = readInComplexs[fftIndex].Magnitude * 2 / sampleCount;
            }
            return outputFrequencies;
        }

        private void ReadData(long pos)
        {
            readInComplexs = new Complex[sampleCount];
            stream.Position = pos;
            int min = (int)Math.Min((stream.Length - pos) / (bytePreSamplePreChannel * channels), sampleCount);
            for (int i = 0; i < min; i++)
            {
                double d = 0;

                for (int j = 0; j < channels; j++)
                {
                    d += readFunction();
                }

                d /= channels;

                readInComplexs[i] = new(d * window[i] / normalizeConst, 0);

            }
        }

        public void Dispose()
        {
            stream.Dispose();
            reader.Dispose();
        }
    }
}
