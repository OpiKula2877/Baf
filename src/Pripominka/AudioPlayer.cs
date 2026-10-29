using NAudio.Wave;

namespace Pripominka;

public sealed class AudioPlayer : IAudioPlayer, IDisposable
{
    private readonly Action<Action> _dispatch;
    private AudioFileReader? _reader;
    private WaveOut? _output;

    public AudioPlayer(Action<Action> dispatch) => _dispatch = dispatch;

    public event Action<Exception?>? Stopped;

    public TimeSpan Play(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Soubor zvuku neexistuje.", path);
        Stop();
        Release();
        var reader = new AudioFileReader(path);
        var output = new WaveOut { BufferMilliseconds = 100 };
        output.Init(reader);
        _reader = reader;
        _output = output;
        output.PlaybackStopped += (_, e) => _dispatch(() =>
        {
            // Zpožděná událost z předchozího přehrávání se zahodí; kontroluje se až na UI vlákně.
            if (ReferenceEquals(output, _output)) Stopped?.Invoke(e.Exception);
        });
        output.Play();
        return reader.TotalTime;
    }

    public void Stop() => _output?.Stop();

    public void Dispose()
    {
        Stop();
        Release();
    }

    // Zdroje se uvolňují až tady, ne uvnitř události přehrávání.
    private void Release()
    {
        _output?.Dispose();
        _reader?.Dispose();
        _output = null;
        _reader = null;
    }
}
