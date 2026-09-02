using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace CodexTokenQuest.Desktop;

internal sealed class SettingsForm : Form
{
    private readonly PixelNumberDisplay _minutesDisplay;
    private readonly PixelScaleBar _scaleBar;
    private readonly Label _scaleValue;
    private readonly Label _marginValue;
    private int _refreshMinutes;
    private int _hudScalePercent;
    private int _margin;

    internal int RefreshMinutes => _refreshMinutes;
    internal int HudScalePercent => _hudScalePercent;
    internal int HudMargin => _margin;

    internal SettingsForm(int currentMinutes, int currentHudScalePercent, int currentMargin)
    {
        _refreshMinutes = Math.Clamp(currentMinutes, DesktopSettings.MinimumRefreshMinutes, DesktopSettings.MaximumRefreshMinutes);
        _hudScalePercent = Math.Clamp(currentHudScalePercent, DesktopSettings.MinimumHudScalePercent, DesktopSettings.MaximumHudScalePercent);
        _margin = Math.Clamp(currentMargin, DesktopSettings.MinimumMargin, DesktopSettings.MaximumMargin);

        Text = "Codex Token Quest Options";
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(HudScale.Px(360), HudScale.Px(360));
        BackColor = HudColors.Background;
        ForeColor = HudColors.Text;
        Font = PixelArt.CreateHudFont(8f);
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
            Font = PixelArt.CreateHudFont(11f),
            ForeColor = HudColors.Gold,
            BackColor = Color.Transparent,
            Location = new Point(HudScale.Px(18), HudScale.Px(15)),
            Size = new Size(HudScale.Px(260), HudScale.Px(22))
        };
        var close = CreatePixelButton("×", HudColors.Red, 316, 11, 28, 27);
        close.DialogResult = DialogResult.Cancel;

        var section = new Label
        {
            Text = "AUTO-SAVE INTERVAL",
            Font = PixelArt.CreateHudFont(8.5f),
            ForeColor = HudColors.Cyan,
            BackColor = Color.Transparent,
            Location = new Point(HudScale.Px(20), HudScale.Px(55)),
            Size = new Size(HudScale.Px(200), HudScale.Px(18))
        };
        var description = new Label
        {
            Text = "多久重新讀取一次 Codex 用量（1–1440 分鐘）",
            ForeColor = HudColors.Muted,
            BackColor = Color.Transparent,
            Location = new Point(HudScale.Px(20), HudScale.Px(74)),
            Size = new Size(HudScale.Px(320), HudScale.Px(17))
        };

        var minusFive = CreatePixelButton("-5", HudColors.Amber, 17, 99, 42, 37);
        var minusOne = CreatePixelButton("-1", HudColors.Cyan, 63, 99, 42, 37);
        _minutesDisplay = new PixelNumberDisplay
        {
            Location = new Point(HudScale.Px(109), HudScale.Px(99)),
            Size = new Size(HudScale.Px(136), HudScale.Px(37)),
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
            Location = new Point(HudScale.Px(20), HudScale.Px(148)),
            Size = new Size(HudScale.Px(120), HudScale.Px(15))
        };
        var quickValues = new[] { 1, 5, 15, 60 };
        for (var index = 0; index < quickValues.Length; index++)
        {
            var value = quickValues[index];
            var quick = CreatePixelButton($"{value}M", HudColors.Green, 20 + index * 81, 166, 74, 31);
            quick.Click += (_, _) => SetMinutes(value);
            Controls.Add(quick);
        }

        var sizeSection = new Label
        {
            Text = "HUD SIZE",
            Font = PixelArt.CreateHudFont(8.5f),
            ForeColor = HudColors.Cyan,
            BackColor = Color.Transparent,
            Location = new Point(HudScale.Px(20), HudScale.Px(207)),
            Size = new Size(HudScale.Px(120), HudScale.Px(18))
        };
        var sizeDescription = new Label
        {
            Text = "拖曳調整介面大小（50%–300%）",
            ForeColor = HudColors.Muted,
            BackColor = Color.Transparent,
            Location = new Point(HudScale.Px(20), HudScale.Px(226)),
            Size = new Size(HudScale.Px(320), HudScale.Px(17))
        };
        _scaleBar = new PixelScaleBar
        {
            Location = new Point(HudScale.Px(20), HudScale.Px(246)),
            Size = new Size(HudScale.Px(242), HudScale.Px(27)),
            Value = _hudScalePercent
        };
        _scaleValue = new Label
        {
            Text = $"{_hudScalePercent}%",
            Font = PixelArt.CreateHudFont(8f),
            ForeColor = HudColors.Gold,
            BackColor = HudColors.Panel,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(HudScale.Px(270), HudScale.Px(246)),
            Size = new Size(HudScale.Px(70), HudScale.Px(27))
        };
        _scaleBar.ValueChanged += (_, _) => SetHudScale(_scaleBar.Value);

        var marginSection = new Label
        {
            Text = "HUD MARGIN",
            Font = PixelArt.CreateHudFont(8f),
            ForeColor = HudColors.Cyan,
            BackColor = Color.Transparent,
            Location = new Point(HudScale.Px(20), HudScale.Px(283)),
            Size = new Size(HudScale.Px(142), HudScale.Px(20)),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var marginMinus = CreatePixelButton("-", HudColors.Cyan, 174, 278, 34, 29);
        _marginValue = new Label
        {
            Text = $"{_margin} PX",
            Font = PixelArt.CreateHudFont(8f),
            ForeColor = HudColors.Gold,
            BackColor = HudColors.Panel,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(HudScale.Px(212), HudScale.Px(278)),
            Size = new Size(HudScale.Px(86), HudScale.Px(29))
        };
        var marginPlus = CreatePixelButton("+", HudColors.Cyan, 302, 278, 38, 29);
        marginMinus.Click += (_, _) => AdjustMargin(-1);
        marginPlus.Click += (_, _) => AdjustMargin(1);

        var cancel = CreatePixelButton("CANCEL", HudColors.Red, 176, 316, 78, 28);
        cancel.DialogResult = DialogResult.Cancel;
        var save = CreatePixelButton("SAVE", HudColors.Green, 264, 316, 78, 28);
        save.DialogResult = DialogResult.OK;

        AcceptButton = save;
        CancelButton = cancel;
        Controls.AddRange([title, close, section, description, minusFive, minusOne, _minutesDisplay, plusOne, plusFive, quickLabel, sizeSection, sizeDescription, _scaleBar, _scaleValue, marginSection, marginMinus, _marginValue, marginPlus, cancel, save]);

        Shown += (_, _) =>
        {
            BringToFront();
            Activate();
        };
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        var state = eventArgs.Graphics.Save();
        eventArgs.Graphics.ScaleTransform((float)HudScale.Factor, (float)HudScale.Factor);
        var width = HudScale.Logical(ClientSize.Width);
        var height = HudScale.Logical(ClientSize.Height);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.None;
        PixelArt.DrawPanel(eventArgs.Graphics, new Rectangle(0, 0, width, height), HudColors.Gold);
        using var divider = new Pen(HudColors.Grid, 2f);
        eventArgs.Graphics.DrawLine(divider, 18, 45, width - 18, 45);
        eventArgs.Graphics.DrawLine(divider, 18, 199, width - 18, 199);
        eventArgs.Graphics.DrawLine(divider, 18, 312, width - 18, 312);
        eventArgs.Graphics.Restore(state);
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

    private void SetHudScale(int percent)
    {
        _hudScalePercent = Math.Clamp(percent, DesktopSettings.MinimumHudScalePercent, DesktopSettings.MaximumHudScalePercent);
        _scaleValue.Text = $"{_hudScalePercent}%";
    }

    private void AdjustMargin(int delta)
    {
        _margin = Math.Clamp(_margin + delta, DesktopSettings.MinimumMargin, DesktopSettings.MaximumMargin);
        _marginValue.Text = $"{_margin} PX";
    }

    private static Button CreatePixelButton(string text, Color accent, int x, int y, int width, int height)
    {
        var button = new Button
        {
            Text = text,
            Font = PixelArt.CreateHudFont(8f),
            ForeColor = accent,
            BackColor = HudColors.Panel,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(HudScale.Px(x), HudScale.Px(y)),
            Size = new Size(HudScale.Px(width), HudScale.Px(height)),
            Cursor = Cursors.Hand,
            TabStop = true
        };
        button.FlatAppearance.BorderColor = accent;
        button.FlatAppearance.BorderSize = HudScale.Px(2);
        button.FlatAppearance.MouseOverBackColor = HudColors.PanelBright;
        button.FlatAppearance.MouseDownBackColor = HudColors.Ink;
        return button;
    }
}

internal sealed class PixelScaleBar : Control
{
    private int _value = DesktopSettings.DefaultHudScalePercent;
    private bool _dragging;

    internal event EventHandler? ValueChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int Value
    {
        get => _value;
        set
        {
            var next = Math.Clamp(value, DesktopSettings.MinimumHudScalePercent, DesktopSettings.MaximumHudScalePercent);
            if (next == _value) return;
            _value = next;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal PixelScaleBar()
    {
        DoubleBuffered = true;
        BackColor = HudColors.Panel;
        Cursor = Cursors.Hand;
        TabStop = true;
        SetStyle(ControlStyles.Selectable, true);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        var state = eventArgs.Graphics.Save();
        eventArgs.Graphics.ScaleTransform((float)HudScale.Factor, (float)HudScale.Factor);
        var width = HudScale.Logical(Width);
        var height = HudScale.Logical(Height);
        PixelArt.DrawPanel(eventArgs.Graphics, new Rectangle(0, 0, width, height), Focused ? HudColors.Gold : HudColors.Cyan);
        var track = new Rectangle(10, height / 2 - 5, Math.Max(1, width - 20), 10);
        var progress = (_value - DesktopSettings.MinimumHudScalePercent) * 100m /
                       (DesktopSettings.MaximumHudScalePercent - DesktopSettings.MinimumHudScalePercent);
        PixelArt.DrawBar(eventArgs.Graphics, track, progress, HudColors.Cyan);
        var thumbX = track.Left + (int)Math.Round((track.Width - 1) * progress / 100m);
        using var thumb = new SolidBrush(HudColors.Gold);
        eventArgs.Graphics.FillRectangle(thumb, thumbX - 3, track.Top - 4, 7, track.Height + 8);
        eventArgs.Graphics.Restore(state);
    }

    protected override void OnMouseDown(MouseEventArgs eventArgs)
    {
        base.OnMouseDown(eventArgs);
        if (eventArgs.Button != MouseButtons.Left) return;
        Focus();
        _dragging = true;
        Capture = true;
        SetValueFromX(HudScale.Logical(eventArgs.X));
    }

    protected override void OnMouseMove(MouseEventArgs eventArgs)
    {
        base.OnMouseMove(eventArgs);
        if (_dragging) SetValueFromX(HudScale.Logical(eventArgs.X));
    }

    protected override void OnMouseUp(MouseEventArgs eventArgs)
    {
        base.OnMouseUp(eventArgs);
        _dragging = false;
        Capture = false;
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Left or Keys.Right or Keys.Home or Keys.End || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs eventArgs)
    {
        if (eventArgs.KeyCode == Keys.Left) Value -= 5;
        else if (eventArgs.KeyCode == Keys.Right) Value += 5;
        else if (eventArgs.KeyCode == Keys.Home) Value = DesktopSettings.MinimumHudScalePercent;
        else if (eventArgs.KeyCode == Keys.End) Value = DesktopSettings.MaximumHudScalePercent;
        else { base.OnKeyDown(eventArgs); return; }
        eventArgs.Handled = true;
    }

    private void SetValueFromX(int x)
    {
        var trackWidth = Math.Max(1, HudScale.Logical(Width) - 20);
        var ratio = Math.Clamp((x - 10d) / trackWidth, 0d, 1d);
        var raw = DesktopSettings.MinimumHudScalePercent +
                  ratio * (DesktopSettings.MaximumHudScalePercent - DesktopSettings.MinimumHudScalePercent);
        Value = (int)Math.Round(raw);
    }
}

internal sealed class PixelNumberDisplay : Control
{
    private int _minutes;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
        var state = eventArgs.Graphics.Save();
        eventArgs.Graphics.ScaleTransform((float)HudScale.Factor, (float)HudScale.Factor);
        var width = HudScale.Logical(Width);
        var height = HudScale.Logical(Height);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.None;
        using var outer = new Pen(HudColors.Ink, 3f);
        using var border = new Pen(HudColors.Gold, 2f);
        eventArgs.Graphics.DrawRectangle(outer, 1, 1, width - 3, height - 3);
        eventArgs.Graphics.DrawRectangle(border, 3, 3, width - 7, height - 7);
        using var valueFont = PixelArt.CreateFont(12f);
        PixelArt.DrawText(
            eventArgs.Graphics,
            $"{_minutes:0000} MIN",
            valueFont,
            new Rectangle(0, 0, width, height),
            HudColors.Cream,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        eventArgs.Graphics.Restore(state);
    }
}
