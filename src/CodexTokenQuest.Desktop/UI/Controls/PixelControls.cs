using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Globalization;

namespace CodexTokenQuest.Desktop;

internal static class PixelArt
{
    internal static readonly FontFamily Font = new("Consolas, Menlo, Courier New, DejaVu Sans Mono, Microsoft JhengHei, PingFang TC, sans-serif");
    internal static IBrush Brush(Color color) => new SolidColorBrush(color);
    internal static string Number(long? value) => value switch
    {
        null => "--", >= 1_000_000_000_000 => $"{value / 1_000_000_000_000d:0.##}T",
        >= 1_000_000_000 => $"{value / 1_000_000_000d:0.##}B", >= 1_000_000 => $"{value / 1_000_000d:0.##}M",
        >= 1_000 => $"{value / 1_000d:0.##}K", _ => value.Value.ToString("N0")
    };
    internal static void Text(DrawingContext context, string text, double x, double y, double size, Color color, double maxWidth = 1000)
    {
        var formatted = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(Font, FontStyle.Normal, FontWeight.Bold), size + 1, Brush(color)) { MaxTextWidth = Math.Max(1, maxWidth) };
        context.DrawText(formatted, new(x, y));
    }
    internal static void Panel(DrawingContext c, Rect bounds)
    {
        c.DrawRectangle(Brush(HudColors.Panel), new Pen(Brush(HudColors.Ink), 2), bounds.Deflate(1));
        c.DrawRectangle(null, new Pen(Brush(HudColors.Gold), 1), bounds.Deflate(3));
        if (HudColors.Theme != HudTheme.CodeTerminal)
            c.DrawRectangle(null, new Pen(Brush(HudColors.Grid), 1), bounds.Deflate(5));
        var gap = HudColors.Theme == HudTheme.CodeTerminal ? 4 : 17;
        if (HudColors.Theme is HudTheme.CodeTerminal or HudTheme.GuildLedger)
            for (var y = 10; y < bounds.Height - 6; y += gap)
                c.DrawLine(new Pen(Brush(HudColors.Grid), 0.25), new(7, y), new(bounds.Width - 7, y));
    }
}
internal sealed class PixelFrame : Decorator
{
    public override void Render(DrawingContext context) { PixelArt.Panel(context, new(Bounds.Size)); base.Render(context); }
}
internal sealed class HeroCanvas : Control, IDisposable
{
    private readonly Bitmap?[] _sheets;
    internal int CharacterIndex { get; set; }
    internal int Level { get; set; } = 1;
    internal event Action<int>? Selected;
    internal HeroCanvas()
    {
        _sheets = new[] { "hero-sprite-sheet.png", "hero-sprite-sheet-arcane.png", "hero-sprite-sheet-guild.png", "hero-sprite-sheet-terminal.png" }
            .Select(name => { var path = Path.Combine(AppContext.BaseDirectory, "assets", "rpg", name); return File.Exists(path) ? new Bitmap(path) : null; }).ToArray();
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
        Cursor = new Cursor(StandardCursorType.Hand);
        PointerReleased += (_, e) => Select(e.GetPosition(this).X < Bounds.Width / 2 ? -1 : 1);
        Focusable = true;
        KeyDown += (_, e) => { if (e.Key is Key.Left or Key.Right) { Select(e.Key == Key.Left ? -1 : 1); e.Handled = true; } };
    }
    private void Select(int direction)
    {
        for (var i = 1; i <= Characters.All.Length; i++)
        {
            var next = (CharacterIndex + direction * i + Characters.All.Length * 2) % Characters.All.Length;
            if (Characters.All[next].UnlockLevel <= Level) { Selected?.Invoke(next); break; }
        }
    }
    public override void Render(DrawingContext c)
    {
        PixelArt.Panel(c, new(Bounds.Size));
        var area = new Rect(8, 8, Math.Max(1, Bounds.Width - 16), Math.Max(1, Bounds.Height - 56));
        c.DrawRectangle(PixelArt.Brush(HudColors.Ink), null, area);
        using (c.PushClip(area))
        {
            for (var x = 10; x < area.Width; x += 18)
                c.DrawRectangle(PixelArt.Brush(HudColors.Grid), null, new(x, area.Bottom - 25 - x % 15, 10, 35));
            for (var y = area.Bottom - 17; y < area.Bottom; y += 7)
                c.DrawLine(new Pen(PixelArt.Brush(HudColors.Muted), 0.5), new(area.X, y), new(area.Right, y));
            var sheet = _sheets[(int)HudColors.Theme] ?? _sheets[0];
            var hero = Characters.All[CharacterIndex];
            if (sheet is not null)
            {
                var w = sheet.Size.Width / 2; var h = sheet.Size.Height / 2;
                var scale = Math.Min((area.Width - 12) / w, (area.Height - 8) / h);
                c.DrawImage(sheet, new(hero.Column * w, hero.Row * h, w, h),
                    new(area.Center.X - w * scale / 2, area.Bottom - h * scale - 4, w * scale, h * scale));
            }
        }
    }
    public void Dispose() { foreach (var sheet in _sheets) sheet?.Dispose(); }
}
internal sealed class DailyUsageChart : Control
{
    internal IReadOnlyList<DailyTokenUsage> Data { get; set; } = [];
    internal DailyUsageChart()
    {
        PointerMoved += (_, e) =>
        {
            var slot = (Bounds.Width - 24) / 7;
            var index = slot <= 0 ? -1 : (int)Math.Floor((e.GetPosition(this).X - 12) / slot);
            ToolTip.SetTip(this, index >= 0 && index < Data.Count ? $"{Data[index].Date:yyyy-MM-dd}: {Data[index].Tokens:N0} {UiText.Tokens}" : null);
        };
    }
    public override void Render(DrawingContext c)
    {
        PixelArt.Panel(c, new(Bounds.Size));
        PixelArt.Text(c, HudCopy.ChartTitle, 12, 10, 11, HudColors.Gold, Bounds.Width - 24);
        if (Data.Count == 0) { PixelArt.Text(c, HudCopy.EmptyHistory, 12, 80, 12, HudColors.Muted); return; }
        var max = Math.Max(1, Data.Max(d => d.Tokens)); var slot = (Bounds.Width - 24) / 7;
        var baseline = Bounds.Height - 30; var available = Math.Max(1, baseline - 53);
        for (var i = 0; i < Data.Count && i < 7; i++)
        {
            var h = Math.Max(1, Data[i].Tokens / (double)max * available);
            var x = 12 + i * slot;
            c.DrawRectangle(PixelArt.Brush(i == 6 ? HudColors.Gold : HudColors.Cyan), null, new(x + 4, baseline - h, Math.Max(1, slot - 8), h));
            PixelArt.Text(c, PixelArt.Number(Data[i].Tokens), x, baseline - h - 15, 9, HudColors.Cream, slot);
            PixelArt.Text(c, Data[i].Date.ToString("MM/dd"), x + 1, baseline + 6, 9, HudColors.Muted, slot);
        }
    }
}
