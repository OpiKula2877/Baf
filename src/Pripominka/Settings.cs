using System.Text.Json;

namespace Pripominka;

public sealed record AppSettings
{
    public string? ImagePath { get; init; }
    public string? AudioPath { get; init; }
    public int IntervalMinutes { get; init; } = 10;
    public int ShowSeconds { get; init; } = 3;
    public bool HoldUntilAudioEnds { get; init; }
    public int VolumePercent { get; init; } = 100;
    public bool RestoreVolume { get; init; } = true;
    public bool StartWithWindows { get; init; }
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Pripominka", "settings.json");

    public static AppSettings Load(string path)
    {
        if (!File.Exists(path)) return Normalize(new AppSettings());
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path));
            return Normalize(settings ?? new AppSettings());
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return Normalize(new AppSettings());
        }
    }

    public static void Save(string path, AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(Normalize(settings), Options));
    }

    public static AppSettings Normalize(AppSettings s) => s with
    {
        ImagePath = string.IsNullOrWhiteSpace(s.ImagePath) ? null : s.ImagePath,
        AudioPath = string.IsNullOrWhiteSpace(s.AudioPath) ? null : s.AudioPath,
        IntervalMinutes = Math.Clamp(s.IntervalMinutes, 1, 1440),
        ShowSeconds = Math.Clamp(s.ShowSeconds, 1, 600),
        VolumePercent = Math.Clamp(s.VolumePercent, 0, 100),
    };
}

public sealed class AppLog
{
    private readonly string _path;
    private readonly long _maxBytes;

    public AppLog(string path, long maxBytes = 1_000_000)
    {
        _path = path;
        _maxBytes = maxBytes;
    }

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Pripominka", "log.txt");

    public void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.AppendAllText(_path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
            Trim();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Log nesmí shodit upozornění.
        }
    }

    private void Trim()
    {
        if (new FileInfo(_path).Length <= _maxBytes) return;
        var bytes = File.ReadAllBytes(_path);
        var cut = Array.IndexOf(bytes, (byte)'\n', bytes.Length - (int)(_maxBytes / 2));
        File.WriteAllBytes(_path, bytes[(cut < 0 ? bytes.Length : cut + 1)..]);
    }
}
