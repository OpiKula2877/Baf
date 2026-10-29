using System.Drawing;
using System.Windows.Forms;

namespace Pripominka;

public sealed class SettingsForm : Form
{
    private readonly TextBox _imageBox = new() { ReadOnly = true, Width = 280 };
    private readonly TextBox _audioBox = new() { ReadOnly = true, Width = 280 };
    private readonly NumericUpDown _interval = new() { Minimum = 1, Maximum = 1440, Width = 80 };
    private readonly NumericUpDown _show = new() { Minimum = 1, Maximum = 600, Width = 80 };
    private readonly CheckBox _hold = new() { Text = "Držet, dokud nedohraje zvuk", AutoSize = true };
    private readonly TrackBar _volume = new() { Minimum = 0, Maximum = 100, TickFrequency = 10, Width = 220 };
    private readonly Label _volumeValue = new() { AutoSize = true };
    private readonly CheckBox _restore = new() { Text = "Po dohrání vrátit původní hlasitost", AutoSize = true };
    private readonly CheckBox _autostart = new() { Text = "Spouštět s Windows", AutoSize = true };
    private string? _imagePath;
    private string? _audioPath;

    public SettingsForm(AppSettings current)
    {
        Text = "Připomínka";
        Font = new Font(PreferredFontFamily(), 10f);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        _imagePath = current.ImagePath;
        _audioPath = current.AudioPath;
        _imageBox.Text = _imagePath ?? string.Empty;
        _audioBox.Text = _audioPath ?? string.Empty;
        _interval.Value = current.IntervalMinutes;
        _show.Value = current.ShowSeconds;
        _hold.Checked = current.HoldUntilAudioEnds;
        _volume.Value = current.VolumePercent;
        _volumeValue.Text = $"{_volume.Value} %";
        _volume.ValueChanged += (_, _) => _volumeValue.Text = $"{_volume.Value} %";
        _restore.Checked = current.RestoreVolume;
        _autostart.Checked = current.StartWithWindows;

        var table = new TableLayoutPanel { AutoSize = true, ColumnCount = 3 };
        var row = 0;
        void AddRow(Control first, Control second, Control third)
        {
            table.Controls.Add(first, 0, row);
            table.Controls.Add(second, 1, row);
            table.Controls.Add(third, 2, row);
            row++;
        }
        void AddFullRow(Control control)
        {
            table.Controls.Add(control, 0, row);
            table.SetColumnSpan(control, 3);
            row++;
        }

        AddRow(MakeLabel("Obrázek"), _imageBox, MakeButton("Vybrat…", (_, _) =>
            PickFile("Obrázek (*.png;*.jpg)|*.png;*.jpg", path => { _imagePath = path; _imageBox.Text = path; })));
        AddRow(MakeLabel("Zvuk"), _audioBox, Row(
            MakeButton("Vybrat…", (_, _) =>
                PickFile("Zvuk (*.mp3;*.wav)|*.mp3;*.wav", path => { _audioPath = path; _audioBox.Text = path; })),
            MakeButton("×", (_, _) => { _audioPath = null; _audioBox.Text = string.Empty; })));
        AddRow(MakeLabel("Připomenout každých"), _interval, MakeLabel("minut"));
        AddRow(MakeLabel("Zobrazit"), _show, MakeLabel("s"));
        AddFullRow(_hold);
        AddRow(MakeLabel("Hlasitost při upozornění"), _volume, _volumeValue);
        AddFullRow(_restore);
        AddFullRow(_autostart);
        AddFullRow(Row(
            MakeButton("Vyzkoušet", (_, _) => TestRequested?.Invoke(BuildSettings())),
            MakeButton("Uložit", (_, _) =>
            {
                Saved?.Invoke(BuildSettings());
                Close();
            })));

        Controls.Add(table);
    }

    // Formulář soubor nezapisuje. Uložení a autostart dělá TrayApp.
    public event Action<AppSettings>? Saved;

    // Vyzkoušení používá hodnoty z formuláře a neukládá je.
    public event Action<AppSettings>? TestRequested;

    private AppSettings BuildSettings() => new()
    {
        ImagePath = _imagePath,
        AudioPath = _audioPath,
        IntervalMinutes = (int)_interval.Value,
        ShowSeconds = (int)_show.Value,
        HoldUntilAudioEnds = _hold.Checked,
        VolumePercent = _volume.Value,
        RestoreVolume = _restore.Checked,
        StartWithWindows = _autostart.Checked,
    };

    private void PickFile(string filter, Action<string> onPicked)
    {
        using var dialog = new OpenFileDialog { Filter = filter };
        if (dialog.ShowDialog(this) == DialogResult.OK) onPicked(dialog.FileName);
    }

    private static string PreferredFontFamily() =>
        FontFamily.Families.Any(f => f.Name == "Segoe UI Variable Text") ? "Segoe UI Variable Text" : "Segoe UI";

    private static Label MakeLabel(string text) => new() { Text = text, AutoSize = true };

    private static Button MakeButton(string text, EventHandler onClick)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += onClick;
        return button;
    }

    private static FlowLayoutPanel Row(params Control[] items)
    {
        var panel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        panel.Controls.AddRange(items);
        return panel;
    }
}
