using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Pripominka;

public sealed class TrayApp : ApplicationContext
{
    public const string ShowSettingsEvent = "Pripominka.ShowSettings";

    private readonly AppLog _log;
    private readonly Control _uiControl = new();
    private readonly AudioPlayer _audio;
    private readonly OverlayController _overlay;
    private readonly AlertController _controller;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private AppSettings _settings;
    private SettingsForm? _settingsForm;

    public TrayApp(AppSettings settings, AppLog log, bool testOnStart)
    {
        _log = log;
        _settings = settings;
        _ = _uiControl.Handle;
        _audio = new AudioPlayer(action => _uiControl.BeginInvoke(action));
        _overlay = new OverlayController(log);
        _controller = new AlertController(new SystemClock(), new VolumeControl(), _audio, _overlay, File.Exists, log);
        _overlay.DismissRequested += () => _controller.Dismiss();

        _pauseItem = new ToolStripMenuItem("Pozastavit", null, (_, _) => TogglePause());
        var menu = new ContextMenuStrip();
        menu.Items.Add("Vyzkoušet", null, (_, _) => _controller.TriggerTest());
        menu.Items.Add("Nastavení…", null, (_, _) => ShowSettings());
        menu.Items.Add(_pauseItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Ukončit", null, (_, _) => ExitThread());

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Připomínka",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => ShowSettings();
        _controller.Notice += text => _notifyIcon.ShowBalloonTip(3000, "Připomínka", text, ToolTipIcon.Warning);

        _controller.ApplySettings(settings);
        _timer.Tick += (_, _) => _controller.Tick();
        _timer.Start();
        StartShowSettingsListener();

        if (testOnStart) _controller.TriggerTest();
        else if (settings.ImagePath == null && settings.AudioPath == null) ShowSettings();
    }

    private void ShowSettings()
    {
        if (_settingsForm == null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm(_settings);
            _settingsForm.Saved += OnSettingsSaved;
            _settingsForm.TestRequested += s => _controller.TriggerTest(s);
        }
        _settingsForm.Show();
        _settingsForm.Activate();
    }

    private void OnSettingsSaved(AppSettings settings)
    {
        try
        {
            SettingsStore.Save(SettingsStore.DefaultPath, settings);
            Autostart.Set(settings.StartWithWindows, Application.ExecutablePath);
        }
        catch (Exception e)
        {
            _log.Write($"settings save failed: {e.Message}");
            _notifyIcon.ShowBalloonTip(3000, "Připomínka", "Nastavení se nepodařilo uložit", ToolTipIcon.Error);
        }
        _settings = settings;
        _controller.ApplySettings(settings);
    }

    private void TogglePause()
    {
        _controller.TogglePause();
        _pauseItem.Text = _controller.IsPaused ? "Pokračovat" : "Pozastavit";
    }

    // Druhé spuštění pošle signál a běžící instance otevře nastavení na UI vlákně.
    private void StartShowSettingsListener()
    {
        var signal = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEvent);
        var listener = new Thread(() =>
        {
            while (signal.WaitOne()) _uiControl.BeginInvoke(new Action(ShowSettings));
        })
        {
            IsBackground = true,
        };
        listener.Start();
    }

    private static Icon LoadIcon()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Pripominka.Assets.app.ico")!;
        return new Icon(stream);
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _timer.Dispose();
        _controller.Dismiss();
        _overlay.Dispose();
        _audio.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _uiControl.Dispose();
        base.ExitThreadCore();
    }
}
