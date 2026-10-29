namespace Pripominka;

public readonly record struct VolumeState(float Level, bool Muted);

public interface IVolume
{
    VolumeState Get();
    void Set(VolumeState state);
}

public interface IAudioPlayer
{
    event Action<Exception?>? Stopped;
    TimeSpan Play(string path);
    void Stop();
}

public interface IOverlay
{
    void Show(string? imagePath);
    void Hide();
}

public interface IClock
{
    DateTime Now { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}

public sealed class AlertController
{
    private readonly IClock _clock;
    private readonly IVolume _volume;
    private readonly IAudioPlayer _audio;
    private readonly IOverlay _overlay;
    private readonly Func<string, bool> _fileExists;
    private readonly AppLog _log;

    private AppSettings _settings = new();
    private AppSettings? _pendingSettings;
    private AppSettings _active = new();   // snímek nastavení běžícího upozornění
    private DateTime _nextAlertAt;
    private bool _isPaused;
    private bool _isAlertActive;
    private bool _scheduledAlert;
    private bool _audioActive;
    private bool _overlayActive;
    private DateTime? _overlayEndsAt;
    private bool _volumeChanged;
    private VolumeState _previousVolume;

    public AlertController(IClock clock, IVolume volume, IAudioPlayer audio, IOverlay overlay,
                           Func<string, bool> fileExists, AppLog log)
    {
        _clock = clock;
        _volume = volume;
        _audio = audio;
        _overlay = overlay;
        _fileExists = fileExists;
        _log = log;
        _audio.Stopped += OnAudioEnded;
        ApplySettings(new AppSettings());
    }

    public event Action<string>? Notice;

    public bool IsPaused => _isPaused;
    public bool IsAlertActive => _isAlertActive;
    public DateTime NextAlertAt => _nextAlertAt;

    public void ApplySettings(AppSettings settings)
    {
        if (_isAlertActive)
        {
            _pendingSettings = settings;
            return;
        }
        _settings = settings;
        _nextAlertAt = _clock.Now.AddMinutes(settings.IntervalMinutes);
    }

    public void Tick()
    {
        if (_isAlertActive)
        {
            if (_overlayActive && _overlayEndsAt <= _clock.Now) HideOverlay();
            MaybeFinish();
        }
        else if (!_isPaused && _clock.Now >= _nextAlertAt)
        {
            Start(scheduled: true, _settings);
        }
    }

    // Vyzkoušení může dostat nastavení z formuláře. Uložené nastavení a časovač se nemění.
    public bool TriggerTest(AppSettings? settings = null)
    {
        if (_isAlertActive) return false;
        Start(scheduled: false, settings ?? _settings);
        return true;
    }

    public void Dismiss()
    {
        if (!_isAlertActive) return;
        if (_audioActive)
        {
            _audio.Stop();
            OnAudioEnded(null);
        }
        if (_overlayActive) HideOverlay();
        _log.Write("dismissed");
        MaybeFinish();
    }

    public void TogglePause()
    {
        _isPaused = !_isPaused;
        if (!_isPaused) _nextAlertAt = _clock.Now.AddMinutes(_settings.IntervalMinutes);
    }

    private void Start(bool scheduled, AppSettings settings)
    {
        var now = _clock.Now;
        _active = settings;
        var audioPath = ResolveFile(settings.AudioPath, "Zvuk nenalezen");
        var imagePath = ResolveFile(settings.ImagePath, "Obrázek nenalezen, použit výchozí");
        _isAlertActive = true;
        _scheduledAlert = scheduled;
        _audioActive = false;
        _overlayActive = false;
        _log.Write("start");

        if (audioPath != null)
        {
            _previousVolume = _volume.Get();
            _volume.Set(new VolumeState(settings.VolumePercent / 100f, Muted: false));
            _volumeChanged = true;
            _log.Write($"volume set {settings.VolumePercent}% (before {_previousVolume.Level:0.00}, muted {_previousVolume.Muted})");
            try
            {
                _audio.Play(audioPath);
                _audioActive = true;
                _log.Write("audio started");
            }
            catch (Exception e)
            {
                _log.Write($"audio failed: {e.Message}");
                Notice?.Invoke("Zvuk nejde přehrát");
                RestoreVolume();
            }
        }

        try
        {
            _overlay.Show(imagePath);
            _overlayActive = true;
            _log.Write("overlay shown");
        }
        catch (Exception e)
        {
            _log.Write($"overlay failed: {e.Message}");
        }

        _overlayEndsAt = settings.HoldUntilAudioEnds && _audioActive ? null : now.AddSeconds(settings.ShowSeconds);
        MaybeFinish();
    }

    private string? ResolveFile(string? path, string notice)
    {
        if (path == null) return null;
        if (_fileExists(path)) return path;
        _log.Write($"{notice}: {path}");
        Notice?.Invoke(notice);
        return null;
    }

    private void OnAudioEnded(Exception? error)
    {
        if (!_audioActive) return;
        _audioActive = false;
        if (error != null)
        {
            _log.Write($"audio error: {error.Message}");
            Notice?.Invoke("Chyba při přehrávání zvuku");
        }
        RestoreVolume();
        if (_active.HoldUntilAudioEnds && _overlayActive) HideOverlay();
        _log.Write("audio ended");
        MaybeFinish();
    }

    private void RestoreVolume()
    {
        if (!_volumeChanged) return;
        _volumeChanged = false;
        if (!_active.RestoreVolume) return;
        _volume.Set(_previousVolume);
        _log.Write($"volume restored (level {_previousVolume.Level:0.00}, muted {_previousVolume.Muted})");
    }

    private void HideOverlay()
    {
        _overlay.Hide();
        _overlayActive = false;
        _log.Write("overlay hidden");
    }

    private void MaybeFinish()
    {
        if (!_isAlertActive || _audioActive || _overlayActive) return;
        _isAlertActive = false;
        if (_pendingSettings != null)
        {
            _settings = _pendingSettings;
            _pendingSettings = null;
        }
        if (_scheduledAlert) _nextAlertAt = _clock.Now.AddMinutes(_settings.IntervalMinutes);
        _log.Write("end");
    }
}
