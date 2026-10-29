using System.Drawing;
using System.Drawing.Text;

namespace Pripominka;

public static class DefaultImage
{
    private const string Text = "Nezapomeň se učit";

    public static Bitmap Create(int width, int height)
    {
        var bitmap = new Bitmap(width, height);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.Black);
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using var font = new Font("Segoe UI Semibold", height / 8f, GraphicsUnit.Pixel);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        g.DrawString(Text, font, Brushes.White, new RectangleF(0, 0, width, height), format);
        return bitmap;
    }
}
