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

namespace Penguin690_sMusicPlayer.Models;

internal class FFTWaver : IDisposable, IStatusSender
{
    private readonly int maxFrequency = 2525;
    private readonly int minFrequency = 50;
    public readonly int selectFrequenciesCount = 100;
    public readonly int[] FrequencyQuartiles;
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

    public Status Status { get; private set; }
    public event EventHandler<StatusEventArgs> StatusSendEvent;
    public void StatusRegister()
    {
        Status.Register(this);
    }
    public void StatusUpdate(string message)
    {
        StatusSendEvent?.Invoke(this, new StatusEventArgs(message));
    }

    public FFTWaver(Status status)
    {
        Status = status;
        int frequencyRange = maxFrequency - minFrequency;
        frequencySapn = frequencyRange / (selectFrequenciesCount - 1);
        double Precentage = 1 / 100d;
        FrequencyQuartiles = new int[5]
        {
            (int)(minFrequency + (frequencyRange * Precentage * 0)),
            (int)(minFrequency + (frequencyRange * Precentage * 25)),
            (int)(minFrequency + (frequencyRange * Precentage * 50)),
            (int)(minFrequency + (frequencyRange * Precentage * 75)),
            (int)(minFrequency + (frequencyRange * Precentage * 100)),
        };
        window = Window.Hamming(sampleCount);
        StatusRegister();
    }

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

        readInComplexs = new Complex[sampleCount * 4];
        normalizeConst = (int)Math.Pow(2, _stream.WaveFormat.BitsPerSample / channels - 10);
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
        StatusUpdate($"FFTWaver Setup successful; sampleCount: {sampleCount}, maxFreq: {maxFrequency}, minFreq: {minFrequency}");
    }

    public double[] CountFFT(long pos)
    {
        double[] outputFrequencies = new double[selectFrequenciesCount];
        ReadData(pos);
        Fourier.Forward(readInComplexs);
        for (int i = 0; i < selectFrequenciesCount; i++)
        {
            double selectFrequency = minFrequency + frequencySapn * i;
            int fftIndex = (int)(selectFrequency * sampleCount / sampleRate) * 4;
            fftIndex = Math.Min(fftIndex, sampleRate / 2);
            outputFrequencies[i] = readInComplexs[fftIndex].Magnitude * 2 / sampleCount;
        }
        return outputFrequencies;
    }

    private void ReadData(long pos)
    {
        Array.Fill(readInComplexs, Complex.Zero);
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