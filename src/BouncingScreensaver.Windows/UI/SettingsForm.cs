using BouncingScreensaver.Windows.Configuration;

namespace BouncingScreensaver.Windows.UI;

internal sealed class SettingsForm : Form
{
    private readonly SettingsStore _store;
    private readonly TextBox _logoSource = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown _speed = NumberBox(20, 4000);
    private readonly NumericUpDown _logoSize = NumberBox(3, 60);
    private readonly TextBox _background = new() { Dock = DockStyle.Fill };
    private readonly TextBox _flashColor = new() { Dock = DockStyle.Fill };
    private readonly CheckBox _flashEnabled = new() { AutoSize = true };
    private readonly NumericUpDown _flashDuration = NumberBox(50, 5000);
    private readonly NumericUpDown _cornerTolerance = NumberBox(0, 200);

    public SettingsForm(SettingsStore store)
    {
        _store = store;
        Text = "BouncingScreensaver Settings";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(620, 450);
        ClientSize = new Size(720, 480);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 10,
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(layout, 0, "Logo PNG source", _logoSource);
        AddRow(layout, 1, "Speed (pixels/second)", _speed);
        AddRow(layout, 2, "Logo width (% primary display)", _logoSize);
        AddRow(layout, 3, "Background colour", _background);
        AddRow(layout, 4, "Corner flash colour", _flashColor);
        AddRow(layout, 5, "Corner flash enabled", _flashEnabled);
        AddRow(layout, 6, "Flash duration (ms)", _flashDuration);
        AddRow(layout, 7, "Corner tolerance (pixels)", _cornerTolerance);

        var note = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(650, 0),
            Text = "PNG only. A UNC path is supported. The logo is cached per user under LocalAppData. " +
                   "Values under HKLM\\Software\\Policies\\BouncingScreensaver override local settings."
        };
        layout.Controls.Add(note, 0, 8);
        layout.SetColumnSpan(note, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };
        var save = new Button { Text = "Save", AutoSize = true };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        var reset = new Button { Text = "Reset user overrides", AutoSize = true };
        save.Click += (_, _) => Save();
        reset.Click += (_, _) => ResetUserOverrides();
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(reset);
        layout.Controls.Add(buttons, 0, 9);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);
        AcceptButton = save;
        CancelButton = cancel;
        LoadValues();
    }

    private void LoadValues()
    {
        var settings = _store.Load();
        _logoSource.Text = settings.LogoSource;
        _speed.Value = settings.SpeedPxPerSecond;
        _logoSize.Value = settings.LogoWidthPercent;
        _background.Text = settings.BackgroundColor;
        _flashColor.Text = settings.FlashColor;
        _flashEnabled.Checked = settings.FlashEnabled;
        _flashDuration.Value = settings.FlashDurationMs;
        _cornerTolerance.Value = settings.CornerTolerancePx;

        ApplyManagedState(_logoSource, nameof(ScreensaverSettings.LogoSource));
        ApplyManagedState(_speed, nameof(ScreensaverSettings.SpeedPxPerSecond));
        ApplyManagedState(_logoSize, nameof(ScreensaverSettings.LogoWidthPercent));
        ApplyManagedState(_background, nameof(ScreensaverSettings.BackgroundColor));
        ApplyManagedState(_flashColor, nameof(ScreensaverSettings.FlashColor));
        ApplyManagedState(_flashEnabled, nameof(ScreensaverSettings.FlashEnabled));
        ApplyManagedState(_flashDuration, nameof(ScreensaverSettings.FlashDurationMs));
        ApplyManagedState(_cornerTolerance, nameof(ScreensaverSettings.CornerTolerancePx));
    }

    private void Save()
    {
        var settings = new ScreensaverSettings
        {
            LogoSource = _logoSource.Text.Trim(),
            SpeedPxPerSecond = (int)_speed.Value,
            LogoWidthPercent = (int)_logoSize.Value,
            BackgroundColor = _background.Text.Trim(),
            FlashColor = _flashColor.Text.Trim(),
            FlashEnabled = _flashEnabled.Checked,
            FlashDurationMs = (int)_flashDuration.Value,
            CornerTolerancePx = (int)_cornerTolerance.Value
        };

        try
        {
            _store.SaveUser(settings);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Unable to save settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetUserOverrides()
    {
        _store.ClearUserOverrides();
        LoadValues();
    }

    private void ApplyManagedState(Control control, string settingName)
    {
        if (_store.IsManaged(settingName))
        {
            control.Enabled = false;
        }
    }

    private static NumericUpDown NumberBox(decimal min, decimal max) => new()
    {
        Minimum = min,
        Maximum = max,
        Dock = DockStyle.Left,
        Width = 140
    };

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 9, 3, 3)
        }, 0, row);
        layout.Controls.Add(control, 1, row);
    }
}
