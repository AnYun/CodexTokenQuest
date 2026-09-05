using Avalonia.Controls;

namespace CodexTokenQuest.Desktop;

internal interface IHostWindowTracker : IDisposable
{
    HostState Read();
    void RequestPermission();
}
internal interface IHudWindowIntegration
{
    void Configure(Window window);
    void Attach(Window window, HostState host);
}
internal sealed record DesktopPlatform(IHostWindowTracker Tracker, IHudWindowIntegration Windows)
{
    internal static DesktopPlatform Create()
    {
        if (OperatingSystem.IsWindows()) { var adapter = new WindowsDesktopAdapter(); return new(adapter, adapter); }
        if (OperatingSystem.IsMacOS()) { var adapter = new MacDesktopAdapter(); return new(adapter, adapter); }
        throw new PlatformNotSupportedException("Codex Token Quest supports Windows and Apple Silicon macOS.");
    }
}
