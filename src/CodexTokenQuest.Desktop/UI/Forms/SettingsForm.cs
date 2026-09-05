using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace CodexTokenQuest.Desktop;

internal sealed class SettingsForm : Window
{
    private readonly List<Action> _themeUpdates = [];
    private readonly PixelFrame _frame;

    internal SettingsForm(DesktopSettings settings, Action<DesktopSettings> save)
    {
        Title = UiText.GameOptions; Width = 480; Height = 620; MinWidth = 380; MinHeight = 420;
        WindowDecorations = WindowDecorations.None; ShowInTaskbar = false; Topmost = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = PixelArt.Font; FontSize = 14; FontWeight = FontWeight.Bold;
        TextBlock Label(string text, Func<Color> color, double size = 14)
        {
            var label = new TextBlock { Text = text, FontSize = size, LineHeight = Math.Ceiling(size * 1.25), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
            _themeUpdates.Add(() => label.Foreground = PixelArt.Brush(color())); return label;
        }
        Button Button(string text, Action action, Func<Color> accent)
        {
            var button = new Button { Content = text, CornerRadius = new CornerRadius(0), BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4), MinHeight = 32, HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
            button.Click += (_, _) => action();
            _themeUpdates.Add(() =>
            {
                button.Background = PixelArt.Brush(HudColors.Panel); button.Foreground = button.BorderBrush = PixelArt.Brush(accent());
                button.Resources["ButtonBackgroundPointerOver"] = PixelArt.Brush(HudColors.PanelBright);
                button.Resources["ButtonBackgroundPressed"] = PixelArt.Brush(HudColors.Theme == HudTheme.GuildLedger ? HudColors.PanelBright : HudColors.Ink);
                foreach (var state in new[] { "PointerOver", "Pressed" })
                {
                    button.Resources["ButtonForeground" + state] = PixelArt.Brush(state == "Pressed" ? HudColors.Cyan : accent());
                    button.Resources["ButtonBorderBrush" + state] = PixelArt.Brush(accent());
                }
            });
            return button;
        }
        var root = new Grid { RowDefinitions = new RowDefinitions("44,*,Auto,42"), Margin = new Thickness(18) };
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,36"), Margin = new Thickness(0, 0, 0, 8) };
        var title = Label(UiText.GameOptions, () => HudColors.Gold, 21);
        title.PointerPressed += (_, e) => { if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e); };
        header.Children.Add(title);
        var close = Button("×", Close, () => HudColors.Red); Grid.SetColumn(close, 1); header.Children.Add(close); root.Children.Add(header);
        var panel = new StackPanel { Spacing = 12, Margin = new Thickness(0, 4, 8, 12) };
        NumericUpDown Number(string name, string label, decimal value, decimal minimum, decimal maximum, decimal increment = 1, bool multiply = false)
        {
            var section = new StackPanel { Spacing = 6 };
            section.Children.Add(Label(label, () => HudColors.Cyan));
            var input = new NumericUpDown { Name = name, Value = value, Minimum = minimum, Maximum = maximum, Increment = increment,
                FormatString = "0", ShowButtonSpinner = false, CornerRadius = new CornerRadius(0), BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center, MinWidth = 0, MinHeight = 34 };
            _themeUpdates.Add(() => { input.Background = PixelArt.Brush(HudColors.PanelBright); input.Foreground = PixelArt.Brush(HudColors.Text); input.BorderBrush = PixelArt.Brush(HudColors.Grid); });
            void Adjust(bool increase)
            {
                var current = input.Value ?? value;
                input.Value = Math.Clamp(multiply ? increase ? current * 10 : current / 10 : current + (increase ? increment : -increment), minimum, maximum);
            }
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("52,*,52"), ColumnSpacing = 6 };
            row.Children.Add(Button(multiply ? "/10" : "−", () => Adjust(false), () => HudColors.Amber));
            Grid.SetColumn(input, 1); row.Children.Add(input);
            var plus = Button(multiply ? "×10" : "+", () => Adjust(true), () => HudColors.Cyan); Grid.SetColumn(plus, 2); row.Children.Add(plus);
            section.Children.Add(row); panel.Children.Add(section); return input;
        }
        var refresh = Number("RefreshMinutes", $"{UiText.RefreshInterval} (1–1440 min)", settings.RefreshMinutes, 1, 1440);
        var quick = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*"), ColumnSpacing = 6 };
        var presets = new[] { 1, 5, 15, 30, 60 };
        for (var i = 0; i < presets.Length; i++)
        {
            var minutes = presets[i]; var button = Button($"{minutes}m", () => refresh.Value = minutes, () => HudColors.Green);
            Grid.SetColumn(button, i); quick.Children.Add(button);
        }
        panel.Children.Add(quick);
        var scale = Number("HudScalePercent", $"{UiText.HudSize} (50–300%)", settings.HudScalePercent, 50, 300, 10);
        var margin = Number("HudMargin", $"{UiText.HudMargin} (0–100)", settings.Margin, 0, 100);
        var xp = Number("ExperienceBase", $"{UiText.ExperienceBase} (1K–1T)", settings.ExperienceBase, 1000, 1_000_000_000_000, 1000, multiply: true);
        panel.Children.Add(Label(UiText.ExperienceBaseDescription, () => HudColors.Muted, 12));
        var opacity = Number("OpacityPercent", $"{UiText.HudOpacity} (20–100%)", settings.OpacityPercent, 20, 100, 5);
        panel.Children.Add(Label(UiText.Pick("LANGUAGE", "語言"), () => HudColors.Cyan));
        var languageCode = settings.Language;
        Button language = null!;
        language = Button(languageCode == UiText.TraditionalChinese ? "正體中文" : "English", () =>
        {
            languageCode = languageCode == UiText.TraditionalChinese ? UiText.English : UiText.TraditionalChinese;
            language.Content = languageCode == UiText.TraditionalChinese ? "正體中文" : "English";
        }, () => HudColors.Cyan);
        language.Name = "SettingsLanguage"; panel.Children.Add(language);
        var scroll = new ScrollViewer { Content = panel, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        Grid.SetRow(scroll, 1); root.Children.Add(scroll);
        var error = Label("", () => HudColors.Red, 12); error.IsVisible = false; error.Margin = new Thickness(0, 4);
        Grid.SetRow(error, 2); root.Children.Add(error);
        void Error(string message) { error.Text = message; error.IsVisible = true; }
        var buttons = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*"), ColumnSpacing = 8, Margin = new Thickness(0, 8, 0, 0) };
        var cancel = Button(UiText.Cancel, Close, () => HudColors.Red); Grid.SetColumn(cancel, 1); buttons.Children.Add(cancel);
        var apply = Button(UiText.Save, () =>
        {
            if (new[] { refresh, scale, margin, xp, opacity }.Any(input => input.Value is null))
            { Error(UiText.Pick("Enter a value in every field.", "請填入所有欄位的數值。")); return; }
            try
            {
                save(settings with { RefreshMinutes = (int)refresh.Value!.Value, HudScalePercent = (int)scale.Value!.Value,
                    Margin = (int)margin.Value!.Value, ExperienceBase = (long)xp.Value!.Value, OpacityPercent = (int)opacity.Value!.Value, Language = languageCode });
                Close();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Error(e.Message); }
        }, () => HudColors.Green);
        apply.Name = "SaveSettings"; Grid.SetColumn(apply, 2); buttons.Children.Add(apply); Grid.SetRow(buttons, 3); root.Children.Add(buttons);
        KeyDown += (_, e) => { if (e.Key == Key.Escape) { Close(); e.Handled = true; } };
        _frame = new PixelFrame { Child = root }; Content = _frame; ApplyTheme();
    }

    internal void ApplyTheme()
    {
        RequestedThemeVariant = HudColors.Theme == HudTheme.GuildLedger ? Avalonia.Styling.ThemeVariant.Light : Avalonia.Styling.ThemeVariant.Dark;
        Background = PixelArt.Brush(HudColors.Background); Foreground = PixelArt.Brush(HudColors.Text);
        Resources["ControlCornerRadius"] = new CornerRadius(0);
        foreach (var state in new[] { "", "PointerOver", "Focused" })
        {
            Resources["TextControlBackground" + state] = PixelArt.Brush(HudColors.PanelBright);
            Resources["TextControlForeground" + state] = PixelArt.Brush(HudColors.Text);
            Resources["TextControlBorderBrush" + state] = PixelArt.Brush(HudColors.Cyan);
        }
        foreach (var update in _themeUpdates) update();
        _frame.InvalidateVisual();
    }
}
