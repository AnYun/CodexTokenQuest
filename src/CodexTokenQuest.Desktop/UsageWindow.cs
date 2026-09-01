namespace CodexTokenQuest.Desktop;

internal sealed class UsageWindow : Form
{
    private static readonly Size CompactSize = new(362, 284);
    private static readonly Size FullSize = new(392, 636);

    private readonly Panel _compactPanel;
    private readonly Panel _fullPanel;
    private readonly RpgHeroPanel _compactHero;
    private readonly RpgHeroPanel _fullHero;
    private readonly RpgStatsPanel _compactStats;
    private readonly RpgStatsPanel _fullStats;
    private readonly PixelScrollPanel _quotaCards;
    private readonly DailyUsageChart _dailyChart;
    private readonly Label _status;
    private readonly Label _compactReset;
    private readonly Label _footer;
    private readonly Button _mode;
    private readonly Button _refresh;
    private readonly Button _close;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _trayMode;
    private readonly System.Windows.Forms.Timer _hostTimer;
    private readonly System.Windows.Forms.Timer _countdownTimer;
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private readonly CancellationTokenSource _shutdown = new();

    private DesktopSettings _settings;
    private CodexAppServerClient? _client;
    private bool _refreshing;
    private bool _allowExit;
    private bool _manuallyHidden;
    private nint _hostWindow;
    private DateTimeOffset? _codexMissingSince;
    private DateTimeOffset? _compactResetAt;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int wsExToolWindow = 0x00000080;
            const int wsExNoActivate = 0x08000000;
            var parameters = base.CreateParams;
            parameters.ExStyle |= wsExToolWindow | wsExNoActivate;
            return parameters;
        }
    }

    internal UsageWindow()
    {
        _settings = DesktopSettings.Load();
        Text = "Codex Token Quest";
        ClientSize = CompactSize;
        BackColor = HudColors.Background;
        ForeColor = HudColors.Text;
        Font = new Font("Consolas", 8f, FontStyle.Bold);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        DoubleBuffered = true;

        var brand = new Label
        {
            Text = "◆ CODEX TOKEN QUEST ◆",
            Font = new Font("Consolas", 9f, FontStyle.Bold),
            ForeColor = HudColors.Gold,
            Location = new Point(12, 10),
            Size = new Size(198, 17)
        };
        _status = new Label
        {
            Text = "LOADING SAVE DATA...",
            Font = new Font("Consolas", 6.5f, FontStyle.Bold),
            ForeColor = HudColors.Muted,
            Location = new Point(14, 29),
            Size = new Size(205, 14)
        };
        _mode = CreatePixelButton("MAP", HudColors.Cyan, 248, 9, 49);
        _mode.Click += (_, _) => ToggleMode();
        _refresh = CreatePixelButton("↻", HudColors.Green, 300, 9, 24);
        _refresh.Click += async (_, _) => await RefreshSnapshotAsync();
        _close = CreatePixelButton("×", HudColors.Red, 327, 9, 24);
        _close.Click += (_, _) =>
        {
            _manuallyHidden = true;
            Hide();
        };

        _compactHero = new RpgHeroPanel
        {
            CharacterIndex = _settings.CharacterIndex,
            Location = new Point(0, 0),
            Size = new Size(136, 174)
        };
        _compactStats = new RpgStatsPanel { Location = new Point(142, 0), Size = new Size(198, 174) };
        _compactPanel = new Panel
        {
            BackColor = HudColors.Background,
            Location = new Point(11, 51),
            Size = new Size(340, 176)
        };
        _compactPanel.Controls.AddRange([_compactHero, _compactStats]);
        _compactReset = new Label
        {
            Text = "◆ NEXT RESET // UNKNOWN",
            Font = new Font("Consolas", 6.7f, FontStyle.Bold),
            ForeColor = HudColors.Cyan,
            BackColor = HudColors.Panel,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(14, 231),
            Padding = new Padding(5, 0, 0, 0),
            Size = new Size(334, 19)
        };

        _fullHero = new RpgHeroPanel
        {
            CharacterIndex = _settings.CharacterIndex,
            Location = new Point(0, 0),
            Size = new Size(164, 218)
        };
        _fullStats = new RpgStatsPanel { Location = new Point(170, 0), Size = new Size(198, 218) };
        var questTitle = new Label
        {
            Text = "⚔ STAMINA DUNGEON // WEEKLY LIMITS",
            ForeColor = HudColors.Gold,
            Font = new Font("Consolas", 7f, FontStyle.Bold),
            Location = new Point(3, 225),
            Size = new Size(360, 15)
        };
        _quotaCards = new PixelScrollPanel
        {
            BackColor = HudColors.Background,
            Location = new Point(3, 244),
            Size = new Size(365, 174)
        };
        _dailyChart = new DailyUsageChart { Location = new Point(3, 425), Size = new Size(365, 112) };
        _fullPanel = new Panel
        {
            BackColor = HudColors.Background,
            Location = new Point(11, 51),
            Size = new Size(370, 542)
        };
        _fullPanel.Controls.AddRange([_fullHero, _fullStats, questTitle, _quotaCards, _dailyChart]);

        _compactHero.CharacterChanged += (_, index) => SelectCharacter(index);
        _fullHero.CharacterChanged += (_, index) => SelectCharacter(index);

        _footer = new Label
        {
            Text = "AUTO-SAVE ◆ SYNC 5M ◆ OPTIONS",
            Cursor = Cursors.Hand,
            Font = new Font("Consolas", 6.7f, FontStyle.Bold),
            ForeColor = HudColors.Muted,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(14, 237),
            Size = new Size(330, 16)
        };
        _footer.Click += (_, _) => ShowSettings();

        Controls.AddRange([brand, _status, _mode, _refresh, _close, _compactPanel, _compactReset, _fullPanel, _footer]);

        _trayMode = new ToolStripMenuItem("開啟冒險地圖（完整版）", null, (_, _) => ToggleMode());
        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("顯示 / 隱藏", null, (_, _) => ToggleVisibility());
        trayMenu.Items.Add(_trayMode);
        trayMenu.Items.Add("重新讀取冒險紀錄", null, async (_, _) => await RefreshSnapshotAsync());
        trayMenu.Items.Add("遊戲選項", null, (_, _) => ShowSettings());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("離開遊戲", null, (_, _) => ExitApplication());
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Codex Token Quest",
            ContextMenuStrip = trayMenu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => ToggleVisibility();

        _hostTimer = new System.Windows.Forms.Timer { Interval = 500, Enabled = true };
        _hostTimer.Tick += (_, _) => TrackHostWindow();
        _countdownTimer = new System.Windows.Forms.Timer { Interval = 1000, Enabled = true };
        _countdownTimer.Tick += (_, _) => UpdateCountdowns();
        _refreshTimer = new System.Windows.Forms.Timer();
        _refreshTimer.Tick += async (_, _) => await RefreshSnapshotAsync();

        ApplyMode();
        ApplyRefreshInterval();
        Shown += async (_, _) =>
        {
            TrackHostWindow();
            await RefreshSnapshotAsync();
        };
    }

    private static Button CreatePixelButton(string text, Color accent, int x, int y, int width)
    {
        var button = new Button
        {
            Text = text,
            Font = new Font("Consolas", 7f, FontStyle.Bold),
            ForeColor = accent,
            BackColor = HudColors.Panel,
            FlatStyle = FlatStyle.Flat,
            TabStop = false,
            Location = new Point(x, y),
            Size = new Size(width, 25),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = accent;
        button.FlatAppearance.BorderSize = 2;
        button.FlatAppearance.MouseOverBackColor = HudColors.PanelBright;
        return button;
    }

    private void ApplyMode()
    {
        SuspendLayout();
        ClientSize = _settings.CompactMode ? CompactSize : FullSize;
        _compactPanel.Visible = _settings.CompactMode;
        _compactReset.Visible = _settings.CompactMode;
        _fullPanel.Visible = !_settings.CompactMode;
        _mode.Text = _settings.CompactMode ? "MAP" : "CAMP";
        _trayMode.Text = _settings.CompactMode ? "開啟冒險地圖（完整版）" : "返回營地（精簡版）";
        _mode.Location = new Point(ClientSize.Width - 114, 9);
        _refresh.Location = new Point(ClientSize.Width - 62, 9);
        _close.Location = new Point(ClientSize.Width - 35, 9);
        _footer.Location = new Point(14, ClientSize.Height - 28);
        _footer.Size = new Size(ClientSize.Width - 28, 16);
        ResumeLayout(true);
        TrackHostWindow();
        Invalidate();
    }

    private void ToggleMode()
    {
        _settings = _settings with { CompactMode = !_settings.CompactMode };
        _settings.Save();
        ApplyMode();
    }

    private void SelectCharacter(int index)
    {
        _settings = _settings with { CharacterIndex = index };
        _settings.Save();
        _compactHero.CharacterIndex = index;
        _fullHero.CharacterIndex = index;
    }

    private async Task RefreshSnapshotAsync()
    {
        if (_refreshing || _shutdown.IsCancellationRequested)
        {
            return;
        }

        _refreshing = true;
        _status.Text = "READING QUEST LOG...";
        _status.ForeColor = HudColors.Amber;
        _refresh.Enabled = false;
        try
        {
            _client ??= await CodexAppServerClient.StartAsync(_shutdown.Token);
            var snapshot = await _client.ReadSnapshotAsync(_shutdown.Token);
            RenderSnapshot(snapshot);
            _status.Text = $"SAVE OK ◆ {snapshot.FetchedAt.ToLocalTime():HH:mm:ss}";
            _status.ForeColor = HudColors.Green;
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            await ResetClientAsync();
            RenderError(exception.Message);
        }
        finally
        {
            _refreshing = false;
            _refresh.Enabled = true;
        }
    }

    private void RenderSnapshot(UsageSnapshot snapshot)
    {
        var weekly = snapshot.RateLimits
            .Where(bucket => bucket.WindowDurationMinutes >= 7 * 24 * 60)
            .OrderBy(bucket => bucket.Id.Equals("codex", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(bucket => bucket.Name ?? bucket.Id, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault()
            ?? snapshot.RateLimits.OrderByDescending(bucket => bucket.WindowDurationMinutes ?? 0).FirstOrDefault();
        var stamina = weekly?.RemainingPercent;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var apiTodayTokens = snapshot.DailyUsage.FirstOrDefault(item => item.Date == today)?.Tokens;
        var localTodayTokens = LocalTokenUsageReader.ReadForDate(today);
        long? todayTokens = apiTodayTokens is not null && localTodayTokens is not null
            ? Math.Max(apiTodayTokens.Value, localTodayTokens.Value)
            : apiTodayTokens ?? localTodayTokens;
        var lifetime = snapshot.Tokens?.LifetimeTokens;
        var level = RpgProgress.GetLevel(Math.Max(0, lifetime ?? 0));

        var selected = Math.Clamp(_settings.CharacterIndex, 0, RpgHeroPanel.Characters.Count - 1);
        if (RpgHeroPanel.Characters[selected].UnlockLevel > level)
        {
            selected = RpgHeroPanel.Characters
                .Select((character, index) => (character, index))
                .Where(item => item.character.UnlockLevel <= level)
                .Select(item => item.index)
                .LastOrDefault();
            SelectCharacter(selected);
        }
        _compactHero.Level = level;
        _fullHero.Level = level;
        _compactHero.CharacterIndex = selected;
        _fullHero.CharacterIndex = selected;
        _compactStats.SetStats(lifetime, todayTokens, stamina);
        _fullStats.SetStats(lifetime, todayTokens, stamina);
        _compactResetAt = weekly?.ResetsAt;
        UpdateCompactReset(DateTimeOffset.Now);

        _quotaCards.SuspendLayout();
        _quotaCards.Controls.Clear();
        foreach (var bucket in snapshot.RateLimits
                     .OrderBy(item => item.Id.Equals("codex", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                     .ThenByDescending(item => item.WindowDurationMinutes ?? 0))
        {
            _quotaCards.Controls.Add(new QuotaCard(bucket));
        }
        _quotaCards.ResumeLayout();

        var chartUsage = snapshot.DailyUsage.Where(item => item.Date != today).ToList();
        if (todayTokens is not null)
        {
            chartUsage.Add(new DailyTokenUsage(today, todayTokens.Value));
        }
        _dailyChart.SetData(chartUsage);
        var hero = RpgHeroPanel.Characters[selected];
        _trayIcon.Text = stamina is null
            ? $"{hero.Name} ◆ LV.{level}"
            : $"{hero.Name} ◆ LV.{level} ◆ STA {stamina:0.#}";
    }

    private void RenderError(string message)
    {
        _compactStats.SetStats(null, null, null);
        _fullStats.SetStats(null, null, null);
        _quotaCards.Controls.Clear();
        _dailyChart.SetData([]);
        _compactResetAt = null;
        UpdateCompactReset(DateTimeOffset.Now);
        _status.Text = "QUEST LOG LOST ◆ RETRY";
        _status.ForeColor = HudColors.Red;
        _trayIcon.Text = "Codex Token Quest：讀取失敗";
        _trayIcon.BalloonTipTitle = "冒險紀錄讀取失敗";
        _trayIcon.BalloonTipText = message.Length > 220 ? message[..220] : message;
        _trayIcon.ShowBalloonTip(3500);
    }

    private void UpdateCountdowns()
    {
        var now = DateTimeOffset.Now;
        foreach (var card in _quotaCards.Controls.OfType<QuotaCard>())
        {
            card.UpdateCountdown(now);
        }
        UpdateCompactReset(now);
    }

    private void UpdateCompactReset(DateTimeOffset now)
    {
        if (_compactResetAt is null)
        {
            _compactReset.Text = "◆ NEXT RESET // UNKNOWN";
            _compactReset.ForeColor = HudColors.Muted;
            return;
        }

        var local = _compactResetAt.Value.ToLocalTime();
        var remaining = local - now;
        var countdown = remaining <= TimeSpan.Zero ? "SYNCING" : QuotaCard.FormatDuration(remaining);
        _compactReset.Text = $"◆ NEXT RESET // {local:MM/dd HH:mm} // {countdown}";
        _compactReset.ForeColor = remaining <= TimeSpan.FromHours(12) ? HudColors.Amber : HudColors.Cyan;
    }

    private void ApplyRefreshInterval()
    {
        _refreshTimer.Interval = checked(_settings.RefreshMinutes * 60 * 1000);
        _refreshTimer.Start();
        _footer.Text = $"AUTO-SAVE ◆ SYNC {_settings.RefreshMinutes}M ◆ OPTIONS";
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings.RefreshMinutes);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _settings = _settings with { RefreshMinutes = form.RefreshMinutes };
        _settings.Save();
        ApplyRefreshInterval();
    }

    private void TrackHostWindow()
    {
        _hostWindow = NativeMethods.FindCodexWindow();
        if (_hostWindow == 0 || !NativeMethods.GetWindowRect(_hostWindow, out var host))
        {
            if (Visible)
            {
                Hide();
            }

            if (NativeMethods.IsCodexRunning())
            {
                _codexMissingSince = null;
                return;
            }

            _codexMissingSince ??= DateTimeOffset.Now;
            if (DateTimeOffset.Now - _codexMissingSince >= TimeSpan.FromSeconds(5))
            {
                ExitApplication();
            }
            return;
        }

        _codexMissingSince = null;

        var targetSize = _settings.CompactMode ? CompactSize : FullSize;
        const int margin = 12;
        var hostWidth = host.Right - host.Left;
        var hostHeight = host.Bottom - host.Top;
        var desiredWidth = Math.Min(targetSize.Width, Math.Max(280, hostWidth - margin * 2));
        var desiredHeight = Math.Min(targetSize.Height, Math.Max(210, hostHeight - margin * 2));
        if (Size != new Size(desiredWidth, desiredHeight))
        {
            Size = new Size(desiredWidth, desiredHeight);
        }
        Location = new Point(host.Right - Width - margin, host.Bottom - Height - margin);
        if (!Visible && !_manuallyHidden)
        {
            Show();
        }
    }

    private void ToggleVisibility()
    {
        if (Visible)
        {
            _manuallyHidden = true;
            Hide();
        }
        else
        {
            _manuallyHidden = false;
            TrackHostWindow();
        }
    }

    private void ExitApplication()
    {
        _allowExit = true;
        Close();
    }

    private async Task ResetClientAsync()
    {
        if (_client is null)
        {
            return;
        }
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
            if (_client is not null)
            {
                _client.DisposeAsync().AsTask().GetAwaiter().GetResult();
                _client = null;
            }
            _shutdown.Dispose();
        }
        base.Dispose(disposing);
    }
}
