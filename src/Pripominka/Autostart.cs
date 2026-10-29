using Microsoft.Win32;

namespace Pripominka;

public static class Autostart
{
    public const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ValueName = "Pripominka";

    public static bool IsEnabled(string keyPath = RunKey, string valueName = ValueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(keyPath);
        return key?.GetValue(valueName) is string;
    }

    // Vypnutí smaže jen hodnotu. Klíč Run zůstává, protože ho používají další programy.
    public static void Set(bool enabled, string exePath, string keyPath = RunKey, string valueName = ValueName)
    {
        if (enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(keyPath);
            key.SetValue(valueName, $"\"{exePath}\"");
        }
        else
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
            key?.DeleteValue(valueName, throwOnMissingValue: false);
        }
    }
}
