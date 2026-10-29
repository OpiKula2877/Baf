namespace Pripominka.Tests;

internal sealed class Fixture
{
    public const string VoicePath = @"C:\test\voice.mp3";
    public const string ImagePath = @"C:\test\image.png";

    public FakeClock Clock { get; } = new();
    public FakeVolume Volume { get; } = new();
    public FakeAudio Audio { get; } = new();
    public FakeOverlay Overlay { get; } = new();
    public AlertController Controller { get; }

    public Fixture(Func<string, bool>? fileExists = null, AppSettings? settings = null)
    {
        Controller = new AlertController(Clock, Volume, Audio, Overlay,
            fileExists ?? (_ => true), new AppLog(Path.GetTempFileName()));
        Controller.ApplySettings(settings ?? Default());
    }

    public static AppSettings Default() => new()
    {
        AudioPath = VoicePath,
        ImagePath = ImagePath,
        IntervalMinutes = 10,
        ShowSeconds = 3,
        VolumePercent = 100,
    };
}

public class AlertControllerTests
{
    [Fact]
    public void Tick_before_due_time_does_nothing()
    {
        var f = new Fixture();
        f.Clock.Now = f.Controller.NextAlertAt.AddSeconds(-1);
        f.Controller.Tick();
        Assert.Empty(f.Audio.Played);
    }

    [Fact]
    public void Tick_at_due_time_starts_alert()
    {
        var f = new Fixture();
        f.Clock.Now = f.Controller.NextAlertAt;
        f.Controller.Tick();
        Assert.Equal(new VolumeState(1f, false), f.Volume.Sets.Last());
        Assert.Equal(Fixture.VoicePath, f.Audio.Played.Single());
        Assert.Equal(Fixture.ImagePath, f.Overlay.Shown.Single());
        Assert.True(f.Controller.IsAlertActive);
    }

    [Fact]
    public void Fixed_duration_hides_overlay_after_show_seconds_and_restores_volume_only_after_audio()
    {
        var f = new Fixture();
        f.Clock.Now = f.Controller.NextAlertAt;
        f.Controller.Tick();
        f.Clock.Now = f.Clock.Now.AddSeconds(3);
        f.Controller.Tick();
        Assert.False(f.Overlay.IsVisible);
        Assert.True(f.Controller.IsAlertActive);
        Assert.Equal(new VolumeState(1f, false), f.Volume.Current);
        f.Audio.FinishNaturally();
        Assert.Equal(new VolumeState(0.4f, false), f.Volume.Current);
        Assert.False(f.Controller.IsAlertActive);
    }

    [Fact]
    public void Hold_mode_keeps_overlay_until_audio_ends()
    {
        var f = new Fixture(settings: Fixture.Default() with { HoldUntilAudioEnds = true });
        f.Clock.Now = f.Controller.NextAlertAt;
        f.Controller.Tick();
        f.Clock.Now = f.Clock.Now.AddSeconds(30);
        f.Controller.Tick();
        Assert.True(f.Overlay.IsVisible);
        f.Audio.FinishNaturally();
        Assert.False(f.Overlay.IsVisible);
        Assert.False(f.Controller.IsAlertActive);
    }

    [Fact]
    public void Restores_original_level_and_mute_state()
    {
        var f = new Fixture();
        f.Volume.Current = new VolumeState(0.4f, true);
        f.Clock.Now = f.Controller.NextAlertAt;
        f.Controller.Tick();
        f.Audio.FinishNaturally();
        Assert.Equal(new VolumeState(0.4f, true), f.Volume.Current);
    }

    [Fact]
    public void Missing_audio_file_shows_overlay_without_touching_volume()
    {
        var f = new Fixture(fileExists: p => p != Fixture.VoicePath);
        string? notice = null;
        f.Controller.Notice += text => notice = text;
        f.Clock.Now = f.Controller.NextAlertAt;
        f.Controller.Tick();
        Assert.Empty(f.Volume.Sets);
        Assert.Empty(f.Audio.Played);
        Assert.Equal(Fixture.ImagePath, f.Overlay.Shown.Single());
        Assert.NotNull(notice);
    }

    [Fact]
    public void Missing_image_file_uses_default_image()
    {
        var f = new Fixture(fileExists: p => p != Fixture.ImagePath);
        f.Clock.Now = f.Controller.NextAlertAt;
        f.Controller.Tick();
        Assert.Null(f.Overlay.Shown.Single());
    }

    [Fact]
    public void Audio_start_failure_restores_volume()
    {
        var f = new Fixture();
        f.Audio.PlayError = new IOException("zvuk nejde spustit");
        f.Clock.Now = f.Controller.NextAlertAt;
        f.Controller.Tick();
        Assert.Equal(new VolumeState(0.4f, false), f.Volume.Current);
        Assert.True(f.Overlay.IsVisible);
        f.Clock.Now = f.Clock.Now.AddSeconds(3);
        f.Controller.Tick();
        Assert.False(f.Overlay.IsVisible);
        Assert.False(f.Controller.IsAlertActive);
    }

    [Fact]
    public void Dismiss_stops_audio_restores_volume_and_hides_overlay()
    {
        var f = new Fixture();
        f.Clock.Now = f.Controller.NextAlertAt;
        f.Controller.Tick();
        f.Controller.Dismiss();
        Assert.False(f.Audio.Playing);
        Assert.Equal(new VolumeState(0.4f, false), f.Volume.Current);
        Assert.False(f.Overlay.IsVisible);
        Assert.False(f.Controller.IsAlertActive);
    }

    [Fact]
    public void Dismiss_when_idle_does_nothing()
    {
        var f = new Fixture();
        f.Controller.Dismiss();
        Assert.Empty(f.Volume.Sets);
        Assert.Empty(f.Audio.Played);
        Assert.False(f.Overlay.IsVisible);
    }

    [Fact]
    public void Next_alert_is_scheduled_after_the_end_of_the_previous_one()
    {
        var f = new Fixture(settings: Fixture.Default() with { IntervalMinutes = 1, HoldUntilAudioEnds = true });
        var start = f.Controller.NextAlertAt;
        f.Clock.Now = start;
        f.Controller.Tick();
        f.Clock.Now = start.AddSeconds(60);
        f.Controller.Tick();
        Assert.Single(f.Audio.Played);
        f.Clock.Now = start.AddSeconds(90);
        f.Audio.FinishNaturally();
        Assert.Equal(start.AddSeconds(90).AddMinutes(1), f.Controller.NextAlertAt);
    }

    [Fact]
    public void Long_sleep_fires_one_alert_not_many()
    {
        var f = new Fixture();
        f.Clock.Now = f.Controller.NextAlertAt.AddHours(5);
        f.Controller.Tick();
        f.Audio.FinishNaturally();
        f.Clock.Now = f.Clock.Now.AddSeconds(3);
        f.Controller.Tick();
        f.Controller.Tick();
        Assert.Single(f.Audio.Played);
        Assert.Equal(f.Clock.Now.AddMinutes(10), f.Controller.NextAlertAt);
    }

    [Fact]
    public void Test_trigger_does_not_move_schedule()
    {
        var f = new Fixture();
        var before = f.Controller.NextAlertAt;
        Assert.True(f.Controller.TriggerTest());
        f.Audio.FinishNaturally();
        Assert.Equal(before, f.Controller.NextAlertAt);
    }

    [Fact]
    public void Test_trigger_with_given_settings_keeps_saved_ones()
    {
        var f = new Fixture();
        var schedule = f.Controller.NextAlertAt;
        Assert.True(f.Controller.TriggerTest(Fixture.Default() with { ShowSeconds = 7 }));
        f.Clock.Now = f.Clock.Now.AddSeconds(3);
        f.Controller.Tick();
        Assert.True(f.Overlay.IsVisible);
        f.Audio.FinishNaturally();
        f.Clock.Now = f.Clock.Now.AddSeconds(4);
        f.Controller.Tick();
        Assert.False(f.Overlay.IsVisible);
        Assert.Equal(schedule, f.Controller.NextAlertAt);
    }

    [Fact]
    public void Second_trigger_while_active_is_ignored()
    {
        var f = new Fixture();
        Assert.True(f.Controller.TriggerTest());
        Assert.False(f.Controller.TriggerTest());
    }

    [Fact]
    public void Paused_suppresses_scheduled_alerts_but_test_still_works()
    {
        var f = new Fixture();
        f.Controller.TogglePause();
        f.Clock.Now = f.Controller.NextAlertAt.AddMinutes(1);
        f.Controller.Tick();
        Assert.Empty(f.Audio.Played);
        Assert.True(f.Controller.TriggerTest());
    }

    [Fact]
    public void Resume_reschedules_from_now()
    {
        var f = new Fixture();
        f.Controller.TogglePause();
        f.Clock.Now = f.Clock.Now.AddHours(2);
        f.Controller.TogglePause();
        Assert.Equal(f.Clock.Now.AddMinutes(10), f.Controller.NextAlertAt);
    }

    [Fact]
    public void Settings_change_during_alert_is_applied_after_it_ends()
    {
        var f = new Fixture();
        f.Clock.Now = f.Controller.NextAlertAt;
        f.Controller.Tick();
        f.Controller.ApplySettings(Fixture.Default() with { ShowSeconds = 7 });
        f.Clock.Now = f.Clock.Now.AddSeconds(3);
        f.Controller.Tick();
        Assert.False(f.Overlay.IsVisible);
        f.Audio.FinishNaturally();
        f.Clock.Now = f.Controller.NextAlertAt;
        f.Controller.Tick();
        f.Clock.Now = f.Clock.Now.AddSeconds(6);
        f.Controller.Tick();
        Assert.True(f.Overlay.IsVisible);
    }

    [Fact]
    public void Shutdown_during_alert_restores_volume()
    {
        var f = new Fixture();
        f.Clock.Now = f.Controller.NextAlertAt;
        f.Controller.Tick();
        f.Controller.Dismiss();
        Assert.Equal(new VolumeState(0.4f, false), f.Volume.Current);
    }

    [Fact]
    public void Restore_disabled_keeps_alert_volume()
    {
        var f = new Fixture(settings: Fixture.Default() with { RestoreVolume = false });
        f.Clock.Now = f.Controller.NextAlertAt;
        f.Controller.Tick();
        f.Audio.FinishNaturally();
        Assert.Equal(new VolumeState(1f, false), f.Volume.Current);
    }

    [Fact]
    public void Overlay_failure_still_plays_audio_and_restores_volume()
    {
        var f = new Fixture();
        f.Overlay.ShowError = new InvalidOperationException("okno selhalo");
        f.Clock.Now = f.Controller.NextAlertAt;
        f.Controller.Tick();
        Assert.Single(f.Audio.Played);
        f.Audio.FinishNaturally();
        Assert.Equal(new VolumeState(0.4f, false), f.Volume.Current);
        Assert.False(f.Controller.IsAlertActive);
    }
}
