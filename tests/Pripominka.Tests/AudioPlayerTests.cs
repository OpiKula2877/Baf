using NAudio.Lame;
using NAudio.Wave;

namespace Pripominka.Tests;

public class AudioPlayerTests
{
    private static string TempPath(string extension) => Path.ChangeExtension(Path.GetTempFileName(), extension);

    // 0,3 s tónu 440 Hz, 16 bit, mono, zapsané jako WAV nebo MP3.
    private static void WriteTone(string path, bool mp3)
    {
        var format = new WaveFormat(44100, 16, 1);
        var samples = new short[(int)(format.SampleRate * 0.3)];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = (short)(Math.Sin(2 * Math.PI * 440 * i / format.SampleRate) * 0.3 * short.MaxValue);
        var bytes = new byte[samples.Length * 2];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        if (mp3)
        {
            using var writer = new LameMP3FileWriter(path, format, LAMEPreset.ABR_128);
            writer.Write(bytes, 0, bytes.Length);
        }
        else
        {
            using var writer = new WaveFileWriter(path, format);
            writer.Write(bytes, 0, bytes.Length);
        }
    }

    private static (TimeSpan Duration, Exception? Error) PlayAndWait(string path)
    {
        using var done = new ManualResetEventSlim();
        Exception? error = null;
        using var player = new AudioPlayer(a => a());
        player.Stopped += e =>
        {
            error = e;
            done.Set();
        };
        var duration = player.Play(path);
        Assert.True(done.Wait(TimeSpan.FromSeconds(3)), "Stopped nepřišel do 3 s");
        return (duration, error);
    }

    [Fact]
    public void Play_wav_raises_Stopped_after_its_duration()
    {
        var path = TempPath(".wav");
        WriteTone(path, mp3: false);
        var (duration, error) = PlayAndWait(path);
        Assert.InRange(duration.TotalSeconds, 0.25, 0.35);
        Assert.Null(error);
    }

    [Fact]
    public void Play_mp3_raises_Stopped()
    {
        var path = TempPath(".mp3");
        WriteTone(path, mp3: true);
        var (_, error) = PlayAndWait(path);
        Assert.Null(error);
    }

    [Fact]
    public void Play_missing_file_throws()
    {
        using var player = new AudioPlayer(a => a());
        Assert.ThrowsAny<IOException>(() => { player.Play(Path.Combine(Path.GetTempPath(), "neexistuje.mp3")); });
    }
}
