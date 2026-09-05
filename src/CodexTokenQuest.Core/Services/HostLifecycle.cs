namespace CodexTokenQuest.Core;

// Coordinates are physical pixels on Windows and desktop points on macOS.
// UI conversion to DIPs happens once, at the platform/UI boundary.
internal readonly record struct HostBounds(double X, double Y, double Width, double Height);
internal sealed record HostState(bool Running, bool PermissionGranted, bool Foreground,
    HostBounds? Bounds = null, nint Window = 0, double Scale = 1, bool Reliable = true);
internal enum HostAction { Hide, Show, Exit }

internal sealed class HostLifecycle
{
    private DateTimeOffset? _missingSince;
    internal bool ManuallyHidden { get; set; }
    internal HostAction Update(HostState state, DateTimeOffset now)
    {
        if (!state.Reliable) { _missingSince = null; return HostAction.Hide; }
        if (!state.Running)
        {
            _missingSince ??= now;
            return now - _missingSince >= TimeSpan.FromSeconds(5) ? HostAction.Exit : HostAction.Hide;
        }
        _missingSince = null;
        return !ManuallyHidden && state.PermissionGranted && state.Bounds is not null
            ? HostAction.Show : HostAction.Hide;
    }

    internal static HostBounds Place(HostBounds host, double width, double height, double margin)
    {
        margin = Math.Clamp(margin, 0, Math.Max(0, Math.Min(host.Width, host.Height) / 2 - 1));
        width = Math.Clamp(width, 1, Math.Max(1, host.Width - margin * 2));
        height = Math.Clamp(height, 1, Math.Max(1, host.Height - margin * 2));
        return new(host.X + host.Width - width - margin, host.Y + host.Height - height - margin, width, height);
    }
}
