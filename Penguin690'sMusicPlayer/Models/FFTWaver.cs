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

namespace Penguin690_sMusicPlayer.Models
{
    internal class FFTWaver : IDisposable
    {
        private readonly int maxFrequency = 2500;
        private readonly int minFrequency = 65;
        private readonly int selectFrequenciesCount = 6;
        private int sampleCount = 1024;
        private int frequencySapn;
        private int bytePreSample;
        private int sampleRate;
        private int normalizeConst;
        private double[] window;
        private Complex[] readInComplexs;
        private Func<double> readFunction;


        private BinaryReader reader;
        private WaveStream stream;

        public FFTWaver()
        {
            frequencySapn = (maxFrequency - minFrequency) / (selectFrequenciesCount - 1);
            window = Window.Hamming(sampleCount);
        }

        public int SelectFrequenciesCount { get; set; }

        public void SetMusic(MusicFile file)
        {
            if (reader != null)
            {
                reader.Dispose();
            }
            if (stream != null)
            {
                stream.Dispose();
            }
            AudioFileReader _reader = new(file.FullPath);
            stream = _reader;
            reader = new(stream);
            bytePreSample = _reader.WaveFormat.BitsPerSample / 8;
            sampleRate = _reader.WaveFormat.SampleRate;
            normalizeConst = (int)Math.Pow(2, _reader.WaveFormat.BitsPerSample - 7);
            readFunction = null;
            switch (bytePreSample)
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
        }

        public double[] CountFFT(long pos)
        {
            double[] outputFrequencies = new double[selectFrequenciesCount];
            try
            {
                ReadData(pos);
                Fourier.Forward(readInComplexs);
                for (int i = 0; i < selectFrequenciesCount; i++)
                {
                    double selectFrequency = minFrequency + frequencySapn * i;
                    int fftIndex = (int)(selectFrequency * sampleCount / sampleRate);
                    fftIndex = Math.Min(fftIndex, sampleRate / 2);
                    outputFrequencies[i] = readInComplexs[fftIndex].Magnitude * 2 / sampleCount;
                }
            }
            catch
            {
                Array.Fill(outputFrequencies, 0);
            }
            
            return outputFrequencies;
        }

        private void ReadData(long pos)
        {
            readInComplexs = new Complex[sampleCount];
            stream.Position = pos;
            int min = (int)Math.Min(stream.Length - pos - 1, sampleCount);
            for (int i = 0; i < min; i++)
            {
                readInComplexs[i] = new(readFunction() * window[i] / normalizeConst, 0);
            }
        }

        public void Dispose()
        {
            reader.Dispose();
            stream.Dispose();
        }
    }
}
