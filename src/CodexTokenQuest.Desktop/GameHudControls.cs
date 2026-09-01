using System.Drawing.Drawing2D;
using System.Globalization;

namespace CodexTokenQuest.Desktop;

internal static class HudColors
{
    internal static readonly Color Background = Color.FromArgb(18, 16, 29);
    internal static readonly Color Panel = Color.FromArgb(32, 30, 49);
    internal static readonly Color PanelBright = Color.FromArgb(46, 42, 66);
    internal static readonly Color Ink = Color.FromArgb(15, 13, 24);
    internal static readonly Color Cream = Color.FromArgb(255, 238, 196);
    internal static readonly Color Gold = Color.FromArgb(255, 203, 79);
    internal static readonly Color Cyan = Color.FromArgb(91, 224, 255);
    internal static readonly Color Green = Color.FromArgb(92, 224, 112);
    internal static readonly Color Amber = Color.FromArgb(255, 162, 72);
    internal static readonly Color Red = Color.FromArgb(235, 76, 83);
    internal static readonly Color Text = Color.FromArgb(250, 244, 225);
    internal static readonly Color Muted = Color.FromArgb(167, 154, 174);
    internal static readonly Color Grid = Color.FromArgb(92, 76, 112);
}

internal sealed record RpgCharacter(string Name, string ClassName, int UnlockLevel, int Column, int Row);

internal static class RpgProgress
{
    internal const int MaximumLevel = 99;

    internal static int GetLevel(long tokens)
    {
        if (tokens <= 0)
        {
            return 1;
        }

        return Math.Clamp(1 + (int)Math.Floor(10d * Math.Log10(tokens / 1000d + 1d)), 1, MaximumLevel);
    }

    internal static long GetThreshold(int level)
    {
        if (level <= 1)
        {
            return 0;
        }

        var threshold = 1000d * (Math.Pow(10d, (level - 1) / 10d) - 1d);
        return threshold >= long.MaxValue ? long.MaxValue : (long)Math.Round(threshold);
    }

    internal static decimal GetLevelProgress(long tokens, int level)
    {
        if (level >= MaximumLevel)
        {
            return 100m;
        }

        var current = GetThreshold(level);
        var next = GetThreshold(level + 1);
        return next <= current
            ? 100m
            : Math.Clamp((decimal)(tokens - current) / (next - current) * 100m, 0m, 100m);
    }
}

internal sealed class RpgHeroPanel : Control
{
    internal static readonly IReadOnlyList<RpgCharacter> Characters =
    [
        new("AERON", "SWORD WARDEN", 1, 0, 0),
        new("LYRA", "ASTRAL MAGE", 10, 1, 0),
        new("SYLVI", "WILD RANGER", 25, 0, 1),
        new("NOVA", "RUNE KNIGHT", 50, 1, 1)
    ];

    private readonly Image? _spriteSheet;
    private int _characterIndex;
    private int _level = 1;

    internal event EventHandler<int>? CharacterChanged;

    internal int CharacterIndex
    {
        get => _characterIndex;
        set
        {
            _characterIndex = Math.Clamp(value, 0, Characters.Count - 1);
            Invalidate();
        }
    }

    internal int Level
    {
        get => _level;
        set { _level = Math.Clamp(value, 1, RpgProgress.MaximumLevel); Invalidate(); }
    }

    internal RpgHeroPanel()
    {
        DoubleBuffered = true;
        BackColor = HudColors.Panel;
        Cursor = Cursors.Hand;
        var path = Path.Combine(AppContext.BaseDirectory, "assets", "rpg", "hero-sprite-sheet.png");
        if (File.Exists(path))
        {
            using var source = Image.FromFile(path);
            _spriteSheet = new Bitmap(source);
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
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        PixelArt.DrawPanel(graphics, ClientRectangle, HudColors.Gold);

        var heroArea = new Rectangle(7, 7, Width - 14, Math.Max(70, Height - 51));
        using var sky = new SolidBrush(Color.FromArgb(35, 47, 73));
        graphics.FillRectangle(sky, heroArea);
        PixelArt.DrawDungeonBackdrop(graphics, heroArea);

        var character = Characters[_characterIndex];
        if (_spriteSheet is not null)
        {
            var cellWidth = _spriteSheet.Width / 2;
            var cellHeight = _spriteSheet.Height / 2;
            var source = new Rectangle(character.Column * cellWidth, character.Row * cellHeight, cellWidth, cellHeight);
            var inset = Math.Max(3, heroArea.Width / 20);
            var target = new Rectangle(heroArea.X + inset, heroArea.Y + 2, heroArea.Width - inset * 2, heroArea.Height - 3);
            graphics.DrawImage(_spriteSheet, target, source, GraphicsUnit.Pixel);
        }

        using var shade = new SolidBrush(Color.FromArgb(225, HudColors.Ink));
        graphics.FillRectangle(shade, 7, Height - 43, Width - 14, 36);
        using var nameFont = new Font("Consolas", 9f, FontStyle.Bold);
        using var classFont = new Font("Consolas", 6.7f, FontStyle.Bold);
        TextRenderer.DrawText(graphics, $"◀ {character.Name} ▶", nameFont, new Rectangle(8, Height - 41, Width - 16, 17), HudColors.Gold, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(graphics, character.ClassName, classFont, new Rectangle(8, Height - 23, Width - 16, 13), HudColors.Cream, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _spriteSheet?.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class RpgStatsPanel : Control
{
    private long? _lifetimeTokens;
    private long? _todayTokens;
    private decimal? _staminaPercent;

    internal void SetStats(long? lifetimeTokens, long? todayTokens, decimal? staminaPercent)
    {
        _lifetimeTokens = lifetimeTokens;
        _todayTokens = todayTokens;
        _staminaPercent = staminaPercent;
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
        graphics.SmoothingMode = SmoothingMode.None;
        PixelArt.DrawPanel(graphics, ClientRectangle, HudColors.Cyan);

        var tokens = Math.Max(0, _lifetimeTokens ?? 0);
        var level = RpgProgress.GetLevel(tokens);
        var progress = RpgProgress.GetLevelProgress(tokens, level);
        using var levelFont = new Font("Consolas", Height >= 195 ? 17f : 14f, FontStyle.Bold);
        using var labelFont = new Font("Consolas", 7f, FontStyle.Bold);
        using var valueFont = new Font("Consolas", Height >= 195 ? 9f : 8f, FontStyle.Bold);

        var compact = Height < 195;
        TextRenderer.DrawText(graphics, $"LV.{level:00}", levelFont, new Rectangle(11, compact ? 7 : 9, Width - 22, 28), HudColors.Gold, TextFormatFlags.Left | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(graphics, "ADVENTURER STATUS", labelFont, new Rectangle(12, compact ? 32 : 37, Width - 24, 14), HudColors.Muted, TextFormatFlags.Left | TextFormatFlags.NoPadding);

        var stamina = Math.Clamp(_staminaPercent ?? 0m, 0m, 100m);
        DrawBar(graphics, "STA", stamina, $"{stamina:0.#} / 100", compact ? 45 : 56, HudColors.Green, labelFont);
        DrawBar(graphics, "EXP", progress, $"{progress:0.#}%", compact ? 75 : 91, HudColors.Cyan, labelFont);

        var textTop = compact ? 108 : 128;
        TextRenderer.DrawText(graphics, "TOTAL EXP", labelFont, new Rectangle(12, textTop, Width - 24, 13), HudColors.Muted, TextFormatFlags.Left | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(graphics, PixelArt.FormatNumber(_lifetimeTokens), valueFont, new Rectangle(12, textTop + 13, Width - 24, 20), HudColors.Cream, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        var todayOffset = compact ? 29 : 37;
        TextRenderer.DrawText(graphics, "TODAY QUEST EXP", labelFont, new Rectangle(12, textTop + todayOffset, Width - 24, 13), HudColors.Muted, TextFormatFlags.Left | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(graphics, $"+{PixelArt.FormatNumber(_todayTokens)}", valueFont, new Rectangle(12, textTop + todayOffset + 13, Width - 24, 20), HudColors.Gold, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }

    private void DrawBar(Graphics graphics, string label, decimal percent, string value, int y, Color color, Font font)
    {
        TextRenderer.DrawText(graphics, label, font, new Rectangle(12, y, 32, 13), HudColors.Text, TextFormatFlags.Left | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(graphics, value, font, new Rectangle(48, y, Width - 60, 13), HudColors.Cream, TextFormatFlags.Right | TextFormatFlags.NoPadding);
        PixelArt.DrawBar(graphics, new Rectangle(12, y + 15, Width - 24, 10), percent, color);
    }
}

internal sealed class DailyUsageChart : Control
{
    private IReadOnlyList<DailyTokenUsage> _usage = [];

    internal DailyUsageChart()
    {
        DoubleBuffered = true;
        BackColor = HudColors.Panel;
    }

    internal void SetData(IReadOnlyList<DailyTokenUsage> usage)
    {
        _usage = usage.OrderBy(item => item.Date).TakeLast(7).ToArray();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        var graphics = eventArgs.Graphics;
        graphics.SmoothingMode = SmoothingMode.None;
        PixelArt.DrawPanel(graphics, ClientRectangle, HudColors.Grid);
        using var titleFont = new Font("Consolas", 7f, FontStyle.Bold);
        TextRenderer.DrawText(graphics, "QUEST LOG // 7-DAY EXP", titleFont, new Rectangle(11, 8, Width - 22, 14), HudColors.Gold, TextFormatFlags.Left | TextFormatFlags.NoPadding);
        if (_usage.Count == 0)
        {
            TextRenderer.DrawText(graphics, "NO QUEST RECORD", titleFont, ClientRectangle, HudColors.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var maximum = Math.Max(1L, _usage.Max(item => item.Tokens));
        var top = 28;
        var bottom = Height - 19;
        var slot = (Width - 20f) / 7f;
        using var dayFont = new Font("Consolas", 6f, FontStyle.Bold);
        for (var index = 0; index < 7; index++)
        {
            DailyTokenUsage? item = index < _usage.Count ? _usage[index] : null;
            var height = item is null ? 0 : (int)Math.Round((double)item.Tokens / maximum * (bottom - top - 4));
            var x = 12 + (int)(index * slot);
            using var bar = new SolidBrush(index == _usage.Count - 1 ? HudColors.Gold : HudColors.Cyan);
            graphics.FillRectangle(bar, x, bottom - height, Math.Max(5, (int)slot - 7), height);
            var day = item?.Date.ToString("MM/dd", CultureInfo.InvariantCulture) ?? "--";
            TextRenderer.DrawText(graphics, day, dayFont, new Rectangle(x - 2, bottom + 2, (int)slot, 10), HudColors.Muted, TextFormatFlags.Left | TextFormatFlags.NoPadding);
        }
    }
}

internal static class PixelArt
{
    internal static void DrawPanel(Graphics graphics, Rectangle bounds, Color accent)
    {
        if (bounds.Width < 4 || bounds.Height < 4)
        {
            return;
        }
        using var outer = new Pen(HudColors.Ink, 3f);
        using var middle = new Pen(accent, 2f);
        using var inner = new Pen(Color.FromArgb(115, HudColors.Cream));
        graphics.DrawRectangle(outer, 1, 1, bounds.Width - 3, bounds.Height - 3);
        graphics.DrawRectangle(middle, 3, 3, bounds.Width - 7, bounds.Height - 7);
        graphics.DrawRectangle(inner, 5, 5, bounds.Width - 11, bounds.Height - 11);
        using var corner = new SolidBrush(accent);
        graphics.FillRectangle(corner, 3, 3, 6, 3);
        graphics.FillRectangle(corner, bounds.Width - 9, 3, 6, 3);
        graphics.FillRectangle(corner, 3, bounds.Height - 6, 6, 3);
        graphics.FillRectangle(corner, bounds.Width - 9, bounds.Height - 6, 6, 3);
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
        using var distant = new SolidBrush(Color.FromArgb(49, 57, 87));
        using var ground = new SolidBrush(Color.FromArgb(43, 35, 55));
        using var highlight = new Pen(Color.FromArgb(76, 81, 116));
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
