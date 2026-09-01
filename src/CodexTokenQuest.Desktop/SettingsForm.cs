using System.Drawing.Drawing2D;

namespace CodexTokenQuest.Desktop;

internal sealed class SettingsForm : Form
{
    private readonly PixelNumberDisplay _minutesDisplay;
    private int _refreshMinutes;

    internal int RefreshMinutes => _refreshMinutes;

    internal SettingsForm(int currentMinutes)
    {
        _refreshMinutes = Math.Clamp(currentMinutes, DesktopSettings.MinimumRefreshMinutes, DesktopSettings.MaximumRefreshMinutes);

        Text = "Codex Token Quest Options";
        ClientSize = new Size(360, 270);
        BackColor = HudColors.Background;
        ForeColor = HudColors.Text;
        Font = new Font("Consolas", 8f, FontStyle.Bold);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        DoubleBuffered = true;
        KeyPreview = true;

        var title = new Label
        {
            Text = "◆ GAME OPTIONS ◆",
            Font = new Font("Consolas", 11f, FontStyle.Bold),
            ForeColor = HudColors.Gold,
            BackColor = Color.Transparent,
            Location = new Point(18, 15),
            Size = new Size(260, 22)
        };
        var close = CreatePixelButton("×", HudColors.Red, 316, 11, 28, 27);
        close.DialogResult = DialogResult.Cancel;

        var section = new Label
        {
            Text = "AUTO-SAVE INTERVAL",
            Font = new Font("Consolas", 8.5f, FontStyle.Bold),
            ForeColor = HudColors.Cyan,
            BackColor = Color.Transparent,
            Location = new Point(20, 55),
            Size = new Size(200, 18)
        };
        var description = new Label
        {
            Text = "多久重新讀取一次 Codex 用量（1–1440 分鐘）",
            ForeColor = HudColors.Muted,
            BackColor = Color.Transparent,
            Location = new Point(20, 74),
            Size = new Size(320, 17)
        };

        var minusFive = CreatePixelButton("-5", HudColors.Amber, 17, 99, 42, 37);
        var minusOne = CreatePixelButton("-1", HudColors.Cyan, 63, 99, 42, 37);
        _minutesDisplay = new PixelNumberDisplay
        {
            Location = new Point(109, 99),
            Size = new Size(136, 37),
            Minutes = _refreshMinutes
        };
        var plusOne = CreatePixelButton("+1", HudColors.Cyan, 249, 99, 42, 37);
        var plusFive = CreatePixelButton("+5", HudColors.Amber, 295, 99, 42, 37);
        minusFive.Click += (_, _) => AdjustMinutes(-5);
        minusOne.Click += (_, _) => AdjustMinutes(-1);
        plusOne.Click += (_, _) => AdjustMinutes(1);
        plusFive.Click += (_, _) => AdjustMinutes(5);

        var quickLabel = new Label
        {
            Text = "QUICK SLOTS",
            ForeColor = HudColors.Muted,
            BackColor = Color.Transparent,
            Location = new Point(20, 148),
            Size = new Size(120, 15)
        };
        var quickValues = new[] { 1, 5, 15, 60 };
        for (var index = 0; index < quickValues.Length; index++)
        {
            var value = quickValues[index];
            var quick = CreatePixelButton($"{value}M", HudColors.Green, 20 + index * 81, 166, 74, 31);
            quick.Click += (_, _) => SetMinutes(value);
            Controls.Add(quick);
        }

        var cancel = CreatePixelButton("CANCEL", HudColors.Red, 176, 218, 78, 34);
        cancel.DialogResult = DialogResult.Cancel;
        var save = CreatePixelButton("SAVE", HudColors.Green, 264, 218, 78, 34);
        save.DialogResult = DialogResult.OK;

        AcceptButton = save;
        CancelButton = cancel;
        Controls.AddRange([title, close, section, description, minusFive, minusOne, _minutesDisplay, plusOne, plusFive, quickLabel, cancel, save]);

        Shown += (_, _) =>
        {
            BringToFront();
            Activate();
        };
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.None;
        PixelArt.DrawPanel(eventArgs.Graphics, ClientRectangle, HudColors.Gold);
        using var divider = new Pen(HudColors.Grid, 2f);
        eventArgs.Graphics.DrawLine(divider, 18, 45, ClientSize.Width - 18, 45);
        eventArgs.Graphics.DrawLine(divider, 18, 207, ClientSize.Width - 18, 207);
    }

    protected override void OnKeyDown(KeyEventArgs eventArgs)
    {
        if (eventArgs.KeyCode is Keys.Left or Keys.Down)
        {
            AdjustMinutes(eventArgs.Shift ? -5 : -1);
            eventArgs.Handled = true;
        }
        else if (eventArgs.KeyCode is Keys.Right or Keys.Up)
        {
            AdjustMinutes(eventArgs.Shift ? 5 : 1);
            eventArgs.Handled = true;
        }
        base.OnKeyDown(eventArgs);
    }

    private void AdjustMinutes(int delta) => SetMinutes(_refreshMinutes + delta);

    private void SetMinutes(int value)
    {
        _refreshMinutes = Math.Clamp(value, DesktopSettings.MinimumRefreshMinutes, DesktopSettings.MaximumRefreshMinutes);
        _minutesDisplay.Minutes = _refreshMinutes;
    }

    private static Button CreatePixelButton(string text, Color accent, int x, int y, int width, int height)
    {
        var button = new Button
        {
            Text = text,
            Font = new Font("Consolas", 8f, FontStyle.Bold),
            ForeColor = accent,
            BackColor = HudColors.Panel,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(x, y),
            Size = new Size(width, height),
            Cursor = Cursors.Hand,
            TabStop = true
        };
        button.FlatAppearance.BorderColor = accent;
        button.FlatAppearance.BorderSize = 2;
        button.FlatAppearance.MouseOverBackColor = HudColors.PanelBright;
        button.FlatAppearance.MouseDownBackColor = HudColors.Ink;
        return button;
    }
}

internal sealed class PixelNumberDisplay : Control
{
    private int _minutes;

    internal int Minutes
    {
        get => _minutes;
        set
        {
            _minutes = value;
            Invalidate();
        }
    }

    internal PixelNumberDisplay()
    {
        DoubleBuffered = true;
        BackColor = HudColors.Ink;
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.None;
        using var outer = new Pen(HudColors.Ink, 3f);
        using var border = new Pen(HudColors.Gold, 2f);
        eventArgs.Graphics.DrawRectangle(outer, 1, 1, Width - 3, Height - 3);
        eventArgs.Graphics.DrawRectangle(border, 3, 3, Width - 7, Height - 7);
        using var valueFont = new Font("Consolas", 12f, FontStyle.Bold);
        TextRenderer.DrawText(
            eventArgs.Graphics,
            $"{_minutes:0000} MIN",
            valueFont,
            ClientRectangle,
            HudColors.Cream,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
