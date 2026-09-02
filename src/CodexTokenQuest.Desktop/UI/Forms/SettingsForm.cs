using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace CodexTokenQuest.Desktop;

internal sealed class SettingsForm : Form
{
    private readonly PixelNumberDisplay _minutesDisplay;
    private readonly PixelScaleBar _scaleBar;
    private readonly Label _scaleValue;
    private readonly Label _marginValue;
    private readonly Label _experienceBaseValue;
    private readonly PixelScaleBar _opacityBar;
    private readonly Label _opacityValue;
    private int _refreshMinutes;
    private int _hudScalePercent;
    private int _margin;
    private long _experienceBase;
    private int _opacityPercent;
    private string _language;

    internal int RefreshMinutes => _refreshMinutes;
    internal int HudScalePercent => _hudScalePercent;
    internal int HudMargin => _margin;
    internal long ExperienceBase => _experienceBase;
    internal int OpacityPercent => _opacityPercent;
    internal string Language => _language;

    internal SettingsForm(int currentMinutes, int currentHudScalePercent, int currentMargin, long currentExperienceBase, int currentOpacityPercent, string currentLanguage)
    {
        _refreshMinutes = Math.Clamp(currentMinutes, DesktopSettings.MinimumRefreshMinutes, DesktopSettings.MaximumRefreshMinutes);
        _hudScalePercent = Math.Clamp(currentHudScalePercent, DesktopSettings.MinimumHudScalePercent, DesktopSettings.MaximumHudScalePercent);
        _margin = Math.Clamp(currentMargin, DesktopSettings.MinimumMargin, DesktopSettings.MaximumMargin);
        _experienceBase = Math.Clamp(currentExperienceBase, DesktopSettings.MinimumExperienceBase, DesktopSettings.MaximumExperienceBase);
        _opacityPercent = Math.Clamp(currentOpacityPercent, DesktopSettings.MinimumOpacityPercent, DesktopSettings.MaximumOpacityPercent);
        _language = UiText.NormalizeLanguage(currentLanguage);

        Text = $"{UiText.WindowTitle} - {UiText.Pick("Options", "選項")}";
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(HudScale.Px(360), HudScale.Px(530));
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
            Text = UiText.GameOptions,
            Font = PixelArt.CreateHudFont(11f),
            ForeColor = HudColors.Gold,
            BackColor = Color.Transparent,
            Location = new Point(HudScale.Px(18), HudScale.Px(15)),
            Size = new Size(HudScale.Px(200), HudScale.Px(22))
        };
        var language = CreatePixelButton(UiText.LanguageButton(_language), HudColors.Cyan, 222, 11, 86, 27);
        language.Font = PixelArt.CreateHudFont(7f);
        language.Click += (_, _) =>
        {
            _language = _language == UiText.English ? UiText.TraditionalChinese : UiText.English;
            language.Text = UiText.LanguageButton(_language);
        };
        var close = CreatePixelButton("×", HudColors.Red, 316, 11, 28, 27);
        close.DialogResult = DialogResult.Cancel;

        var section = new Label
        {
            Text = UiText.RefreshInterval,
            Font = PixelArt.CreateHudFont(8.5f),
            ForeColor = HudColors.Cyan,
            BackColor = Color.Transparent,
            Location = new Point(HudScale.Px(20), HudScale.Px(55)),
            Size = new Size(HudScale.Px(200), HudScale.Px(18))
        };
        var description = new Label
        {
            Text = UiText.RefreshDescription,
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
            Text = UiText.QuickSlots,
            ForeColor = HudColors.Muted,
            BackColor = Color.Transparent,
            Location = new Point(HudScale.Px(20), HudScale.Px(148)),
            Size = new Size(HudScale.Px(120), HudScale.Px(15))
        };
        var quickButtons = new List<Button>();
        var quickValues = new[] { 1, 5, 15, 60 };
        for (var index = 0; index < quickValues.Length; index++)
        {
            var value = quickValues[index];
            var quick = CreatePixelButton($"{value}M", HudColors.Green, 20 + index * 81, 166, 74, 31);
            quick.Click += (_, _) => SetMinutes(value);
            quickButtons.Add(quick);
        }

        var sizeSection = new Label
        {
            Text = UiText.HudSize,
            Font = PixelArt.CreateHudFont(8.5f),
            ForeColor = HudColors.Cyan,
            BackColor = Color.Transparent,
            Location = new Point(HudScale.Px(20), HudScale.Px(207)),
            Size = new Size(HudScale.Px(120), HudScale.Px(18))
        };
        var sizeDescription = new Label
        {
            Text = UiText.HudSizeDescription,
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
            Text = UiText.HudMargin,
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

        var experienceSection = new Label
        {
            Text = UiText.ExperienceBase,
            Font = PixelArt.CreateHudFont(8.5f),
            ForeColor = HudColors.Cyan,
            BackColor = Color.Transparent,
            Location = new Point(HudScale.Px(20), HudScale.Px(318)),
            Size = new Size(HudScale.Px(200), HudScale.Px(18))
        };
        var experienceDescription = new Label
        {
            Text = UiText.ExperienceBaseDescription,
            ForeColor = HudColors.Muted,
            BackColor = Color.Transparent,
            Location = new Point(HudScale.Px(20), HudScale.Px(337)),
            Size = new Size(HudScale.Px(320), HudScale.Px(17))
        };
        var experienceDivide = CreatePixelButton("/10", HudColors.Cyan, 20, 358, 55, 31);
        _experienceBaseValue = new Label
        {
            Text = FormatExperienceBase(),
            Font = PixelArt.CreateHudFont(8f),
            ForeColor = HudColors.Gold,
            BackColor = HudColors.Panel,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(HudScale.Px(79), HudScale.Px(358)),
            Size = new Size(HudScale.Px(178), HudScale.Px(31))
        };
        var experienceMultiply = CreatePixelButton("X10", HudColors.Cyan, 261, 358, 79, 31);
        experienceDivide.Click += (_, _) => AdjustExperienceBase(-1);
        experienceMultiply.Click += (_, _) => AdjustExperienceBase(1);

        var opacitySection = new Label
        {
            Text = UiText.HudOpacity,
            Font = PixelArt.CreateHudFont(8.5f),
            ForeColor = HudColors.Cyan,
            BackColor = Color.Transparent,
            Location = new Point(HudScale.Px(20), HudScale.Px(400)),
            Size = new Size(HudScale.Px(200), HudScale.Px(18))
        };
        var opacityDescription = new Label
        {
            Text = UiText.HudOpacityDescription,
            ForeColor = HudColors.Muted,
            BackColor = Color.Transparent,
            Location = new Point(HudScale.Px(20), HudScale.Px(419)),
            Size = new Size(HudScale.Px(320), HudScale.Px(17))
        };
        _opacityBar = new PixelScaleBar
        {
            Location = new Point(HudScale.Px(20), HudScale.Px(439)),
            Size = new Size(HudScale.Px(242), HudScale.Px(27)),
            Minimum = DesktopSettings.MinimumOpacityPercent,
            Maximum = DesktopSettings.MaximumOpacityPercent,
            Value = _opacityPercent
        };
        _opacityValue = new Label
        {
            Text = $"{_opacityPercent}%",
            Font = PixelArt.CreateHudFont(8f),
            ForeColor = HudColors.Gold,
            BackColor = HudColors.Panel,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(HudScale.Px(270), HudScale.Px(439)),
            Size = new Size(HudScale.Px(70), HudScale.Px(27))
        };
        _opacityBar.ValueChanged += (_, _) => SetOpacity(_opacityBar.Value);

        var content = new Panel
        {
            Location = new Point(HudScale.Px(17), HudScale.Px(50)),
            Size = new Size(HudScale.Px(326), HudScale.Px(427)),
            BackColor = HudColors.Background
        };
        var settingsContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = HudColors.Background
        };
        var settingsControls = new List<Control>
        {
            section, description, minusFive, minusOne, _minutesDisplay, plusOne, plusFive, quickLabel,
            sizeSection, sizeDescription, _scaleBar, _scaleValue, marginSection, marginMinus, _marginValue,
            marginPlus, experienceSection, experienceDescription, experienceDivide, _experienceBaseValue,
            experienceMultiply, opacitySection, opacityDescription, _opacityBar, _opacityValue
        };
        settingsControls.AddRange(quickButtons);
        foreach (var control in settingsControls)
        {
            control.Left -= HudScale.Px(17);
            control.Top -= HudScale.Px(50);
            settingsContent.Controls.Add(control);
        }
        content.Controls.Add(settingsContent);

        var cancel = CreatePixelButton(UiText.Cancel, HudColors.Red, 176, 486, 78, 28);
        cancel.DialogResult = DialogResult.Cancel;
        var save = CreatePixelButton(UiText.Save, HudColors.Green, 264, 486, 78, 28);
        save.DialogResult = DialogResult.OK;

        AcceptButton = save;
        CancelButton = cancel;
        Controls.AddRange([title, language, close, content, cancel, save]);

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
        eventArgs.Graphics.DrawLine(divider, 18, 482, width - 18, 482);
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

    private void AdjustExperienceBase(int direction)
    {
        _experienceBase = direction < 0
            ? Math.Max(DesktopSettings.MinimumExperienceBase, _experienceBase / 10)
            : Math.Min(DesktopSettings.MaximumExperienceBase, _experienceBase * 10);
        _experienceBaseValue.Text = FormatExperienceBase();
    }

    private string FormatExperienceBase() => $"{PixelArt.FormatNumber(_experienceBase)} {UiText.Tokens}";

    private void SetOpacity(int percent)
    {
        _opacityPercent = Math.Clamp(percent, DesktopSettings.MinimumOpacityPercent, DesktopSettings.MaximumOpacityPercent);
        _opacityValue.Text = $"{_opacityPercent}%";
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
    private int _minimum = DesktopSettings.MinimumHudScalePercent;
    private int _maximum = DesktopSettings.MaximumHudScalePercent;
    private bool _dragging;

    internal event EventHandler? ValueChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum < _minimum) _maximum = _minimum;
            Value = _value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(value, _minimum);
            Value = _value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int Value
    {
        get => _value;
        set
        {
            var next = Math.Clamp(value, _minimum, _maximum);
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
        var progress = (_value - _minimum) * 100m / Math.Max(1, _maximum - _minimum);
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
        else if (eventArgs.KeyCode == Keys.Home) Value = _minimum;
        else if (eventArgs.KeyCode == Keys.End) Value = _maximum;
        else { base.OnKeyDown(eventArgs); return; }
        eventArgs.Handled = true;
    }

    private void SetValueFromX(int x)
    {
        var trackWidth = Math.Max(1, HudScale.Logical(Width) - 20);
        var ratio = Math.Clamp((x - 10d) / trackWidth, 0d, 1d);
        var raw = _minimum + ratio * (_maximum - _minimum);
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
