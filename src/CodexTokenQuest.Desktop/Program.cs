using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Styling;

namespace CodexTokenQuest.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (!OperatingSystem.IsWindows() && !(OperatingSystem.IsMacOS() && System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64))
        { Console.Error.WriteLine("Supported platforms: Windows and Apple Silicon macOS."); return; }
        using var lease = InstanceLease.TryAcquire(Path.Combine(AppPaths.StateDirectory, args.Contains("--preview") ? "preview.lock" : "desktop.lock"));
        if (lease is null) return;
        AppPaths.Log("HUD starting.");
        try { BuildApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown); }
        catch (Exception e) { AppPaths.Log($"HUD failed: {e}"); throw; }
    }
    public static AppBuilder BuildApp() => AppBuilder.Configure<QuestApp>().UsePlatformDetect()
        .With(new MacOSPlatformOptions { ShowInDock = false }).LogToTrace();
}
internal sealed class QuestApp : Application
{
    public QuestApp() => Name = "Codex Token Quest";
    public override void Initialize() { RequestedThemeVariant = ThemeVariant.Dark; Styles.Add(new FluentTheme()); }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? [];
            var preview = args.Contains("--preview");
            var settings = preview ? new DesktopSettings() : DesktopSettings.Load();
            string? Option(string name) { var index = Array.IndexOf(args, name); return index >= 0 && index + 1 < args.Length ? args[index + 1] : null; }
            if (preview)
            {
                settings = settings with { Language = Option("--language") ?? "en", ThemeIndex = int.TryParse(Option("--theme"), out var theme) ? theme : 0,
                    SelectedPanel = Option("--panel") ?? "CAMP", MinimizedMode = args.Contains("--compact") };
                settings = settings.Normalize();
            }
            var model = preview ? new UsageViewModel(_ => Task.FromResult(Sample()), _ => 123456) : new UsageViewModel();
            var window = new UsageWindow(DesktopPlatform.Create(), model, preview, settings);
            desktop.ShutdownRequested += (_, e) => { e.Cancel = true; _ = window.ExitAsync(); };
            window.Start();
        }
        base.OnFrameworkInitializationCompleted();
    }
    private static UsageSnapshot Sample() => new(DateTimeOffset.Now,
        [new("codex", "Codex", "PRIMARY", 32, 300, DateTimeOffset.Now.AddHours(2), null, null),
         new("codex", "Codex", "SECONDARY", 44, 10080, DateTimeOffset.Now.AddDays(3), null, null)],
        new(12345678, 234567, null, 4, 7), Enumerable.Range(0, 7).Select(i => new DailyTokenUsage(DateOnly.FromDateTime(DateTime.Today).AddDays(i - 6), (i + 1) * 23456)).ToArray(), 2, null);
}
