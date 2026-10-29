namespace Pripominka.Tests;

public class AppLogTests
{
    [Fact]
    public void Write_appends_timestamped_line()
    {
        var path = Path.GetTempFileName();
        new AppLog(path).Write("test zprávy");
        var line = File.ReadAllLines(path).Single();
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} test zprávy$", line);
    }

    [Fact]
    public void Write_keeps_file_under_limit()
    {
        var path = Path.GetTempFileName();
        var log = new AppLog(path, maxBytes: 1000);
        for (var i = 0; i < 200; i++) log.Write($"řádek číslo {i}");
        Assert.True(new FileInfo(path).Length <= 1000);
    }
}
