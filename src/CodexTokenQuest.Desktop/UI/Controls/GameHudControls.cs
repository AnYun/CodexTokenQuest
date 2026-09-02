using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace CodexTokenQuest.Desktop;

internal static class HudScale
{
    private static double _factor = 1d;

    internal static double Factor => _factor;
    internal static void Set(int percent) => _factor = Math.Clamp(percent, 50, 300) / 100d;
    internal static int Px(int value)
    {
        if (value == 0) return 0;
        var scaled = (int)Math.Round(value * _factor);
        return value > 0 ? Math.Max(1, scaled) : Math.Min(-1, scaled);
    }
    internal static int Logical(int value) => Math.Max(1, (int)Math.Round(value / _factor));
}

internal enum HudTheme
{
    PixelDungeon,
    ArcaneGlass,
    GuildLedger,
    CodeTerminal
}

internal sealed record HudPalette(
    Color Background,
    Color Panel,
    Color PanelBright,
    Color Ink,
    Color Cream,
    Color Gold,
    Color Cyan,
    Color Green,
    Color Amber,
    Color Red,
    Color Text,
    Color Muted,
    Color Grid);

internal static class HudColors
{
    private static readonly HudPalette[] Palettes =
    [
        new(Color.FromArgb(18, 16, 29), Color.FromArgb(32, 30, 49), Color.FromArgb(46, 42, 66), Color.FromArgb(15, 13, 24), Color.FromArgb(255, 238, 196), Color.FromArgb(255, 203, 79), Color.FromArgb(91, 224, 255), Color.FromArgb(92, 224, 112), Color.FromArgb(255, 162, 72), Color.FromArgb(235, 76, 83), Color.FromArgb(250, 244, 225), Color.FromArgb(167, 154, 174), Color.FromArgb(92, 76, 112)),
        new(Color.FromArgb(10, 10, 30), Color.FromArgb(25, 24, 62), Color.FromArgb(43, 39, 92), Color.FromArgb(7, 7, 24), Color.FromArgb(238, 237, 255), Color.FromArgb(199, 149, 255), Color.FromArgb(88, 205, 255), Color.FromArgb(98, 235, 195), Color.FromArgb(255, 176, 92), Color.FromArgb(255, 95, 137), Color.FromArgb(245, 243, 255), Color.FromArgb(168, 163, 205), Color.FromArgb(91, 75, 158)),
        new(Color.FromArgb(61, 42, 25), Color.FromArgb(224, 197, 143), Color.FromArgb(239, 218, 171), Color.FromArgb(55, 35, 20), Color.FromArgb(62, 40, 22), Color.FromArgb(139, 91, 38), Color.FromArgb(78, 96, 71), Color.FromArgb(75, 111, 55), Color.FromArgb(160, 91, 35), Color.FromArgb(151, 52, 39), Color.FromArgb(55, 35, 20), Color.FromArgb(112, 82, 52), Color.FromArgb(151, 111, 66)),
        new(Color.FromArgb(3, 10, 7), Color.FromArgb(7, 20, 13), Color.FromArgb(12, 35, 21), Color.FromArgb(1, 7, 4), Color.FromArgb(197, 255, 148), Color.FromArgb(255, 192, 53), Color.FromArgb(95, 255, 157), Color.FromArgb(144, 255, 65), Color.FromArgb(255, 187, 51), Color.FromArgb(255, 83, 83), Color.FromArgb(171, 255, 112), Color.FromArgb(91, 156, 80), Color.FromArgb(38, 91, 52))
    ];

    internal static HudTheme Theme { get; private set; }
    private static HudPalette Current => Palettes[(int)Theme];
    internal static Color Background => Current.Background;
    internal static Color Panel => Current.Panel;
    internal static Color PanelBright => Current.PanelBright;
    internal static Color Ink => Current.Ink;
    internal static Color Cream => Current.Cream;
    internal static Color Gold => Current.Gold;
    internal static Color Cyan => Current.Cyan;
    internal static Color Green => Current.Green;
    internal static Color Amber => Current.Amber;
    internal static Color Red => Current.Red;
    internal static Color Text => Current.Text;
    internal static Color Muted => Current.Muted;
    internal static Color Grid => Current.Grid;

    internal static void SetTheme(HudTheme theme) => Theme = theme;
}

internal static class HudCopy
{
    internal static string Brand => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => L("◆ CODEX TOKEN QUEST ◆", "◆ CODEX 代幣任務 ◆"),
        HudTheme.ArcaneGlass => L("✦ ARCANE TOKEN ORACLE ✦", "✦ 奧術代幣神諭 ✦"),
        HudTheme.GuildLedger => L("◆ ADVENTURERS' LEDGER ◆", "◆ 冒險者公會帳簿 ◆"),
        _ => L("> CODEX_USAGE_MONITOR", "> CODEX_用量監控器")
    };

    internal static (string Camp, string Quests, string History) Tabs => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => (L("CAMP", "營地"), L("QUESTS", "任務"), L("HISTORY", "歷史")),
        HudTheme.ArcaneGlass => (L("SANCTUM", "聖所"), L("RUNES", "符文"), L("ECHOES", "回響")),
        HudTheme.GuildLedger => (L("GUILD", "公會"), L("CONTRACTS", "契約"), L("LEDGER", "帳簿")),
        _ => (L("STATUS", "狀態"), L("LIMITS", "額度"), L("LOGS", "紀錄"))
    };

    internal static string QuestTitle => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => L("⚔ STAMINA DUNGEON // WEEKLY LIMITS", "⚔ 耐力地城 // 每週額度"),
        HudTheme.ArcaneGlass => L("✦ RESONANCE RUNES // ARCANE LIMITS", "✦ 共鳴符文 // 奧術額度"),
        HudTheme.GuildLedger => L("◆ ACTIVE CONTRACTS // WEEKLY ALLOWANCE", "◆ 進行中契約 // 每週額度"),
        _ => L("> QUOTA_WINDOWS --WEEKLY", "> 額度視窗 --每週")
    };

    internal static string Loading => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => L("READING QUEST LOG...", "讀取任務紀錄..."),
        HudTheme.ArcaneGlass => L("DIVINING TOKEN ECHOES...", "占卜代幣回響..."),
        HudTheme.GuildLedger => L("OPENING GUILD RECORDS...", "開啟公會紀錄..."),
        _ => L("READING_USAGE_STREAM...", "讀取_用量_資料流...")
    };

    internal static string Ready(DateTimeOffset time) => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => L($"SAVE OK ◆ {time:HH:mm:ss}", $"儲存完成 ◆ {time:HH:mm:ss}"),
        HudTheme.ArcaneGlass => L($"ORACLE ATTUNED ✦ {time:HH:mm:ss}", $"神諭同步 ✦ {time:HH:mm:ss}"),
        HudTheme.GuildLedger => L($"LEDGER SEALED ◆ {time:HH:mm:ss}", $"帳簿封存 ◆ {time:HH:mm:ss}"),
        _ => L($"STATUS_OK :: {time:HH:mm:ss}", $"狀態_正常 :: {time:HH:mm:ss}")
    };

    internal static string Lost => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => L("QUEST LOG LOST ◆ RETRY", "任務紀錄遺失 ◆ 重試"),
        HudTheme.ArcaneGlass => L("DIVINATION BROKEN ✦ RETRY", "占卜中斷 ✦ 重試"),
        HudTheme.GuildLedger => L("LEDGER UNAVAILABLE ◆ RETRY", "帳簿無法使用 ◆ 重試"),
        _ => L("ERR_USAGE_STREAM :: RETRY", "錯誤_用量資料流 :: 重試")
    };

    internal static string Footer(int minutes) => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => L($"AUTO-SAVE ◆ SYNC {minutes}M ◆ OPTIONS", $"自動儲存 ◆ 同步 {minutes}分 ◆ 選項"),
        HudTheme.ArcaneGlass => L($"AUTO-ATTUNE ✦ {minutes}M ✦ SETTINGS", $"自動調和 ✦ {minutes}分 ✦ 設定"),
        HudTheme.GuildLedger => L($"AUTO-RECORD ◆ {minutes}M ◆ OPTIONS", $"自動記錄 ◆ {minutes}分 ◆ 選項"),
        _ => L($"AUTO_SYNC={minutes}M :: CONFIG", $"自動_同步={minutes}分 :: 設定")
    };

    internal static string EmptyHistory => HudColors.Theme switch
    {
        HudTheme.ArcaneGlass => L("NO ECHOES DETECTED", "未偵測到回響"),
        HudTheme.GuildLedger => L("NO ENTRIES RECORDED", "尚無帳簿紀錄"),
        HudTheme.CodeTerminal => L("NO_LOG_DATA", "沒有_紀錄_資料"),
        _ => L("NO QUEST RECORD", "尚無任務紀錄")
    };

    internal static string StatusTitle => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => L("ADVENTURER STATUS", "冒險者狀態"),
        HudTheme.ArcaneGlass => L("ARCANE SIGNATURE", "奧術印記"),
        HudTheme.GuildLedger => L("GUILD RECORD", "公會紀錄"),
        _ => L("AGENT STATUS", "代理狀態")
    };

    internal static string Stamina => HudColors.Theme switch
    {
        HudTheme.ArcaneGlass => L("MANA", "魔力"), HudTheme.GuildLedger => L("VIGOR", "活力"),
        HudTheme.CodeTerminal => L("QUOTA", "額度"), _ => L("STA", "耐力")
    };

    internal static string Experience => HudColors.Theme switch
    {
        HudTheme.ArcaneGlass => L("AURA", "靈氣"), HudTheme.GuildLedger => L("RENOWN", "聲望"),
        HudTheme.CodeTerminal => L("LOAD", "負載"), _ => L("EXP", "經驗")
    };

    internal static string Lifetime => HudColors.Theme switch
    {
        HudTheme.ArcaneGlass => L("CRYSTAL MEMORY", "水晶記憶"), HudTheme.GuildLedger => L("LIFETIME RENOWN", "累積聲望"),
        HudTheme.CodeTerminal => L("TOTAL TOKENS", "代幣總數"), _ => L("TOTAL EXP", "總經驗")
    };

    internal static string Today => HudColors.Theme switch
    {
        HudTheme.ArcaneGlass => L("TODAY'S SPARK", "今日星火"), HudTheme.GuildLedger => L("TODAY'S BOUNTY", "今日賞金"),
        HudTheme.CodeTerminal => L("SESSION TOKENS", "工作階段代幣"), _ => L("TODAY QUEST EXP", "今日任務經驗")
    };

    internal static string Reset => HudColors.Theme switch
    {
        HudTheme.ArcaneGlass => L("RECHARGE", "充能"), HudTheme.GuildLedger => L("RENEWAL", "更新"),
        HudTheme.CodeTerminal => L("RESET_AT", "重設時間"), _ => L("RESET", "重設")
    };

    internal static string ChartTitle => HudColors.Theme switch
    {
        HudTheme.ArcaneGlass => L("ECHOES // 7-DAY RESONANCE", "回響 // 7 日共鳴"),
        HudTheme.GuildLedger => L("LEDGER // 7-DAY BOUNTY", "帳簿 // 7 日賞金"),
        HudTheme.CodeTerminal => L("> TOKEN_LOG --LAST=7D", "> 代幣_紀錄 --最近=7日"),
        _ => L("QUEST LOG // 7-DAY EXP", "任務紀錄 // 7 日經驗")
    };

    internal static (string Name, string Class) Hero(RpgCharacter character)
    {
        var index = character.Name switch { "LYRA" => 1, "SYLVI" => 2, "NOVA" => 3, _ => 0 };
        return HudColors.Theme switch
        {
            HudTheme.ArcaneGlass => (
                $"✦ {new[] { "CAEL", "SELENE", "IRIS", "ORION" }[index]} ✦",
                L(
                    new[] { "CRYSTAL WARDEN", "ASTRAL ORACLE", "PRISM RANGER", "RUNE SENTINEL" }[index],
                    new[] { "水晶守衛", "星界神諭", "稜鏡遊俠", "符文哨兵" }[index])),
            HudTheme.GuildLedger => (
                new[] { "ROWAN", "ELSPETH", "BRIAR", "GARRICK" }[index],
                L(
                    new[] { "GUILD SWORD", "LEDGER ARCANIST", "CONTRACT RANGER", "GUILD MARSHAL" }[index],
                    new[] { "公會劍士", "帳簿秘法師", "契約遊俠", "公會元帥" }[index])),
            HudTheme.CodeTerminal => (
                $"[{new[] { "CIPHER", "SYNTAX", "PACKET", "KERNEL" }[index]}]",
                L(
                    $"ROLE::{new[] { "CODE_SENTINEL", "SYNTAX_WITCH", "PACKET_RANGER", "FIREWALL_KNIGHT" }[index]}",
                    $"角色::{new[] { "程式哨兵", "語法巫師", "封包遊俠", "防火牆騎士" }[index]}")),
            _ => ($"◀ {character.Name} ▶", L(character.ClassName, new[] { "劍盾守衛", "星界法師", "荒野遊俠", "符文騎士" }[index]))
        };
    }

    private static string L(string english, string traditionalChinese) => UiText.Pick(english, traditionalChinese);
}

internal sealed record RpgCharacter(string Name, string ClassName, int UnlockLevel, int Column, int Row);

internal sealed class RpgHeroPanel : Control
{
    internal static readonly IReadOnlyList<RpgCharacter> Characters =
    [
        new("AERON", "SWORD WARDEN", 1, 0, 0),
        new("LYRA", "ASTRAL MAGE", 10, 1, 0),
        new("SYLVI", "WILD RANGER", 25, 0, 1),
        new("NOVA", "RUNE KNIGHT", 50, 1, 1)
    ];

    private readonly Image?[] _spriteSheets = new Image?[Enum.GetValues<HudTheme>().Length];
    private int _characterIndex;
    private int _level = 1;

    internal event EventHandler<int>? CharacterChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int CharacterIndex
    {
        get => _characterIndex;
        set
        {
            _characterIndex = Math.Clamp(value, 0, Characters.Count - 1);
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int Level
    {
        get => _level;
        set { _level = Math.Clamp(value, 1, ExperienceProgress.MaximumLevel); Invalidate(); }
    }

    internal RpgHeroPanel()
    {
        DoubleBuffered = true;
        BackColor = HudColors.Panel;
        Cursor = Cursors.Hand;
        var names = new[]
        {
            "hero-sprite-sheet.png", "hero-sprite-sheet-arcane.png",
            "hero-sprite-sheet-guild.png", "hero-sprite-sheet-terminal.png"
        };
        for (var index = 0; index < names.Length; index++)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "assets", "rpg", names[index]);
            if (!File.Exists(path)) continue;
            using var source = Image.FromFile(path);
            _spriteSheets[index] = new Bitmap(source);
        }
    }

    protected override void OnMouseUp(MouseEventArgs eventArgs)
    {
        base.OnMouseUp(eventArgs);
        var direction = eventArgs.X < Width / 2 ? -1 : 1;
        for (var attempt = 0; attempt < Characters.Count; attempt++)
        {
            var candidate = (_characterIndex + direction + Characters.Count) % Characters.Count;
            _characterIndex = candidate;
            if (Characters[candidate].UnlockLevel <= _level)
            {
                CharacterChanged?.Invoke(this, candidate);
                Invalidate();
                break;
            }
        }
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        var graphics = eventArgs.Graphics;
        var state = graphics.Save();
        graphics.ScaleTransform((float)HudScale.Factor, (float)HudScale.Factor);
        var width = HudScale.Logical(Width);
        var height = HudScale.Logical(Height);
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        PixelArt.DrawPanel(graphics, new Rectangle(0, 0, width, height), HudColors.Gold);

        var heroArea = new Rectangle(7, 7, width - 14, Math.Max(70, height - 51));
        var skyColor = HudColors.Theme switch
        {
            HudTheme.ArcaneGlass => Color.FromArgb(38, 29, 86),
            HudTheme.GuildLedger => Color.FromArgb(184, 149, 91),
            HudTheme.CodeTerminal => Color.FromArgb(4, 18, 10),
            _ => Color.FromArgb(35, 47, 73)
        };
        using var sky = new SolidBrush(skyColor);
        graphics.FillRectangle(sky, heroArea);
        PixelArt.DrawDungeonBackdrop(graphics, heroArea);

        var character = Characters[_characterIndex];
        var spriteSheet = _spriteSheets[(int)HudColors.Theme] ?? _spriteSheets[(int)HudTheme.PixelDungeon];
        if (spriteSheet is not null)
        {
            var cellWidth = spriteSheet.Width / 2;
            var cellHeight = spriteSheet.Height / 2;
            var source = new Rectangle(character.Column * cellWidth, character.Row * cellHeight, cellWidth, cellHeight);
            const int horizontalPadding = 8;
            const int verticalPadding = 6;
            var scale = Math.Min(
                (heroArea.Width - horizontalPadding * 2) / (float)cellWidth,
                (heroArea.Height - verticalPadding * 2) / (float)cellHeight);
            var targetWidth = Math.Max(1, (int)Math.Floor(cellWidth * scale));
            var targetHeight = Math.Max(1, (int)Math.Floor(cellHeight * scale));
            var target = new Rectangle(
                heroArea.X + (heroArea.Width - targetWidth) / 2,
                heroArea.Y + verticalPadding,
                targetWidth,
                targetHeight);
            if (HudColors.Theme == HudTheme.PixelDungeon)
            {
                graphics.DrawImage(spriteSheet, target, source, GraphicsUnit.Pixel);
            }
            else
            {
                graphics.DrawImage(spriteSheet, target, source, GraphicsUnit.Pixel);
            }
        }

        using var shade = new SolidBrush(Color.FromArgb(225, HudColors.Ink));
        graphics.FillRectangle(shade, 7, height - 43, width - 14, 36);
        using var nameFont = PixelArt.CreateMainFont(9f);
        using var classFont = PixelArt.CreateMainFont(6.7f);
        var copy = HudCopy.Hero(character);
        PixelArt.DrawText(graphics, copy.Name, nameFont, new Rectangle(8, height - 41, width - 16, 17), HudColors.Gold, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        PixelArt.DrawText(graphics, copy.Class, classFont, new Rectangle(8, height - 23, width - 16, 13), HudColors.Cream, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        graphics.Restore(state);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var spriteSheet in _spriteSheets) spriteSheet?.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class RpgStatsPanel : Control
{
    private long? _lifetimeTokens;
    private long? _todayTokens;
    private decimal? _staminaPercent;
    private long _experienceBase = DesktopSettings.DefaultExperienceBase;

    internal void SetStats(long? lifetimeTokens, long? todayTokens, decimal? staminaPercent, long experienceBase)
    {
        _lifetimeTokens = lifetimeTokens;
        _todayTokens = todayTokens;
        _staminaPercent = staminaPercent;
        _experienceBase = experienceBase;
        Invalidate();
    }

    internal RpgStatsPanel()
    {
        DoubleBuffered = true;
        BackColor = HudColors.Panel;
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        var graphics = eventArgs.Graphics;
        var state = graphics.Save();
        graphics.ScaleTransform((float)HudScale.Factor, (float)HudScale.Factor);
        var width = HudScale.Logical(Width);
        var height = HudScale.Logical(Height);
        graphics.SmoothingMode = SmoothingMode.None;
        PixelArt.DrawPanel(graphics, new Rectangle(0, 0, width, height), HudColors.Cyan);

        var tokens = Math.Max(0, _lifetimeTokens ?? 0);
        var level = ExperienceProgress.GetLevel(tokens, _experienceBase);
        var progress = ExperienceProgress.GetLevelProgress(tokens, level, _experienceBase);
        using var levelFont = PixelArt.CreateMainFont(height >= 195 ? 14f : 12f);
        using var labelFont = PixelArt.CreateMainFont(7f);
        using var valueFont = PixelArt.CreateMainFont(height >= 195 ? 9f : 8f);

        var compact = height < 195;
        PixelArt.DrawText(graphics, $"{UiText.Level}{level:00}", levelFont, new Rectangle(11, compact ? 7 : 9, width - 22, 28), HudColors.Gold, TextFormatFlags.Left | TextFormatFlags.NoPadding);
        PixelArt.DrawText(graphics, HudCopy.StatusTitle, labelFont, new Rectangle(12, compact ? 32 : 37, width - 24, 14), HudColors.Muted, TextFormatFlags.Left | TextFormatFlags.NoPadding);

        var stamina = Math.Clamp(_staminaPercent ?? 0m, 0m, 100m);
        DrawBar(graphics, HudCopy.Stamina, stamina, $"{stamina:0.#} / 100", compact ? 43 : 56, HudColors.Green, labelFont, width);
        DrawBar(graphics, HudCopy.Experience, progress, $"{progress:0.#}%", compact ? 72 : 91, HudColors.Cyan, labelFont, width);

        var textTop = compact ? 103 : 119;
        PixelArt.DrawText(graphics, HudCopy.Lifetime, labelFont, new Rectangle(12, textTop, width - 24, 13), HudColors.Muted, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        PixelArt.DrawText(graphics, PixelArt.FormatNumber(_lifetimeTokens), valueFont, new Rectangle(12, textTop + 13, width - 24, 20), HudColors.Cream, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        var todayOffset = compact ? 28 : 35;
        PixelArt.DrawText(graphics, HudCopy.Today, labelFont, new Rectangle(12, textTop + todayOffset, width - 24, 13), HudColors.Muted, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        PixelArt.DrawText(graphics, $"+{PixelArt.FormatNumber(_todayTokens)}", valueFont, new Rectangle(12, textTop + todayOffset + 13, width - 24, 20), HudColors.Gold, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        graphics.Restore(state);
    }

    private static void DrawBar(Graphics graphics, string label, decimal percent, string value, int y, Color color, Font font, int width)
    {
        var labelWidth = UiText.IsTraditionalChinese ? 52 : 32;
        var valueLeft = 16 + labelWidth;
        PixelArt.DrawText(graphics, label, font, new Rectangle(12, y, labelWidth, 13), HudColors.Text, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        PixelArt.DrawText(graphics, value, font, new Rectangle(valueLeft, y, width - valueLeft - 12, 13), HudColors.Cream, TextFormatFlags.Right | TextFormatFlags.NoPadding);
        PixelArt.DrawBar(graphics, new Rectangle(12, y + 15, width - 24, 10), percent, color);
    }
}

internal sealed class CompactStaminaBar : Control
{
    private decimal? _staminaPercent;

    internal CompactStaminaBar()
    {
        DoubleBuffered = true;
        BackColor = HudColors.Panel;
    }

    internal void SetStamina(decimal? staminaPercent)
    {
        _staminaPercent = staminaPercent;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        var graphics = eventArgs.Graphics;
        var state = graphics.Save();
        graphics.ScaleTransform((float)HudScale.Factor, (float)HudScale.Factor);
        var width = HudScale.Logical(Width);
        var height = HudScale.Logical(Height);
        graphics.SmoothingMode = SmoothingMode.None;
        PixelArt.DrawPanel(graphics, new Rectangle(0, 0, width, height), HudColors.Green);

        var stamina = Math.Clamp(_staminaPercent ?? 0m, 0m, 100m);
        using var font = PixelArt.CreateMainFont(7f);
        PixelArt.DrawText(graphics, HudCopy.Stamina, font, new Rectangle(11, 7, width / 2, 13), HudColors.Text, TextFormatFlags.Left | TextFormatFlags.NoPadding);
        PixelArt.DrawText(graphics, $"{stamina:0.#} / 100", font, new Rectangle(width / 2, 7, width / 2 - 11, 13), HudColors.Cream, TextFormatFlags.Right | TextFormatFlags.NoPadding);
        PixelArt.DrawBar(graphics, new Rectangle(11, Math.Max(22, height - 18), width - 22, 10), stamina, HudColors.Green);
        graphics.Restore(state);
    }
}

internal sealed class DailyUsageChart : Control
{
    private IReadOnlyList<DailyTokenUsage> _usage = [];
    private int _hoveredIndex = -1;

    internal DailyUsageChart()
    {
        DoubleBuffered = true;
        BackColor = HudColors.Panel;
    }

    internal void SetData(IReadOnlyList<DailyTokenUsage> usage)
    {
        _usage = usage.OrderBy(item => item.Date).TakeLast(7).ToArray();
        _hoveredIndex = -1;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs eventArgs)
    {
        base.OnMouseMove(eventArgs);
        var hoveredIndex = HitTestBar(eventArgs.Location);
        if (_hoveredIndex == hoveredIndex)
        {
            return;
        }

        _hoveredIndex = hoveredIndex;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs eventArgs)
    {
        base.OnMouseLeave(eventArgs);
        if (_hoveredIndex < 0)
        {
            return;
        }

        _hoveredIndex = -1;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        var graphics = eventArgs.Graphics;
        var state = graphics.Save();
        graphics.ScaleTransform((float)HudScale.Factor, (float)HudScale.Factor);
        var width = HudScale.Logical(Width);
        var height = HudScale.Logical(Height);
        graphics.SmoothingMode = SmoothingMode.None;
        PixelArt.DrawPanel(graphics, new Rectangle(0, 0, width, height), HudColors.Grid);
        using var titleFont = PixelArt.CreateMainFont(7f);
        PixelArt.DrawText(graphics, HudCopy.ChartTitle, titleFont, new Rectangle(11, 8, width - 22, 14), HudColors.Gold, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        if (_usage.Count == 0)
        {
            PixelArt.DrawText(graphics, HudCopy.EmptyHistory, titleFont, new Rectangle(0, 0, width, height), HudColors.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            graphics.Restore(state);
            return;
        }

        var maximum = Math.Max(1L, _usage.Max(item => item.Tokens));
        var top = 28;
        var bottom = height - 19;
        var slot = (width - 20f) / 7f;
        using var dayFont = PixelArt.CreateMainFont(6f);
        for (var index = 0; index < 7; index++)
        {
            DailyTokenUsage? item = index < _usage.Count ? _usage[index] : null;
            var bounds = GetBarBounds(index, item, maximum, top, bottom, slot);
            using var bar = new SolidBrush(index == _usage.Count - 1 ? HudColors.Gold : HudColors.Cyan);
            graphics.FillRectangle(bar, bounds);
            if (index == _hoveredIndex)
            {
                using var highlight = new Pen(HudColors.Cream);
                graphics.DrawRectangle(highlight, bounds.X - 1, bounds.Y - 1, bounds.Width + 1, bounds.Height + 1);
            }
            var day = item?.Date.ToString("MM/dd", CultureInfo.InvariantCulture) ?? "--";
            PixelArt.DrawText(graphics, day, dayFont, new Rectangle(bounds.X - 2, bottom + 2, (int)slot, 10), HudColors.Muted, TextFormatFlags.Left | TextFormatFlags.NoPadding);
        }

        if (_hoveredIndex >= 0 && _hoveredIndex < _usage.Count)
        {
            DrawTokenTip(graphics, width, top, bottom, slot, maximum, dayFont);
        }
        graphics.Restore(state);
    }

    private int HitTestBar(Point location)
    {
        if (_usage.Count == 0)
        {
            return -1;
        }

        var width = HudScale.Logical(Width);
        var height = HudScale.Logical(Height);
        var point = new Point(HudScale.Logical(location.X), HudScale.Logical(location.Y));
        var maximum = Math.Max(1L, _usage.Max(item => item.Tokens));
        var top = 28;
        var bottom = height - 19;
        var slot = (width - 20f) / 7f;
        for (var index = 0; index < _usage.Count; index++)
        {
            if (GetBarBounds(index, _usage[index], maximum, top, bottom, slot).Contains(point))
            {
                return index;
            }
        }

        return -1;
    }

    private static Rectangle GetBarBounds(int index, DailyTokenUsage? item, long maximum, int top, int bottom, float slot)
    {
        var barHeight = item is null ? 0 : (int)Math.Round((double)item.Tokens / maximum * (bottom - top - 4));
        var x = 12 + (int)(index * slot);
        return new Rectangle(x, bottom - barHeight, Math.Max(5, (int)slot - 7), barHeight);
    }

    private void DrawTokenTip(Graphics graphics, int width, int top, int bottom, float slot, long maximum, Font font)
    {
        var item = _usage[_hoveredIndex];
        var barBounds = GetBarBounds(_hoveredIndex, item, maximum, top, bottom, slot);
        var text = $"{item.Tokens:N0} {UiText.Tokens}";
        var tipWidth = Math.Min(width - 16, Math.Max(82, text.Length * 6 + 12));
        var tipHeight = 20;
        var tipX = Math.Clamp(barBounds.Left + barBounds.Width / 2 - tipWidth / 2, 8, width - tipWidth - 8);
        var tipY = Math.Max(top + 2, barBounds.Top - tipHeight - 3);
        var tipBounds = new Rectangle(tipX, tipY, tipWidth, tipHeight);
        using var background = new SolidBrush(HudColors.Ink);
        using var border = new Pen(HudColors.Cream);
        graphics.FillRectangle(background, tipBounds);
        graphics.DrawRectangle(border, tipBounds.X, tipBounds.Y, tipBounds.Width - 1, tipBounds.Height - 1);
        PixelArt.DrawText(
            graphics,
            text,
            font,
            new Rectangle(tipBounds.X + 5, tipBounds.Y + 4, tipBounds.Width - 10, tipBounds.Height - 7),
            HudColors.Cream,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }
}

internal static class PixelArt
{
    private const float MainTextScale = 1.1f;

    internal static Font CreateFont(float pointSize, FontStyle style = FontStyle.Bold) =>
        new("Consolas", pointSize * 96f / 72f, style, GraphicsUnit.Pixel);

    internal static Font CreateHudFont(float pointSize, FontStyle style = FontStyle.Bold) =>
        new("Consolas", pointSize * 96f / 72f * (float)HudScale.Factor, style, GraphicsUnit.Pixel);

    internal static Font CreateMainFont(float pointSize, FontStyle style = FontStyle.Bold) =>
        CreateFont(pointSize * MainTextScale, style);

    internal static Font CreateMainHudFont(float pointSize, FontStyle style = FontStyle.Bold) =>
        CreateHudFont(pointSize * MainTextScale, style);

    internal static void DrawText(Graphics graphics, string text, Font logicalFont, Rectangle logicalBounds, Color color, TextFormatFlags flags)
    {
        var state = graphics.Save();
        graphics.ResetTransform();
        using var scaledFont = new Font(
            logicalFont.FontFamily,
            logicalFont.Size * (float)HudScale.Factor,
            logicalFont.Style,
            GraphicsUnit.Pixel);
        var bounds = new Rectangle(
            HudScale.Px(logicalBounds.X),
            HudScale.Px(logicalBounds.Y),
            HudScale.Px(logicalBounds.Width),
            HudScale.Px(logicalBounds.Height));
        TextRenderer.DrawText(graphics, text, scaledFont, bounds, color, flags);
        graphics.Restore(state);
    }

    internal static void DrawPanel(Graphics graphics, Rectangle bounds, Color accent)
    {
        if (bounds.Width < 4 || bounds.Height < 4)
        {
            return;
        }
        var theme = HudColors.Theme;
        using var outer = new Pen(HudColors.Ink, theme == HudTheme.CodeTerminal ? 1f : 3f);
        using var middle = new Pen(accent, theme == HudTheme.ArcaneGlass ? 1f : 2f);
        using var inner = new Pen(Color.FromArgb(theme == HudTheme.GuildLedger ? 150 : 90, HudColors.Cream));
        graphics.DrawRectangle(outer, 1, 1, bounds.Width - 3, bounds.Height - 3);
        graphics.DrawRectangle(middle, 3, 3, bounds.Width - 7, bounds.Height - 7);
        if (theme != HudTheme.CodeTerminal)
        {
            graphics.DrawRectangle(inner, 5, 5, bounds.Width - 11, bounds.Height - 11);
        }
        using var corner = new SolidBrush(accent);
        var cornerSize = theme == HudTheme.ArcaneGlass ? 10 : 6;
        graphics.FillRectangle(corner, 3, 3, cornerSize, 3);
        graphics.FillRectangle(corner, bounds.Width - cornerSize - 3, 3, cornerSize, 3);
        graphics.FillRectangle(corner, 3, bounds.Height - 6, cornerSize, 3);
        graphics.FillRectangle(corner, bounds.Width - cornerSize - 3, bounds.Height - 6, cornerSize, 3);

        if (theme == HudTheme.GuildLedger)
        {
            using var grain = new Pen(Color.FromArgb(28, HudColors.Ink));
            for (var y = 12; y < bounds.Height - 8; y += 17)
            {
                graphics.DrawLine(grain, 8, y, bounds.Width - 9, y);
            }
        }
        else if (theme == HudTheme.CodeTerminal)
        {
            using var scan = new Pen(Color.FromArgb(18, HudColors.Green));
            for (var y = 8; y < bounds.Height - 4; y += 4)
            {
                graphics.DrawLine(scan, 4, y, bounds.Width - 5, y);
            }
        }
    }

    internal static void DrawBar(Graphics graphics, Rectangle bounds, decimal percent, Color color)
    {
        using var outline = new Pen(HudColors.Ink, 2f);
        using var track = new SolidBrush(Color.FromArgb(18, 16, 27));
        graphics.FillRectangle(track, bounds);
        graphics.DrawRectangle(outline, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        var width = (int)Math.Round((bounds.Width - 4) * Math.Clamp(percent, 0m, 100m) / 100m);
        if (width > 0)
        {
            using var fill = new SolidBrush(color);
            graphics.FillRectangle(fill, bounds.X + 2, bounds.Y + 2, width, bounds.Height - 4);
        }
    }

    internal static void DrawDungeonBackdrop(Graphics graphics, Rectangle bounds)
    {
        var state = graphics.Save();
        graphics.SetClip(bounds, CombineMode.Intersect);
        var (distantColor, groundColor, highlightColor) = HudColors.Theme switch
        {
            HudTheme.ArcaneGlass => (Color.FromArgb(67, 48, 130), Color.FromArgb(31, 25, 70), Color.FromArgb(112, 86, 190)),
            HudTheme.GuildLedger => (Color.FromArgb(128, 92, 48), Color.FromArgb(105, 70, 39), Color.FromArgb(190, 148, 88)),
            HudTheme.CodeTerminal => (Color.FromArgb(12, 53, 28), Color.FromArgb(5, 27, 14), Color.FromArgb(30, 101, 51)),
            _ => (Color.FromArgb(49, 57, 87), Color.FromArgb(43, 35, 55), Color.FromArgb(76, 81, 116))
        };
        using var distant = new SolidBrush(distantColor);
        using var ground = new SolidBrush(groundColor);
        using var highlight = new Pen(highlightColor);
        var horizon = bounds.Bottom - Math.Max(17, bounds.Height / 5);
        graphics.FillRectangle(ground, bounds.X, horizon, bounds.Width, bounds.Bottom - horizon);
        for (var x = bounds.X + 5; x < bounds.Right; x += 18)
        {
            var height = 8 + ((x / 18) % 3) * 5;
            graphics.FillRectangle(distant, x, horizon - height, 10, height);
        }
        for (var y = horizon + 6; y < bounds.Bottom; y += 8)
        {
            graphics.DrawLine(highlight, bounds.X, y, bounds.Right, y);
        }
        graphics.Restore(state);
    }

    internal static string FormatNumber(long? value)
    {
        if (value is null)
        {
            return "--";
        }
        return value.Value switch
        {
            >= 1_000_000_000 => $"{value.Value / 1_000_000_000d:0.##}B",
            >= 1_000_000 => $"{value.Value / 1_000_000d:0.##}M",
            >= 1_000 => $"{value.Value / 1_000d:0.##}K",
            _ => value.Value.ToString("N0")
        };
    }
}
