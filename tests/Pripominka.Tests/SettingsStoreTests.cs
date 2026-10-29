namespace Pripominka.Tests;

public class SettingsStoreTests
{
    private static string TempFile() => Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

    [Fact]
    public void Load_returns_defaults_when_file_missing()
    {
        Assert.Equal(new AppSettings(), SettingsStore.Load(TempFile()));
    }

    [Fact]
    public void Load_returns_defaults_when_json_is_corrupt()
    {
        var path = TempFile();
        File.WriteAllText(path, "{ nesmysl");
        Assert.Equal(new AppSettings(), SettingsStore.Load(path));
    }

    [Fact]
    public void Load_clamps_values_to_allowed_ranges()
    {
        var path = TempFile();
        File.WriteAllText(path, """{"IntervalMinutes":0,"ShowSeconds":99999,"VolumePercent":150}""");
        var s = SettingsStore.Load(path);
        Assert.Equal(1, s.IntervalMinutes);
        Assert.Equal(600, s.ShowSeconds);
        Assert.Equal(100, s.VolumePercent);
    }

    [Fact]
    public void Save_then_Load_round_trips()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "settings.json");
        var original = new AppSettings
        {
            ImagePath = @"C:\obrazky\a.png",
            AudioPath = @"C:\zvuky\b.mp3",
            IntervalMinutes = 15,
            ShowSeconds = 5,
            HoldUntilAudioEnds = true,
            VolumePercent = 40,
            RestoreVolume = false,
            StartWithWindows = true,
        };
        SettingsStore.Save(path, original);
        Assert.Equal(original, SettingsStore.Load(path));
    }
}
