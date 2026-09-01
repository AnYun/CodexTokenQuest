namespace CodexTokenQuest.Desktop;

internal sealed class UsageWindow : Form
{
    private static readonly IReadOnlyDictionary<string, Size> PanelSizes = new Dictionary<string, Size>
    {
        ["CAMP"] = new(392, 350), ["QUESTS"] = new(392, 552), ["HISTORY"] = new(392, 382)
    };

    private readonly Panel _campPanel, _questsPanel, _historyPanel;
    private readonly RpgHeroPanel _hero;
    private readonly RpgStatsPanel _stats;
    private readonly PixelScrollPanel _quotaCards;
    private readonly DailyUsageChart _dailyChart;
    private readonly Label _brand, _questTitle, _status, _compactReset, _footer;
    private readonly Button _campTab, _questsTab, _historyTab, _theme, _refresh, _close;
    private readonly NotifyIcon _trayIcon;
    private readonly System.Windows.Forms.Timer _hostTimer, _countdownTimer, _refreshTimer;
    private readonly CancellationTokenSource _shutdown = new();
    private DesktopSettings _settings;
    private CodexAppServerClient? _client;
    private bool _refreshing, _allowExit, _manuallyHidden;
    private nint _hostWindow;
    private DateTimeOffset? _codexMissingSince, _compactResetAt;
    private DateTimeOffset? _lastFetchedAt;
    private bool _lastRefreshFailed;

    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get
        {
            const int toolWindow = 0x00000080, noActivate = 0x08000000;
            var value = base.CreateParams;
            value.ExStyle |= toolWindow | noActivate;
            return value;
        }
    }

    internal UsageWindow()
    {
        _settings = DesktopSettings.Load();
        HudColors.SetTheme((HudTheme)_settings.ThemeIndex);
        Text = "Codex Token Quest";
        ClientSize = PanelSizes[_settings.SelectedPanel];
        BackColor = HudColors.Background;
        ForeColor = HudColors.Text;
        Font = new Font("Consolas", 8f, FontStyle.Bold);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = MinimizeBox = ShowIcon = ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = DoubleBuffered = true;

        _brand = new Label { Text = "◆ CODEX TOKEN QUEST ◆", Font = new Font("Consolas", 9f, FontStyle.Bold), Location = new(12, 10), Size = new(210, 17) };
        _status = new Label { Text = "LOADING SAVE DATA...", Font = new Font("Consolas", 6.5f, FontStyle.Bold), Location = new(14, 29), Size = new(215, 14) };
        _theme = CreateButton("A PIX", 249, 9, 61, 25);
        _refresh = CreateButton("↻", 315, 9, 28, 25);
        _close = CreateButton("×", 348, 9, 31, 25);
        _theme.Click += (_, _) => CycleTheme();
        _refresh.Click += async (_, _) => await RefreshSnapshotAsync();
        _close.Click += (_, _) => { _manuallyHidden = true; Hide(); };

        _campTab = CreateButton("CAMP", 11, 51, 116, 29);
        _questsTab = CreateButton("QUESTS", 132, 51, 116, 29);
        _historyTab = CreateButton("HISTORY", 253, 51, 128, 29);
        _campTab.Click += (_, _) => SelectPanel("CAMP");
        _questsTab.Click += (_, _) => SelectPanel("QUESTS");
        _historyTab.Click += (_, _) => SelectPanel("HISTORY");

        _hero = new RpgHeroPanel { CharacterIndex = _settings.CharacterIndex, Location = new(0, 0), Size = new(164, 174) };
        _stats = new RpgStatsPanel { Location = new(170, 0), Size = new(198, 174) };
        _compactReset = new Label
        {
            Text = "◆ NEXT RESET // UNKNOWN", Font = new Font("Consolas", 7f, FontStyle.Bold),
            BorderStyle = BorderStyle.FixedSingle, TextAlign = ContentAlignment.MiddleLeft,
            Location = new(3, 180), Padding = new(7, 0, 0, 0), Size = new(364, 24)
        };
        _campPanel = new Panel { Location = new(11, 88), Size = new(370, 206) };
        _campPanel.Controls.AddRange([_hero, _stats, _compactReset]);

        _questTitle = new Label
        {
            Text = "⚔ STAMINA DUNGEON // WEEKLY LIMITS", Font = new Font("Consolas", 7f, FontStyle.Bold),
            Location = new(3, 0), Size = new(360, 16)
        };
        _quotaCards = new PixelScrollPanel { Location = new(3, 22), Size = new(365, 374) };
        _questsPanel = new Panel { Location = new(11, 88), Size = new(370, 398) };
        _questsPanel.Controls.AddRange([_questTitle, _quotaCards]);

        _dailyChart = new DailyUsageChart { Location = new(3, 0), Size = new(365, 238) };
        _historyPanel = new Panel { Location = new(11, 88), Size = new(370, 238) };
        _historyPanel.Controls.Add(_dailyChart);
        _hero.CharacterChanged += (_, index) => SelectCharacter(index);

        _footer = new Label
        {
            Text = "AUTO-SAVE ◆ SYNC 5M ◆ OPTIONS", Cursor = Cursors.Hand,
            Font = new Font("Consolas", 6.7f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft,
            Location = new(14, ClientSize.Height - 28), Size = new(ClientSize.Width - 28, 16)
        };
        _footer.Click += (_, _) => ShowSettings();
        Controls.AddRange([_brand, _status, _theme, _refresh, _close, _campTab, _questsTab, _historyTab, _campPanel, _questsPanel, _historyPanel, _footer]);

        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("顯示 / 隱藏", null, (_, _) => ToggleVisibility());
        trayMenu.Items.Add("營地", null, (_, _) => SelectPanel("CAMP"));
        trayMenu.Items.Add("任務額度", null, (_, _) => SelectPanel("QUESTS"));
        trayMenu.Items.Add("歷史紀錄", null, (_, _) => SelectPanel("HISTORY"));
        trayMenu.Items.Add("切換介面主題", null, (_, _) => CycleTheme());
        trayMenu.Items.Add("重新讀取冒險紀錄", null, async (_, _) => await RefreshSnapshotAsync());
        trayMenu.Items.Add("遊戲選項", null, (_, _) => ShowSettings());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("離開遊戲", null, (_, _) => ExitApplication());
        _trayIcon = new NotifyIcon { Icon = SystemIcons.Application, Text = "Codex Token Quest", ContextMenuStrip = trayMenu, Visible = true };
        _trayIcon.DoubleClick += (_, _) => ToggleVisibility();

        _hostTimer = new() { Interval = 500, Enabled = true };
        _hostTimer.Tick += (_, _) => TrackHostWindow();
        _countdownTimer = new() { Interval = 1000, Enabled = true };
        _countdownTimer.Tick += (_, _) => UpdateCountdowns();
        _refreshTimer = new();
        _refreshTimer.Tick += async (_, _) => await RefreshSnapshotAsync();
        ApplyTheme();
        ApplyPanel();
        ApplyRefreshInterval();
        Shown += async (_, _) => { TrackHostWindow(); await RefreshSnapshotAsync(); };
    }

    private static Button CreateButton(string text, int x, int y, int width, int height)
    {
        var button = new Button
        {
            Text = text, Font = new Font("Consolas", 7f, FontStyle.Bold), FlatStyle = FlatStyle.Flat,
            TabStop = false, Location = new(x, y), Size = new(width, height), Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 2;
        return button;
    }

    private void SelectPanel(string panel)
    {
        if (!PanelSizes.ContainsKey(panel)) return;
        _settings = _settings with { SelectedPanel = panel };
        _settings.Save();
        ApplyPanel();
    }

    private void ApplyPanel()
    {
        SuspendLayout();
        _campPanel.Visible = _settings.SelectedPanel == "CAMP";
        _questsPanel.Visible = _settings.SelectedPanel == "QUESTS";
        _historyPanel.Visible = _settings.SelectedPanel == "HISTORY";
        ClientSize = PanelSizes[_settings.SelectedPanel];
        _footer.Location = new(14, ClientSize.Height - 28);
        _footer.Size = new(ClientSize.Width - 28, 16);
        StyleNavigation();
        ResumeLayout(true);
        TrackHostWindow();
        Invalidate();
    }

    private void CycleTheme()
    {
        var next = (_settings.ThemeIndex + 1) % Enum.GetValues<HudTheme>().Length;
        _settings = _settings with { ThemeIndex = next };
        _settings.Save();
        HudColors.SetTheme((HudTheme)next);
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        BackColor = HudColors.Background;
        ForeColor = HudColors.Text;
        var tabs = HudCopy.Tabs;
        _brand.Text = HudCopy.Brand;
        _campTab.Text = tabs.Camp;
        _questsTab.Text = tabs.Quests;
        _historyTab.Text = tabs.History;
        _questTitle.Text = HudCopy.QuestTitle;
        _footer.Text = HudCopy.Footer(_settings.RefreshMinutes);
        _status.Text = _refreshing
            ? HudCopy.Loading
            : _lastRefreshFailed
                ? HudCopy.Lost
                : _lastFetchedAt is not null
                    ? HudCopy.Ready(_lastFetchedAt.Value)
                    : HudCopy.Loading;
        _brand.ForeColor = HudColors.Gold;
        _status.ForeColor = _refreshing ? HudColors.Amber : HudColors.Green;
        _questTitle.ForeColor = HudColors.Gold;
        _footer.ForeColor = HudColors.Muted;
        _compactReset.BackColor = HudColors.Panel;
        _compactReset.ForeColor = HudColors.Cyan;
        RefreshThemeTree(this);
        _theme.Text = HudColors.Theme switch
        {
            HudTheme.PixelDungeon => "A PIX", HudTheme.ArcaneGlass => "B GLS",
            HudTheme.GuildLedger => "C GUILD", _ => "D TERM"
        };
        StyleNavigation();
        Invalidate(true);
    }

    private static void RefreshThemeTree(Control root)
    {
        foreach (Control control in root.Controls)
        {
            if (control is Panel or PixelScrollPanel) control.BackColor = HudColors.Background;
            else if (control is RpgHeroPanel or RpgStatsPanel or QuotaCard or DailyUsageChart) control.BackColor = HudColors.Panel;
            control.Invalidate();
            if (control.HasChildren) RefreshThemeTree(control);
        }
    }

    private void StyleNavigation()
    {
        StyleButton(_campTab, _settings.SelectedPanel == "CAMP" ? HudColors.Gold : HudColors.Grid);
        StyleButton(_questsTab, _settings.SelectedPanel == "QUESTS" ? HudColors.Gold : HudColors.Grid);
        StyleButton(_historyTab, _settings.SelectedPanel == "HISTORY" ? HudColors.Gold : HudColors.Grid);
        StyleButton(_theme, HudColors.Cyan);
        StyleButton(_refresh, HudColors.Green);
        StyleButton(_close, HudColors.Red);
    }

    private static void StyleButton(Button button, Color accent)
    {
        button.ForeColor = accent;
        button.BackColor = HudColors.Panel;
        button.FlatAppearance.BorderColor = accent;
        button.FlatAppearance.MouseOverBackColor = HudColors.PanelBright;
        button.FlatAppearance.MouseDownBackColor = HudColors.Ink;
    }

    private void SelectCharacter(int index)
    {
        _settings = _settings with { CharacterIndex = index };
        _settings.Save();
        _hero.CharacterIndex = index;
    }

    private async Task RefreshSnapshotAsync()
    {
        if (_refreshing || _shutdown.IsCancellationRequested) return;
        _refreshing = true;
        _status.Text = HudCopy.Loading;
        _status.ForeColor = HudColors.Amber;
        _refresh.Enabled = false;
        try
        {
            _client ??= await CodexAppServerClient.StartAsync(_shutdown.Token);
            var snapshot = await _client.ReadSnapshotAsync(_shutdown.Token);
            RenderSnapshot(snapshot);
            _lastFetchedAt = snapshot.FetchedAt.ToLocalTime();
            _lastRefreshFailed = false;
            _status.Text = HudCopy.Ready(_lastFetchedAt.Value);
            _status.ForeColor = HudColors.Green;
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { }
        catch (Exception exception) { await ResetClientAsync(); RenderError(exception.Message); }
        finally { _refreshing = false; _refresh.Enabled = true; }
    }

    private void RenderSnapshot(UsageSnapshot snapshot)
    {
        var weekly = snapshot.RateLimits
            .Where(bucket => bucket.WindowDurationMinutes >= 7 * 24 * 60)
            .OrderBy(bucket => bucket.Id.Equals("codex", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(bucket => bucket.Name ?? bucket.Id, StringComparer.CurrentCultureIgnoreCase).FirstOrDefault()
            ?? snapshot.RateLimits.OrderByDescending(bucket => bucket.WindowDurationMinutes ?? 0).FirstOrDefault();
        var stamina = weekly?.RemainingPercent;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var apiToday = snapshot.DailyUsage.FirstOrDefault(item => item.Date == today)?.Tokens;
        var localToday = LocalTokenUsageReader.ReadForDate(today);
        long? todayTokens = apiToday is not null && localToday is not null ? Math.Max(apiToday.Value, localToday.Value) : apiToday ?? localToday;
        var lifetime = snapshot.Tokens?.LifetimeTokens;
        var level = RpgProgress.GetLevel(Math.Max(0, lifetime ?? 0));
        var selected = Math.Clamp(_settings.CharacterIndex, 0, RpgHeroPanel.Characters.Count - 1);
        if (RpgHeroPanel.Characters[selected].UnlockLevel > level)
        {
            selected = RpgHeroPanel.Characters.Select((character, index) => (character, index))
                .Where(item => item.character.UnlockLevel <= level).Select(item => item.index).LastOrDefault();
            SelectCharacter(selected);
        }
        _hero.Level = level;
        _hero.CharacterIndex = selected;
        _stats.SetStats(lifetime, todayTokens, stamina);
        _compactResetAt = weekly?.ResetsAt;
        UpdateCompactReset(DateTimeOffset.Now);

        _quotaCards.SuspendLayout();
        _quotaCards.Controls.Clear();
        foreach (var bucket in snapshot.RateLimits.OrderBy(item => item.Id.Equals("codex", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                     .ThenByDescending(item => item.WindowDurationMinutes ?? 0))
            _quotaCards.Controls.Add(new QuotaCard(bucket));
        _quotaCards.ResumeLayout();

        var chartUsage = snapshot.DailyUsage.Where(item => item.Date != today).ToList();
        if (todayTokens is not null) chartUsage.Add(new(today, todayTokens.Value));
        _dailyChart.SetData(chartUsage);
        var hero = RpgHeroPanel.Characters[selected];
        _trayIcon.Text = stamina is null ? $"{hero.Name} ◆ LV.{level}" : $"{hero.Name} ◆ LV.{level} ◆ STA {stamina:0.#}";
    }

    private void RenderError(string message)
    {
        _stats.SetStats(null, null, null);
        _lastRefreshFailed = true;
        _quotaCards.Controls.Clear();
        _dailyChart.SetData([]);
        _compactResetAt = null;
        UpdateCompactReset(DateTimeOffset.Now);
        _status.Text = HudCopy.Lost;
        _status.ForeColor = HudColors.Red;
        _trayIcon.Text = "Codex Token Quest：讀取失敗";
        _trayIcon.BalloonTipTitle = "冒險紀錄讀取失敗";
        _trayIcon.BalloonTipText = message.Length > 220 ? message[..220] : message;
        _trayIcon.ShowBalloonTip(3500);
    }

    private void UpdateCountdowns()
    {
        var now = DateTimeOffset.Now;
        foreach (var card in _quotaCards.Controls.OfType<QuotaCard>()) card.UpdateCountdown(now);
        UpdateCompactReset(now);
    }

    private void UpdateCompactReset(DateTimeOffset now)
    {
        if (_compactResetAt is null)
        {
            _compactReset.Text = $"◆ NEXT {HudCopy.Reset} // UNKNOWN";
            _compactReset.ForeColor = HudColors.Muted;
            return;
        }
        var local = _compactResetAt.Value.ToLocalTime();
        var remaining = local - now;
        var countdown = remaining <= TimeSpan.Zero ? "SYNCING" : QuotaCard.FormatDuration(remaining);
        _compactReset.Text = $"◆ NEXT {HudCopy.Reset} // {local:MM/dd HH:mm} // {countdown}";
        _compactReset.ForeColor = remaining <= TimeSpan.FromHours(12) ? HudColors.Amber : HudColors.Cyan;
    }

    private void ApplyRefreshInterval()
    {
        _refreshTimer.Interval = checked(_settings.RefreshMinutes * 60 * 1000);
        _refreshTimer.Start();
        _footer.Text = HudCopy.Footer(_settings.RefreshMinutes);
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings.RefreshMinutes);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        _settings = _settings with { RefreshMinutes = form.RefreshMinutes };
        _settings.Save();
        ApplyRefreshInterval();
    }

    private void TrackHostWindow()
    {
        _hostWindow = NativeMethods.FindCodexWindow();
        if (_hostWindow == 0 || !NativeMethods.GetWindowRect(_hostWindow, out var host))
        {
            if (Visible) Hide();
            if (NativeMethods.IsCodexRunning()) { _codexMissingSince = null; return; }
            _codexMissingSince ??= DateTimeOffset.Now;
            if (DateTimeOffset.Now - _codexMissingSince >= TimeSpan.FromSeconds(5)) ExitApplication();
            return;
        }
        _codexMissingSince = null;
        var target = PanelSizes[_settings.SelectedPanel];
        const int margin = 12;
        var width = Math.Min(target.Width, Math.Max(280, host.Right - host.Left - margin * 2));
        var height = Math.Min(target.Height, Math.Max(210, host.Bottom - host.Top - margin * 2));
        if (Size != new Size(width, height)) Size = new(width, height);
        Location = new(host.Right - Width - margin, host.Bottom - Height - margin);
        if (!Visible && !_manuallyHidden) Show();
    }

    private void ToggleVisibility()
    {
        if (Visible) { _manuallyHidden = true; Hide(); }
        else { _manuallyHidden = false; TrackHostWindow(); }
    }
    private void ExitApplication() { _allowExit = true; Close(); }
    private async Task ResetClientAsync()
    {
        if (_client is null) return;
        var client = _client;
        _client = null;
        await client.DisposeAsync();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        PixelArt.DrawPanel(eventArgs.Graphics, ClientRectangle, HudColors.Gold);
        using var line = new Pen(HudColors.Grid);
        eventArgs.Graphics.DrawLine(line, 9, 46, ClientSize.Width - 10, 46);
        eventArgs.Graphics.DrawLine(line, 9, 84, ClientSize.Width - 10, 84);
    }

    protected override void OnFormClosing(FormClosingEventArgs eventArgs)
    {
        if (!_allowExit && eventArgs.CloseReason == CloseReason.UserClosing)
        {
            eventArgs.Cancel = true;
            _manuallyHidden = true;
            Hide();
            return;
        }
        _shutdown.Cancel();
        _hostTimer.Stop();
        _countdownTimer.Stop();
        _refreshTimer.Stop();
        _trayIcon.Visible = false;
        base.OnFormClosing(eventArgs);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _shutdown.Cancel();
            _hostTimer.Dispose();
            _countdownTimer.Dispose();
            _refreshTimer.Dispose();
            _trayIcon.Dispose();
            if (_client is not null) { _client.DisposeAsync().AsTask().GetAwaiter().GetResult(); _client = null; }
            _shutdown.Dispose();
        }
        base.Dispose(disposing);
    }
}
