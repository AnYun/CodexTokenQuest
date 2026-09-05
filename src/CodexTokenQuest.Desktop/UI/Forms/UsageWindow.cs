using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace CodexTokenQuest.Desktop;

internal sealed class UsageWindow : Window
{
    private DesktopSettings _settings;
    private readonly UsageViewModel _model;
    private readonly DesktopPlatform _platform;
    private readonly HostLifecycle _lifecycle = new();
    private readonly DispatcherTimer _hostTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _refresh = new();
    private readonly TrayIcon _tray;
    private readonly bool _preview;
    private bool _exiting, _permissionNotice;
    private SettingsForm? _options;
    private Window? _permissionWindow;
    private HostBounds? _lastPlacement;
    private HostState? _lastHost;
    private string? _lastPresentation;
    private HeroCanvas? _hero;
    private DailyUsageChart? _chart;
    private TextBlock _status = null!, _reset = null!, _level = null!, _total = null!, _today = null!, _error = null!;
    private ProgressBar? _stamina, _experience;
    private TextBlock? _staminaText, _experienceText;
    private Button _refreshButton = null!;
    private StackPanel? _quotas;
    private readonly List<(TextBlock Label, DateTimeOffset? At)> _quotaResets = [];
    private UsageSnapshot? _rendered;
    private DateTimeOffset? _reportedFetch;
    private string? _reportedError;
    private string? _reportedWarning;
    private TextBlock? _heroName, _heroClass;

    internal UsageWindow(DesktopPlatform platform, UsageViewModel model, bool preview = false, DesktopSettings? settings = null)
    {
        _platform = platform; _model = model; _preview = preview;
        _settings = settings ?? DesktopSettings.Load();
        ShowInTaskbar = false; ShowActivated = false; CanResize = false;
        WindowDecorations = WindowDecorations.None;
        FontFamily = PixelArt.Font; FontSize = 12; FontWeight = FontWeight.Bold;
        _tray = new TrayIcon { Icon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "assets", "icons", "plugin-icon.png")), ToolTipText = UiText.WindowTitle };
        TrayIcon.SetIcons(Application.Current!, new TrayIcons { _tray });
        _tray.Clicked += (_, _) => ToggleVisibility();
        _hostTimer.Tick += (_, _) => TrackHost();
        _clock.Tick += (_, _) => _model.Tick(DateTimeOffset.Now);
        _refresh.Tick += async (_, _) => await _model.RefreshAsync();
        _model.Changed += UpdateValues;
        Opened += (_, _) => { _platform.Windows.Configure(this); if (_lastHost is not null) _platform.Windows.Attach(this, _lastHost); };
        Closing += (_, e) => { if (!_exiting) { e.Cancel = true; _lifecycle.ManuallyHidden = true; Hide(); } };
        Build();
    }

    internal void Start()
    {
        _clock.Start(); _refresh.Start();
        if (_preview) { Position = new PixelPoint(160, 160); Show(); }
        else { _hostTimer.Start(); TrackHost(); }
        _ = _model.RefreshAsync();
    }
    private TextBlock Label(string text, double size = 11, Color? color = null) => new()
    { Text = text, FontSize = size + 1, LineHeight = Math.Ceiling((size + 1) * 1.25), Foreground = PixelArt.Brush(color ?? HudColors.Text), TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
    private Button Button(string text, Action action, Color? color = null)
    {
        var result = new Button { Content = text, Padding = new Thickness(6, 3), MinHeight = 24, FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, Background = PixelArt.Brush(HudColors.Panel),
            Foreground = PixelArt.Brush(color ?? HudColors.Gold), BorderBrush = PixelArt.Brush(color ?? HudColors.Grid), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(0) };
        result.Click += (_, _) => action(); return result;
    }
    private static void Cell(Grid grid, Control child, int row, int column = 0)
    { Grid.SetRow(child, row); Grid.SetColumn(child, column); grid.Children.Add(child); }
    private static PixelFrame Frame(Control child) => new() { Child = child };
    private (ProgressBar Bar, TextBlock Text, Control Control) Bar(string label, Color accent, bool separateValue = false)
    {
        var text = Label(separateValue ? "" : label, 11, separateValue ? HudColors.Text : accent);
        Control heading = text;
        if (separateValue)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            Cell(row, Label(label, 11, HudColors.Text), 0);
            text.TextAlignment = TextAlignment.Right; Cell(row, text, 0, 1); heading = row;
        }
        var bar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 10, Foreground = PixelArt.Brush(accent), Background = PixelArt.Brush(HudColors.Ink), CornerRadius = new CornerRadius(0) };
        return (bar, text, new StackPanel { Spacing = 3, Children = { heading, bar } });
    }
    private void Build()
    {
        _hero?.Dispose(); _hero = null; _chart = null; _quotas = null; _stamina = null; _experience = null;
        _heroName = _heroClass = null;
        _staminaText = _experienceText = null; _quotaResets.Clear(); _rendered = null;
        UiText.SetLanguage(_settings.Language); HudColors.SetTheme((HudTheme)_settings.ThemeIndex);
        Title = UiText.WindowTitle; Opacity = _settings.OpacityPercent / 100d;
        Background = PixelArt.Brush(HudColors.Background);
        var compact = _settings.MinimizedMode;
        var height = compact ? 160 : _settings.SelectedPanel == "CAMP" ? 350 : 382;
        var root = new Grid { Width = 392, Height = height, RowDefinitions = new RowDefinitions(compact ? "36,*,24" : "36,32,*,24"), Margin = new Thickness(10) };
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,62,30,30,30") };
        var title = new StackPanel { Spacing = 3 };
        title.Children.Add(Label(HudCopy.Brand, 11, HudColors.Gold));
        _status = Label("", 9, HudColors.Muted); title.Children.Add(_status); Cell(header, title, 0);
        var theme = Button(new[] { "A PIX", "B GLS", "C GUILD", "D TERM" }[_settings.ThemeIndex], CycleTheme, HudColors.Cyan); Cell(header, theme, 0, 1);
        Cell(header, Button(compact ? "□" : "—", () => Change(_settings with { MinimizedMode = !compact }), HudColors.Amber), 0, 2);
        _refreshButton = Button("↻", () => _ = _model.RefreshAsync(), HudColors.Green); Cell(header, _refreshButton, 0, 3);
        Cell(header, Button("×", () => { _lifecycle.ManuallyHidden = true; Hide(); }, HudColors.Red), 0, 4);
        foreach (var button in header.Children.OfType<Button>()) { button.VerticalAlignment = VerticalAlignment.Top; button.Height = 25; }
        Cell(root, header, 0);
        var bodyRow = compact ? 1 : 2;
        if (!compact)
        {
            var tabs = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*"), Margin = new Thickness(0, 2, 0, 3) };
            var labels = HudCopy.Tabs; var names = new[] { "CAMP", "QUESTS", "HISTORY" }; var display = new[] { labels.Camp, labels.Quests, labels.History };
            for (var i = 0; i < 3; i++) { var name = names[i]; Cell(tabs, Button(display[i], () => SelectPanel(name), _settings.SelectedPanel == name ? HudColors.Gold : HudColors.Muted), 0, i); }
            Cell(root, tabs, 1);
        }
        _reset = Label("", 10, HudColors.Cyan); _reset.Name = "NextReset"; _reset.Margin = new Thickness(6, 3);
        _error = Label("", 10, HudColors.Red); _error.Name = "UsageNotice";
        _error.TextWrapping = TextWrapping.NoWrap; _error.MaxHeight = 16; _error.ClipToBounds = true;
        var body = new Grid { RowDefinitions = new RowDefinitions("*,26,Auto"), Margin = new Thickness(0, 5, 0, 4) };
        Cell(body, Frame(_reset), 1); Cell(body, _error, 2);
        if (compact)
        {
            var stamina = Bar(HudCopy.Stamina, HudColors.Green); _stamina = stamina.Bar; _staminaText = stamina.Text;
            stamina.Control.Margin = new Thickness(8); Cell(body, Frame(stamina.Control), 0);
        }
        else if (_settings.SelectedPanel == "CAMP")
        {
            var camp = new Grid { ColumnDefinitions = new ColumnDefinitions("164,6,*"), Margin = new Thickness(0, 0, 0, 5) };
            _hero = new HeroCanvas { CharacterIndex = _settings.CharacterIndex };
            _hero.Selected += index => Change(_settings with { CharacterIndex = index });
            ToolTip.SetTip(_hero, UiText.Pick("Click left/right to choose a hero. Unlocks: LV 1, 10, 25, 50.", "點擊左右切換英雄，解鎖等級：1、10、25、50。"));
            var portrait = new Grid(); portrait.Children.Add(_hero);
            var heroLabels = new StackPanel { Spacing = 3, Margin = new Thickness(10, 0, 10, 10), VerticalAlignment = VerticalAlignment.Bottom, IsHitTestVisible = false };
            _heroName = Label("", 12, HudColors.Gold); _heroClass = Label("", 10, HudColors.Cream);
            _heroName.TextAlignment = _heroClass.TextAlignment = TextAlignment.Center;
            heroLabels.Children.Add(_heroName); heroLabels.Children.Add(_heroClass); portrait.Children.Add(heroLabels); Cell(camp, portrait, 0);
            var stats = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto,*,Auto,*,Auto,*,Auto"), Margin = new Thickness(10) };
            var identity = new StackPanel { Spacing = 2 };
            _level = Label("", 23, HudColors.Gold); identity.Children.Add(_level); identity.Children.Add(Label(HudCopy.StatusTitle, 9, HudColors.Muted)); Cell(stats, identity, 0);
            var stamina = Bar(HudCopy.Stamina, HudColors.Green, true); _stamina = stamina.Bar; _staminaText = stamina.Text; Cell(stats, stamina.Control, 2);
            var xp = Bar(HudCopy.Experience, HudColors.Cyan, true); _experience = xp.Bar; _experienceText = xp.Text; Cell(stats, xp.Control, 4);
            var lifetime = new StackPanel { Spacing = 2 }; lifetime.Children.Add(Label(HudCopy.Lifetime, 9, HudColors.Muted));
            _total = Label("", 15, HudColors.Cream); lifetime.Children.Add(_total); Cell(stats, lifetime, 6);
            var today = new StackPanel { Spacing = 2 }; today.Children.Add(Label(HudCopy.Today, 9, HudColors.Muted));
            _today = Label("", 15, HudColors.Gold); _today.Name = "TodayTokens"; today.Children.Add(_today); Cell(stats, today, 8);
            Cell(camp, Frame(stats), 0, 2); Cell(body, camp, 0);
        }
        else if (_settings.SelectedPanel == "QUESTS")
        {
            _quotas = new StackPanel { Spacing = 6 };
            Cell(body, new ScrollViewer { Content = _quotas, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled }, 0);
        }
        else { _chart = new DailyUsageChart(); Cell(body, _chart, 0); }
        Cell(root, body, bodyRow);
        var footer = Button(HudCopy.Footer(_settings.RefreshMinutes), ShowSettings, HudColors.Muted); footer.Name = "HudFooter";
        footer.HorizontalAlignment = HorizontalAlignment.Stretch; footer.FontSize = 11; Cell(root, footer, bodyRow + 1);
        Content = new Viewbox { Stretch = Stretch.Fill, Child = Frame(root) };
        Width = 412 * _settings.HudScale; Height = (height + 20) * _settings.HudScale;
        _refresh.Interval = TimeSpan.FromMinutes(_settings.RefreshMinutes);
        BuildTray(); UpdateValues(); _options?.ApplyTheme(); _lastPlacement = null;
        if (!_preview) TrackHost();
    }
    private void UpdateValues()
    {
        if (!_preview && _reportedFetch != _model.LastFetched && _model.Snapshot is { } fresh)
        { _reportedFetch = fresh.FetchedAt; AppPaths.Log($"Usage refreshed. Quota windows={fresh.RateLimits.Count}; daily buckets={fresh.DailyUsage.Count}."); }
        if (!_preview && _reportedError != _model.Error)
        { _reportedError = _model.Error; if (_reportedError is not null) AppPaths.Log($"Usage refresh failed: {_reportedError}"); }
        if (!_preview && _reportedWarning != _model.Snapshot?.Warning)
        { _reportedWarning = _model.Snapshot?.Warning; if (_reportedWarning is not null) AppPaths.Log($"Partial usage: {_reportedWarning}"); }
        _status.Text = _preview ? "PREVIEW / 預覽 · SAMPLE DATA" : _model.Refreshing ? HudCopy.Loading : _model.Error is not null ? HudCopy.Lost
            : _model.LastFetched is { } fetched ? HudCopy.Ready(fetched.ToLocalTime()) : HudCopy.Loading;
        _status.Foreground = PixelArt.Brush(_model.Refreshing ? HudColors.Amber : _model.Error is not null ? HudColors.Red
            : _model.LastFetched is not null ? HudColors.Green : HudColors.Amber);
        _refreshButton.IsEnabled = !_model.Refreshing;
        _error.Text = _model.Notice ?? "";
        _error.Foreground = PixelArt.Brush(_model.Error is null ? HudColors.Amber : HudColors.Red);
        _error.IsVisible = !string.IsNullOrEmpty(_error.Text);
        // Raw protocol errors can contain thousands of characters. Keep them in
        // lifecycle.log, and use only bounded, localized text in the HUD and tooltip.
        ToolTip.SetTip(_error, _model.Notice); ToolTip.SetTip(_status, _model.Notice);
        _reset.Text = _model.ResetText(_model.Weekly?.ResetsAt);
        _reset.Foreground = PixelArt.Brush(ResetColor(_model.Weekly?.ResetsAt));
        if (_stamina is not null) _stamina.Value = (double)(_model.Stamina ?? 0);
        if (_staminaText is not null) _staminaText.Text = (_hero is null ? $"{HudCopy.Stamina}  " : "")
            + (_model.Stamina is { } s ? $"{s:0.#} / 100" : "--");
        if (_experience is not null) _experience.Value = (double)_model.Progress(_settings.ExperienceBase);
        if (_experienceText is not null) _experienceText.Text = _model.LifetimeTokens is null
            ? "--" : $"{_model.Progress(_settings.ExperienceBase):0.#}%";
        if (_hero is not null)
        {
            var level = _model.Level(_settings.ExperienceBase);
            if (_model.LifetimeTokens is not null && Characters.All[_settings.CharacterIndex].UnlockLevel > level)
            {
                _settings = _settings with { CharacterIndex = Array.FindLastIndex(Characters.All, c => c.UnlockLevel <= level) };
                try { SaveSettings(); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { AppPaths.Log($"Settings: {e.Message}"); }
            }
            _hero.Level = level; _hero.CharacterIndex = _settings.CharacterIndex; _hero.InvalidateVisual();
            var heroCopy = HudCopy.Hero(Characters.All[_settings.CharacterIndex]);
            _heroName!.Text = heroCopy.Name; _heroClass!.Text = heroCopy.Class;
            _level.Text = _model.LifetimeTokens is null ? $"{UiText.Level}--" : $"{UiText.Level}{level:00}";
            _total.Text = PixelArt.Number(_model.LifetimeTokens); _today.Text = $"+{PixelArt.Number(_model.TodayTokens)}";
        }
        if (_chart is not null) { _chart.Data = _model.History; _chart.InvalidateVisual(); }
        if (_quotas is not null && _rendered != _model.Snapshot)
        {
            _rendered = _model.Snapshot; _quotas.Children.Clear(); _quotaResets.Clear();
            _quotas.Children.Add(Label(HudCopy.QuestTitle, 10, HudColors.Gold));
            if (_model.Snapshot is { } snapshot)
            {
                _quotas.Children.Add(Label(UiText.Pick("Available resets: ", "可用重設次數：") + (snapshot.AvailableResetCredits?.ToString() ?? "--")));
                foreach (var bucket in snapshot.RateLimits.OrderBy(x => x.Id.Equals("codex", StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenByDescending(x => x.WindowDurationMinutes))
                {
                    var stack = new StackPanel { Margin = new Thickness(10), Spacing = 5 };
                    stack.Children.Add(Label($"{bucket.Name ?? bucket.Id} · {UiText.WindowLabel(bucket.Window)}", 11, HudColors.Gold));
                    var bar = Bar($"{UiText.Pick("Used", "已用")} {bucket.UsedPercent:0.#}% · {UiText.Pick("Left", "剩餘")} {bucket.RemainingPercent:0.#}%", HudColors.Green);
                    bar.Bar.Value = (double)bucket.RemainingPercent; stack.Children.Add(bar.Control);
                    var reset = Label(_model.ResetText(bucket.ResetsAt, next: false), 10, ResetColor(bucket.ResetsAt)); stack.Children.Add(reset); _quotaResets.Add((reset, bucket.ResetsAt));
                    _quotas.Children.Add(Frame(stack));
                }
            }
        }
        foreach (var reset in _quotaResets)
        {
            reset.Label.Text = _model.ResetText(reset.At, next: false);
            reset.Label.Foreground = PixelArt.Brush(ResetColor(reset.At));
        }
        _tray.ToolTipText = _model.Error is null ? $"{UiText.WindowTitle} · {UiText.Level}{_model.Level(_settings.ExperienceBase)}" : UiText.TrayReadFailed;
    }
    private Color ResetColor(DateTimeOffset? reset) => reset is null ? HudColors.Muted
        : reset.Value - _model.Now <= TimeSpan.FromHours(12) ? HudColors.Amber : HudColors.Cyan;
    private void SaveSettings() { if (!_preview) _settings.Save(); }
    private void Change(DesktopSettings settings)
    {
        try { var normalized = settings.Normalize(); if (!_preview) normalized.Save(); _settings = normalized; Build(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { AppPaths.Log($"Settings: {e.Message}"); _error.Text = UiText.Pick("Settings could not be saved. Retry.", "設定無法儲存，請重試。"); _error.IsVisible = true; }
    }
    private void SelectPanel(string panel) => Change(_settings with { SelectedPanel = panel, MinimizedMode = false });
    private void CycleTheme() => Change(_settings with { ThemeIndex = (_settings.ThemeIndex + 1) % 4 });
    private void BuildTray()
    {
        // Keep the exported menu identity stable when settings rebuild the HUD.
        // The macOS native exporter cannot replace an already attached menu.
        var menu = _tray.Menu ?? new NativeMenu();
        menu.Items.Clear();
        void Add(string name, Action action) { var item = new NativeMenuItem(name); item.Click += (_, _) => action(); menu.Items.Add(item); }
        Add(UiText.TrayToggle, ToggleVisibility); Add(UiText.TrayCamp, () => SelectPanel("CAMP")); Add(UiText.TrayQuests, () => SelectPanel("QUESTS")); Add(UiText.TrayHistory, () => SelectPanel("HISTORY"));
        Add(UiText.TrayTheme, CycleTheme); Add(UiText.TrayRefresh, () => _ = _model.RefreshAsync()); Add(UiText.TrayOptions, ShowSettings);
        if (OperatingSystem.IsMacOS()) Add(UiText.Pick("Accessibility permission…", "輔助使用權限…"), ShowPermission);
        menu.Items.Add(new NativeMenuItemSeparator()); Add(UiText.TrayExit, () => _ = ExitAsync());
        if (_tray.Menu is null) _tray.Menu = menu;
    }
    private void ToggleVisibility()
    {
        _lifecycle.ManuallyHidden = IsVisible;
        if (IsVisible) Hide(); else if (_preview) Show(); else TrackHost();
    }
    private void ShowSettings()
    {
        if (_options is not null) { _options.Activate(); return; }
        _options = new SettingsForm(_settings, settings =>
        {
            var normalized = (settings with { ThemeIndex = _settings.ThemeIndex, SelectedPanel = _settings.SelectedPanel,
                CharacterIndex = _settings.CharacterIndex, MinimizedMode = _settings.MinimizedMode }).Normalize();
            if (!_preview) normalized.Save(); _settings = normalized; Build();
        });
        _options.Closed += (_, _) => _options = null; _options.Show(); _options.Activate();
    }
    private void ShowPermission()
    {
        if (_permissionWindow is not null) { _permissionWindow.Activate(); return; }
        _permissionWindow = new Window { Title = UiText.WindowTitle, Width = 470, Height = 240, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterScreen };
        var content = new StackPanel { Margin = new Thickness(24), Spacing = 18 };
        content.Children.Add(new TextBlock { Text = UiText.Pick("Allow Accessibility for Codex Token Quest to follow the Codex window. The HUD waits in the menu bar until permission is granted. If macOS requests a restart, quit and reopen the HUD.", "請允許 Codex Token Quest 使用「輔助使用」權限，以跟隨 Codex 視窗。授權前會保留選單列入口；若 macOS 要求重新啟動，請結束並重新開啟 HUD。"), TextWrapping = TextWrapping.Wrap });
        var open = new Button { Content = UiText.Pick("Open Accessibility Settings", "開啟輔助使用設定") }; open.Click += (_, _) => _platform.Tracker.RequestPermission(); content.Children.Add(open);
        _permissionWindow.Content = content; _permissionWindow.Closed += (_, _) => _permissionWindow = null; _permissionWindow.Show();
    }
    private void TrackHost()
    {
        if (_preview || _exiting) return;
        HostState state;
        try { state = _platform.Tracker.Read(); }
        catch (Exception e) { AppPaths.Log($"Tracking failed: {e.Message}"); state = new(true, true, false, Reliable: false); }
        if (_lastHost != state)
            AppPaths.Log($"Host: running={state.Running} permission={state.PermissionGranted} foreground={state.Foreground} reliable={state.Reliable} bounds={state.Bounds} scale={state.Scale}");
        _lastHost = state;
        if (!state.PermissionGranted && !_permissionNotice)
        { _permissionNotice = true; ShowPermission(); }
        if (state.PermissionGranted && _permissionWindow is not null) _permissionWindow.Close();
        var action = _lifecycle.Update(state, DateTimeOffset.Now);
        if (action == HostAction.Exit) { _ = ExitAsync(); return; }
        if (action == HostAction.Hide) { if (IsVisible) Hide(); _lastPlacement = null; return; }
        var desiredHeight = (_settings.MinimizedMode ? 180 : _settings.SelectedPanel == "CAMP" ? 370 : 402) * _settings.HudScale;
        var desiredWidth = 412 * _settings.HudScale;
        var placement = HostLifecycle.Place(state.Bounds!.Value, desiredWidth * state.Scale, desiredHeight * state.Scale, _settings.Margin * state.Scale);
        var position = new PixelPoint((int)Math.Round(placement.X), (int)Math.Round(placement.Y));
        var width = placement.Width / state.Scale;
        var height = placement.Height / state.Scale;
        if (_lastPlacement != placement || Position != position || Math.Abs(Width - width) > 0.5 || Math.Abs(Height - height) > 0.5)
        {
            Width = width; Height = height; Position = position; _lastPlacement = placement;
        }
        if (!IsVisible)
        {
            Show();
            // Native resize notifications during first show can replace the requested
            // dimensions with zero, especially when changing Mac displays. Restore
            // them after the native window exists, and check actual bounds on each tick.
            Width = width; Height = height; Position = position; UpdateLayout();
        }
        _platform.Windows.Configure(this); _platform.Windows.Attach(this, state);
        var presentation = $"HUD: visible={IsVisible} position={Position} size={Width}x{Height} client={ClientSize} scale={RenderScaling} handle={TryGetPlatformHandle()?.HandleDescriptor}";
        if (_lastPresentation != presentation) { _lastPresentation = presentation; AppPaths.Log(presentation); }
    }
    internal async Task ExitAsync()
    {
        if (_exiting) return; _exiting = true;
        _hostTimer.Stop(); _clock.Stop(); _refresh.Stop(); _model.Changed -= UpdateValues;
        _options?.Close(); _permissionWindow?.Close(); Hide(); _tray.Dispose(); _hero?.Dispose();
        _platform.Tracker.Dispose(); await _model.DisposeAsync(); Close();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.Shutdown();
    }
}
