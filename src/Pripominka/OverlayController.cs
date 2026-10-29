using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Pripominka;

public sealed class OverlayController : IOverlay, IDisposable
{
    private readonly AppLog _log;
    private readonly HotKeyWindow _hotKey;
    private readonly Dictionary<Size, Image> _defaultImages = new();
    private List<OverlayWindow> _windows = new();
    private string _layout = string.Empty;
    private Image? _userImage;
    private bool _cursorHidden;

    public OverlayController(AppLog log)
    {
        _log = log;
        _hotKey = new HotKeyWindow(() => DismissRequested?.Invoke());
        EnsureWindows();
    }

    public event Action? DismissRequested;

    public void Show(string? imagePath)
    {
        EnsureWindows();
        _userImage?.Dispose();
        _userImage = imagePath == null ? null : LoadImage(imagePath);
        foreach (var window in _windows)
        {
            window.Picture.Image = _userImage ?? DefaultImageFor(window.Bounds.Size);
            window.Show();
        }
        if (!_cursorHidden)
        {
            Cursor.Hide();
            _cursorHidden = true;
        }
        _windows.FirstOrDefault(w => w.IsPrimary)?.Activate();
        if (!_hotKey.Register()) _log.Write("Esc shortcut could not be registered");
    }

    public void Hide()
    {
        _hotKey.Unregister();
        foreach (var window in _windows)
        {
            window.Hide();
            window.Picture.Image = null;
        }
        _userImage?.Dispose();
        _userImage = null;
        if (_cursorHidden)
        {
            Cursor.Show();
            _cursorHidden = false;
        }
    }

    public void Dispose()
    {
        Hide();
        _hotKey.Dispose();
        foreach (var window in _windows) window.Dispose();
        foreach (var image in _defaultImages.Values) image.Dispose();
    }

    // Okna se vytvářejí předem, aby se Show dalo zobrazit okamžitě. Při změně monitorů se znovu sestaví.
    private void EnsureWindows()
    {
        var layout = string.Join(";", Screen.AllScreens.Select(s => s.Bounds));
        if (layout == _layout) return;
        foreach (var window in _windows) window.Dispose();
        _windows = Screen.AllScreens.Select(s => new OverlayWindow(s.Bounds, s.Primary)).ToList();
        foreach (var window in _windows) window.EscPressed += () => DismissRequested?.Invoke();
        _layout = layout;
    }

    // Soubor se čte přes paměť, aby zůstal nezamčený a šel smazat nebo přepsat.
    private Image? LoadImage(string path)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            using var stream = new MemoryStream(bytes);
            using var original = Image.FromStream(stream);
            return new Bitmap(original);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _log.Write($"image failed: {path}: {e.Message}");
            return null;
        }
    }

    private Image DefaultImageFor(Size size)
    {
        if (!_defaultImages.TryGetValue(size, out var image))
        {
            image = DefaultImage.Create(size.Width, size.Height);
            _defaultImages[size] = image;
        }
        return image;
    }
}

internal sealed class OverlayWindow : Form
{
    public OverlayWindow(Rectangle bounds, bool isPrimary)
    {
        IsPrimary = isPrimary;
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = bounds;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.Black;
        KeyPreview = true;
        Picture = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black,
        };
        Controls.Add(Picture);
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) EscPressed?.Invoke();
        };
        _ = Handle;
    }

    public bool IsPrimary { get; }
    public PictureBox Picture { get; }
    public event Action? EscPressed;
}

// Message-only okno pro globální klávesu Esc. Funguje i tehdy, když upozornění nemá fokus.
internal sealed class HotKeyWindow : NativeWindow, IDisposable
{
    private const int WmHotKey = 0x0312;
    private const int HotKeyId = 1;
    private const uint ModNoRepeat = 0x4000;
    private const uint VkEscape = 0x1B;
    private static readonly IntPtr MessageOnlyParent = new(-3);
    private readonly Action _onPressed;

    public HotKeyWindow(Action onPressed)
    {
        _onPressed = onPressed;
        CreateHandle(new CreateParams { Parent = MessageOnlyParent });
    }

    public bool Register() => RegisterHotKey(Handle, HotKeyId, ModNoRepeat, VkEscape);

    public void Unregister() => UnregisterHotKey(Handle, HotKeyId);

    public void Dispose() => DestroyHandle();

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotKey) _onPressed();
        base.WndProc(ref m);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
