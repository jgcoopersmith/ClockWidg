using System.IO;
using System.Media;

namespace ClockWidg.Services;

/// <summary>
/// A self-contained looping beep. Generates a short WAV tone in memory
/// (no external audio files) and loops it until stopped.
/// </summary>
public sealed class AlertSound : IDisposable
{
    private SoundPlayer? _player;
    private MemoryStream? _stream;

    public bool IsPlaying { get; private set; }

    public void Start()
    {
        Stop();
        _stream = new MemoryStream(BuildBeepWav());
        _player = new SoundPlayer(_stream);
        _player.PlayLooping();
        IsPlaying = true;
    }

    public void Stop()
    {
        try { _player?.Stop(); } catch { }
        _player?.Dispose();
        _stream?.Dispose();
        _player = null;
        _stream = null;
        IsPlaying = false;
    }

    public void Dispose() => Stop();

    private static byte[] BuildBeepWav()
    {
        const int sampleRate = 44100;
        const double toneSeconds = 0.35;
        const double gapSeconds = 0.55;
        const double freq = 880.0;

        int toneSamples = (int)(sampleRate * toneSeconds);
        int gapSamples = (int)(sampleRate * gapSeconds);
        int fade = sampleRate / 100; // 10 ms fade to avoid clicks
        var samples = new short[toneSamples + gapSamples];

        for (int i = 0; i < toneSamples; i++)
        {
            double env = 1.0;
            if (i < fade) env = (double)i / fade;
            else if (i > toneSamples - fade) env = (double)(toneSamples - i) / fade;
            samples[i] = (short)(Math.Sin(2 * Math.PI * freq * i / sampleRate) * short.MaxValue * 0.35 * env);
        }
        // remaining samples stay 0 (silent gap)

        return WavFromSamples(samples, sampleRate);
    }

    private static byte[] WavFromSamples(short[] samples, int sampleRate)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        int dataBytes = samples.Length * 2;

        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataBytes);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);                 // PCM chunk size
        bw.Write((short)1);           // PCM
        bw.Write((short)1);           // mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);     // byte rate (mono, 16-bit)
        bw.Write((short)2);           // block align
        bw.Write((short)16);          // bits per sample
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(dataBytes);
        foreach (short s in samples) bw.Write(s);
        bw.Flush();
        return ms.ToArray();
    }
}
