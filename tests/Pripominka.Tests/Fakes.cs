namespace Pripominka.Tests;

internal sealed class FakeClock : IClock
{
    public DateTime Now { get; set; } = new(2026, 10, 29, 10, 0, 0);
}

internal sealed class FakeVolume : IVolume
{
    public VolumeState Current { get; set; } = new(0.4f, false);
    public List<VolumeState> Sets { get; } = new();

    public VolumeState Get() => Current;

    public void Set(VolumeState state)
    {
        Current = state;
        Sets.Add(state);
    }
}

internal sealed class FakeAudio : IAudioPlayer
{
    public event Action<Exception?>? Stopped;
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);
    public Exception? PlayError { get; set; }
    public List<string> Played { get; } = new();
    public bool Playing { get; private set; }

    public TimeSpan Play(string path)
    {
        if (PlayError is not null) throw PlayError;
        Played.Add(path);
        Playing = true;
        return Duration;
    }

    // Jako NAudio: Stop vyvolá Stopped.
    public void Stop()
    {
        if (!Playing) return;
        Playing = false;
        Stopped?.Invoke(null);
    }

    public void FinishNaturally(Exception? error = null)
    {
        Playing = false;
        Stopped?.Invoke(error);
    }
}

internal sealed class FakeOverlay : IOverlay
{
    public List<string?> Shown { get; } = new();
    public bool IsVisible { get; private set; }
    public Exception? ShowError { get; set; }

    public void Show(string? imagePath)
    {
        if (ShowError is not null) throw ShowError;
        Shown.Add(imagePath);
        IsVisible = true;
    }

    public void Hide() => IsVisible = false;
}
