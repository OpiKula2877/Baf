using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Pripominka;

internal static class Program
{
    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int processId);

    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var singleInstance = new Mutex(true, "Pripominka.SingleInstance", out var first);
        if (!first)
        {
            SignalRunningInstance();
            return 0;
        }

        var log = new AppLog(AppLog.DefaultPath);
        var settings = SettingsStore.Load(SettingsStore.DefaultPath);
        Application.Run(new TrayApp(settings, log, testOnStart: args.Contains("--test")));
        return 0;
    }

    private static void SignalRunningInstance()
    {
        try
        {
            using var signal = EventWaitHandle.OpenExisting(TrayApp.ShowSettingsEvent);
            AllowSetForegroundWindow(-1); // ASFW_ANY: běžící instance smí nastavení přenést navrch
            signal.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // Běžící instance ještě nevytvořila událost.
        }
    }
}
