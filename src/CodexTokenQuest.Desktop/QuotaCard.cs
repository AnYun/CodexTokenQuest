using System.Drawing.Drawing2D;

namespace CodexTokenQuest.Desktop;

internal sealed class QuotaCard : Control
{
    private readonly RateLimitBucket _bucket;
    private string _countdown = string.Empty;

    internal QuotaCard(RateLimitBucket bucket)
    {
        _bucket = bucket;
        DoubleBuffered = true;
        BackColor = HudColors.Panel;
        Margin = new Padding(0, 0, 0, HudScale.Px(8));
        Size = new Size(HudScale.Px(348), HudScale.Px(70));
        UpdateCountdown(DateTimeOffset.Now);
    }

    internal void UpdateCountdown(DateTimeOffset now)
    {
        if (_bucket.ResetsAt is null)
        {
            _countdown = $"{HudCopy.Reset} // {UiText.Unknown}";
        }
        else
        {
            var local = _bucket.ResetsAt.Value.ToLocalTime();
            var remaining = local - now;
            _countdown = remaining <= TimeSpan.Zero
                ? $"{HudCopy.Reset} {local:MM/dd HH:mm} // {UiText.Syncing}"
                : $"{HudCopy.Reset} {local:MM/dd HH:mm} // {FormatDuration(remaining)}";
        }
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
        var remaining = _bucket.RemainingPercent;
        var accent = remaining switch { <= 10m => HudColors.Red, <= 30m => HudColors.Amber, _ => HudColors.Green };
        PixelArt.DrawPanel(graphics, new Rectangle(0, 0, width, height), accent);
        using var titleFont = PixelArt.CreateMainFont(7f);
        using var valueFont = PixelArt.CreateMainFont(8.5f);
        var name = $"{(_bucket.Name ?? _bucket.Id).ToUpperInvariant()} [{UiText.WindowLabel(_bucket.Window)}]";
        var valueWidth = Math.Min(115, Math.Max(72, width / 3));
        var valueLeft = Math.Max(11, width - valueWidth - 11);
        PixelArt.DrawText(graphics, name, titleFont, new Rectangle(11, 8, Math.Max(1, valueLeft - 15), 14), HudColors.Cream, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        PixelArt.DrawText(graphics, $"{HudCopy.Stamina} {remaining:0.#}", valueFont, new Rectangle(valueLeft, 7, valueWidth, 16), accent, TextFormatFlags.Right | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        PixelArt.DrawBar(graphics, new Rectangle(11, 28, width - 22, 9), remaining, accent);
        PixelArt.DrawText(graphics, _countdown, titleFont, new Rectangle(11, 45, width - 22, 14), HudColors.Muted, TextFormatFlags.Left | TextFormatFlags.NoPadding);
        graphics.Restore(state);
    }

    internal static string FormatDuration(TimeSpan duration) => UiText.IsTraditionalChinese
        ? duration.TotalDays >= 1
            ? $"T-{(int)duration.TotalDays}日 {duration.Hours:00}時"
            : duration.TotalHours >= 1
                ? $"T-{(int)duration.TotalHours:00}時 {duration.Minutes:00}分"
                : $"T-{Math.Max(0, duration.Minutes):00}分 {duration.Seconds:00}秒"
        : duration.TotalDays >= 1
            ? $"T-{(int)duration.TotalDays}D {duration.Hours:00}H"
            : duration.TotalHours >= 1
                ? $"T-{(int)duration.TotalHours:00}H {duration.Minutes:00}M"
                : $"T-{Math.Max(0, duration.Minutes):00}M {duration.Seconds:00}S";
}
