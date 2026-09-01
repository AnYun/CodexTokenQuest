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
        Margin = new Padding(0, 0, 0, 8);
        Size = new Size(348, 70);
        UpdateCountdown(DateTimeOffset.Now);
    }

    internal void UpdateCountdown(DateTimeOffset now)
    {
        if (_bucket.ResetsAt is null)
        {
            _countdown = $"{HudCopy.Reset} // UNKNOWN";
        }
        else
        {
            var local = _bucket.ResetsAt.Value.ToLocalTime();
            var remaining = local - now;
            _countdown = remaining <= TimeSpan.Zero
                ? $"{HudCopy.Reset} {local:MM/dd HH:mm} // SYNCING"
                : $"{HudCopy.Reset} {local:MM/dd HH:mm} // {FormatDuration(remaining)}";
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        var graphics = eventArgs.Graphics;
        graphics.SmoothingMode = SmoothingMode.None;
        var remaining = _bucket.RemainingPercent;
        var accent = remaining switch { <= 10m => HudColors.Red, <= 30m => HudColors.Amber, _ => HudColors.Green };
        PixelArt.DrawPanel(graphics, ClientRectangle, accent);
        using var titleFont = new Font("Consolas", 7f, FontStyle.Bold);
        using var valueFont = new Font("Consolas", 8.5f, FontStyle.Bold);
        var name = $"{(_bucket.Name ?? _bucket.Id).ToUpperInvariant()} [{_bucket.Window}]";
        TextRenderer.DrawText(graphics, name, titleFont, new Rectangle(11, 8, 220, 14), HudColors.Cream, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(graphics, $"{HudCopy.Stamina} {remaining:0.#}", valueFont, new Rectangle(220, 7, 115, 16), accent, TextFormatFlags.Right | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        PixelArt.DrawBar(graphics, new Rectangle(11, 28, Width - 22, 9), remaining, accent);
        TextRenderer.DrawText(graphics, _countdown, titleFont, new Rectangle(11, 45, Width - 22, 14), HudColors.Muted, TextFormatFlags.Left | TextFormatFlags.NoPadding);
    }

    internal static string FormatDuration(TimeSpan duration) => duration.TotalDays >= 1
        ? $"T-{(int)duration.TotalDays}D {duration.Hours:00}H"
        : duration.TotalHours >= 1
            ? $"T-{(int)duration.TotalHours:00}H {duration.Minutes:00}M"
            : $"T-{Math.Max(0, duration.Minutes):00}M {duration.Seconds:00}S";
}
