using Microsoft.Win32;

namespace Pripominka.Tests;

public class AutostartTests
{
    private const string TestKey = @"Software\PripominkaTests";

    [Fact]
    public void Set_true_writes_quoted_path_and_IsEnabled_reports_true()
    {
        try
        {
            Autostart.Set(true, @"C:\Program Files\Pripominka\Pripominka.exe", TestKey, "Test");
            Assert.True(Autostart.IsEnabled(TestKey, "Test"));
            using var key = Registry.CurrentUser.OpenSubKey(TestKey)!;
            Assert.Equal("\"C:\\Program Files\\Pripominka\\Pripominka.exe\"", key.GetValue("Test"));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(TestKey, throwOnMissingSubKey: false);
        }
    }

    [Fact]
    public void Set_false_removes_value()
    {
        try
        {
            Autostart.Set(true, @"C:\x\Pripominka.exe", TestKey, "Test");
            Autostart.Set(false, @"C:\x\Pripominka.exe", TestKey, "Test");
            Assert.False(Autostart.IsEnabled(TestKey, "Test"));
            using var key = Registry.CurrentUser.OpenSubKey(TestKey)!;
            Assert.Null(key.GetValue("Test"));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(TestKey, throwOnMissingSubKey: false);
        }
    }
}
