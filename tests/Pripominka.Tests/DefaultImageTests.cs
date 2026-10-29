using System.Drawing;

namespace Pripominka.Tests;

public class DefaultImageTests
{
    [Fact]
    public void Create_returns_requested_size_with_white_text()
    {
        using var bmp = DefaultImage.Create(640, 360);
        Assert.Equal(640, bmp.Width);
        Assert.Equal(360, bmp.Height);
        Assert.Equal(Color.Black.ToArgb(), bmp.GetPixel(0, 0).ToArgb());
        var hasWhite = false;
        for (var y = 0; y < bmp.Height && !hasWhite; y += 4)
            for (var x = 0; x < bmp.Width && !hasWhite; x += 4)
                hasWhite = bmp.GetPixel(x, y).R > 200;
        Assert.True(hasWhite);
    }
}
