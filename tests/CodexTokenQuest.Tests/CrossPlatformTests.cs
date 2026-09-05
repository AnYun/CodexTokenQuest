using System.Diagnostics;
using System.Text.Json;
using CodexTokenQuest.Core;
using CodexTokenQuest.Desktop;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;

internal static class CrossPlatformTests
{
    internal static void Run(bool render)
    {
        Check(WindowsDesktopAdapter.IsMainWindow(new(0, 0, 1600, 1200), 2.5, "Chrome_WidgetWin_1", 0, 0), "Main host accepted at mixed DPI");
        Check(!WindowsDesktopAdapter.IsMainWindow(new(0, 0, 1000, 800), 2.5, "Chrome_WidgetWin_1", 0, 0), "High-DPI popup is not mistaken for a main host");
        foreach (var (name, style, owner) in new[] { ("#32768", 0L, (nint)0), ("tooltips_class32", 0L, (nint)0), ("Chrome_WidgetWin_1", 0x80L, (nint)0), ("Chrome_WidgetWin_1", 0L, (nint)123) })
            Check(!WindowsDesktopAdapter.IsMainWindow(new(0, 0, 2000, 1500), 1, name, style, owner), "Large menus, tool windows and owned popups are excluded");
        var now = DateTimeOffset.UtcNow;
        var lifecycle = new HostLifecycle();
        var visible = new HostState(true, true, true, new(100, 200, 1000, 800));
        Check(lifecycle.Update(visible, now) == HostAction.Show, "Active host shows HUD");
        Check(lifecycle.Update(visible with { Foreground = false }, now) == HostAction.Show, "Inactive but visible host keeps HUD");
        Check(lifecycle.Update(visible with { PermissionGranted = false }, now.AddHours(1)) == HostAction.Hide, "Denied permission waits without exiting");
        Check(lifecycle.Update(visible, now.AddHours(1)) == HostAction.Show, "Restored permission resumes HUD");
        Check(lifecycle.Update(visible with { Foreground = false }, now.AddMinutes(10)) == HostAction.Show, "Inactive host does not expire on a hide timer");
        Check(lifecycle.Update(visible with { Foreground = false, Bounds = null }, now) == HostAction.Hide, "Minimized, hidden or off-desktop host hides HUD even when inactive");
        lifecycle.ManuallyHidden = true;
        Check(lifecycle.Update(visible, now) == HostAction.Hide, "Manual hide persists"); lifecycle.ManuallyHidden = false;
        Check(lifecycle.Update(new(false, true, false), now) == HostAction.Hide, "Missing host starts grace period");
        Check(lifecycle.Update(new(false, true, false), now.AddSeconds(4.99)) == HostAction.Hide, "No premature exit");
        Check(lifecycle.Update(new(false, true, false), now.AddSeconds(5)) == HostAction.Exit, "Exit after five seconds");
        Check(lifecycle.Update(visible with { Bounds = null }, now.AddSeconds(6)) == HostAction.Hide, "Running without a window waits");
        Check(lifecycle.Update(new(false, true, false), now.AddSeconds(7)) == HostAction.Hide, "Reappearing process resets grace");
        Check(lifecycle.Update(new(true, true, false, Reliable: false), now.AddSeconds(20)) == HostAction.Hide, "Observation errors do not terminate HUD");
        var placement = HostLifecycle.Place(new(-1600, -200, 1200, 900), 392, 350, 16);
        Check(placement == new HostBounds(-808, 334, 392, 350), "Negative monitor coordinates preserved");
        placement = HostLifecycle.Place(new(0, 0, 100, 80), 1200, 900, 100);
        Check(placement.Width > 0 && placement.Height > 0 && placement.X >= 0 && placement.Y >= 0
            && placement.X + placement.Width <= 100 && placement.Y + placement.Height <= 80, "Tiny host clamps within bounds");
        var dpi = HostLifecycle.Place(new(0, 0, 2000, 1600), 784, 700, 32);
        Check(dpi.Width / 2 == 392 && dpi.X == 1184, "Mixed DPI uses host physical scale once");
        var old = DesktopSettings.Deserialize("""{"RefreshMinutes":15,"HudScalePercent":150,"Margin":8,"ExperienceBase":1000000,"OpacityPercent":75,"Language":"zh-Hant","MinimizedMode":true,"CharacterIndex":2,"ThemeIndex":3,"SelectedPanel":"HISTORY"}""");
        Check(old.RefreshMinutes == 15 && old.HudScalePercent == 188 && old.MinimizedMode && old.ThemeIndex == 3 && old.SelectedPanel == "HISTORY", "Windows settings remain compatible with rebased scale");
        var rebased = DesktopSettings.Deserialize("""{"HudScalePercent":80}""");
        Check(rebased.HudScalePercent == 100 && rebased.HudScale == new DesktopSettings().HudScale,
            "Saved 80% matches the new default 100% size");
        Check(DesktopSettings.Deserialize(JsonSerializer.Serialize(rebased)) == rebased,
            "Saving and reloading does not apply the scale conversion twice");
        var invalid = (old with { RefreshMinutes = 0, CharacterIndex = 42, ThemeIndex = -1, SelectedPanel = "bogus" }).Normalize();
        Check(invalid.RefreshMinutes == 1 && invalid.CharacterIndex == 3 && invalid.ThemeIndex == 0 && invalid.SelectedPanel == "CAMP", "Settings normalize corrupted values");
        Check(CodexExecutableResolver.Resolve("/one:/two space", "/home/me", false, p => p == Path.Combine("/two space", "codex")) == Path.Combine("/two space", "codex"), "PATH handles spaces");
        Check(CodexExecutableResolver.Resolve("/one", "/home/me", false, p => p == Path.Combine("/opt/homebrew/bin", "codex")) == Path.Combine("/opt/homebrew/bin", "codex"), "GUI launch finds Homebrew CLI");
        Check(CodexExecutableResolver.Resolve("/one", "/home/me", false, _ => false) is null, "Missing CLI reported");
        var pathCli = Path.Combine("/one", "codex");
        var bundledCli = Path.Combine("/Applications/ChatGPT.app/Contents/Resources", "codex");
        var candidates = CodexExecutableResolver.ResolveCandidates("/one:/one", "/home/me", false,
            p => p == pathCli || p == bundledCli);
        Check(candidates.SequenceEqual(new[] { pathCli, bundledCli }),
            "CLI alternatives preserve PATH priority, remove duplicates and include bundled CLI");
        var winPath = Path.Combine("C:/Program Files/Codex", "codex.cmd");
        Check(CodexExecutableResolver.Resolve("C:/Program Files/Codex;C:/bin", "C:/Users/test", true, p => p == winPath) == winPath, "Windows npm command shim resolved");

        var temporary = Path.Combine(Path.GetTempPath(), "token-quest-tests-" + Guid.NewGuid()); Directory.CreateDirectory(temporary);
        try
        {
            var lockPath = Path.Combine(temporary, "instance.lock");
            using (var first = InstanceLease.TryAcquire(lockPath))
            { Check(first is not null, "First launcher acquires lease"); using var second = InstanceLease.TryAcquire(lockPath); Check(second is null, "Concurrent launcher cannot acquire lease"); }
            using (var next = InstanceLease.TryAcquire(lockPath)) Check(next is not null, "Lease reusable after exit");
            using (var child = Child("--hold-lease", lockPath))
            {
                Check(child.StandardOutput.ReadLine() == "READY", "Child holds interprocess lease");
                using var competing = InstanceLease.TryAcquire(lockPath); Check(competing is null, "Lease excludes another process");
                child.Kill(true); child.WaitForExit();
            }
            using (var recovered = InstanceLease.TryAcquire(lockPath)) Check(recovered is not null, "Crashed process lease recovered");
            Directory.CreateDirectory(Path.Combine(temporary, "src", "bin")); Directory.CreateDirectory(Path.Combine(temporary, "assets"));
            File.WriteAllText(Path.Combine(temporary, "src", "example.cs"), "source");
            File.WriteAllText(Path.Combine(temporary, "assets", "sprite.png"), "image");
            var fingerprint = BuildFingerprint.Calculate(temporary);
            File.WriteAllText(Path.Combine(temporary, "src", "bin", "build.dll"), "ignored");
            Check(fingerprint == BuildFingerprint.Calculate(temporary), "Build output does not invalidate source fingerprint");
            File.WriteAllText(Path.Combine(temporary, "assets", "sprite.png"), "new image");
            Check(fingerprint != BuildFingerprint.Calculate(temporary), "Asset changes trigger rebuild");
        }
        finally { Directory.Delete(temporary, true); }
        RunUsageTests().GetAwaiter().GetResult();
        RenderViews(render);
        Console.WriteLine("Cross-platform lifecycle, positioning, settings, resolver, build fingerprint, leases and usage tests passed.");
    }

    private static async Task RunUsageTests()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var snapshot = new UsageSnapshot(DateTimeOffset.Now,
            [new("codex", "Codex", "PRIMARY", 30, 300, DateTimeOffset.Now.AddHours(1), null, null), new("codex", "Codex", "SECONDARY", 40, 10080, DateTimeOffset.Now.AddDays(3), null, null)],
            new(1234567, null, null, null, null), [new(today, 100)], 2, null);
        var fail = false; var reads = 0;
        await using var vm = new UsageViewModel(async token => { reads++; await Task.Delay(5, token); if (fail) throw new IOException("offline"); return snapshot; }, _ => 200);
        await Task.WhenAll(vm.RefreshAsync(), vm.RefreshAsync());
        Check(reads == 1, "Refresh requests serialized"); Check(vm.TodayTokens == 200, "Local tokens supplement delayed API");
        Check(vm.Stamina == 60 && vm.History.Count == 7, "Weekly stamina and seven-day history");
        fail = true; await vm.RefreshAsync();
        Check(vm.Error == "offline" && vm.Snapshot == snapshot, "Refresh failure marks retained data stale");
        fail = false; await vm.RefreshAsync(); Check(vm.Error is null, "Refresh recovers after failure");
        HudColors.SetTheme(HudTheme.PixelDungeon);
        var resetAt = new DateTimeOffset(2026, 9, 7, 11, 11, 0, TimeSpan.FromHours(8));
        foreach (var language in new[] { "en", "zh-Hant" })
        {
            UiText.SetLanguage(language);
            var prefix = language == "en" ? "◆ NEXT RESET" : "◆ 下次 重設";
            foreach (var (remaining, english, chinese) in new[]
            {
                (new TimeSpan(4, 21, 0, 0), "T-4D 21H", "T-4日 21時"),
                (TimeSpan.FromDays(1), "T-1D 00H", "T-1日 00時"),
                (new TimeSpan(23, 59, 0), "T-23H 59M", "T-23時 59分"),
                (TimeSpan.FromHours(1), "T-01H 00M", "T-01時 00分"),
                (new TimeSpan(0, 59, 59), "T-59M 59S", "T-59分 59秒"),
                (TimeSpan.FromSeconds(1), "T-00M 01S", "T-00分 01秒"),
                (TimeSpan.Zero, "SYNCING", "同步中"),
                (TimeSpan.FromSeconds(-1), "SYNCING", "同步中")
            })
            {
                vm.Tick(resetAt - remaining);
                var countdown = language == "en" ? english : chinese;
                Check(vm.ResetText(resetAt) == $"{prefix} // {resetAt.ToLocalTime():MM/dd HH:mm} // {countdown}",
                    $"{language}: complete reset label and countdown at {remaining}");
            }
            Check(vm.ResetText(null) == $"{prefix} // {UiText.Unknown}", "Missing reset retains its label");
        }
        UiText.SetLanguage("en"); vm.Tick(DateTimeOffset.Now.AddDays(10));
        Check(vm.ResetText(snapshot.RateLimits[0].ResetsAt).Contains("SYNCING"), "Expired reset never shows negative countdown");
        using var token = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        foreach (var mode in new[] { "--fake-codex", "--fake-codex-legacy" })
        {
            var log = Path.GetTempFileName();
            try
            {
                await using var client = await CodexAppServerClient.StartAsync(token.Token, ChildInfo(mode, log));
                var actual = await client.ReadSnapshotAsync(token.Token);
                var next = await client.ReadSnapshotAsync(token.Token);
                Check(actual.RateLimits.Count == 1 && actual.UsageUnsupported && next.UsageUnsupported,
                    "Unsupported optional usage keeps quota refresh working");
                Check(File.ReadLines(log).Count(m => m == "account/usage/read") == 1,
                    "Unsupported method is not retried for the same server process");
                Check(File.ReadLines(log).Count(m => m == "account/rateLimits/read") == 2,
                    "Rate limits keep refreshing after usage capability failure");
                await using var partial = new UsageViewModel(_ => Task.FromResult(actual), _ => 1234);
                await partial.RefreshAsync();
                Check(partial.Error is null && partial.TodayTokens == 1234 && partial.Stamina == 75,
                    "Unsupported usage retains local tokens and live quota");
                foreach (var language in new[] { "en", "zh-Hant" })
                {
                    UiText.SetLanguage(language);
                    Check(partial.Notice is { Length: < 90 } && !partial.Notice.Contains("account/usage/read"),
                        "Optional usage warning is localized and does not expose protocol details");
                }
            }
            finally { File.Delete(log); }
        }
        await using var transient = await CodexAppServerClient.StartAsync(token.Token, ChildInfo("--fake-codex-transient"));
        var failed = await transient.ReadSnapshotAsync(token.Token);
        var recovered = await transient.ReadSnapshotAsync(token.Token);
        Check(failed.Warning is not null && !failed.UsageUnsupported && recovered.Warning is null && recovered.Tokens?.LifetimeTokens == 1234567,
            "Transient Invalid Request is retried and usage can recover");
        await using var supported = await CodexAppServerClient.StartAsync(token.Token, ChildInfo("--fake-codex-supported"));
        Check((await supported.ReadSnapshotAsync(token.Token)).Tokens?.LifetimeTokens == 1234567,
            "Supported usage endpoint still provides lifetime totals");
        await using var auth = await CodexAppServerClient.StartAsync(token.Token, ChildInfo("--fake-codex-auth"));
        try { await auth.ReadSnapshotAsync(token.Token); throw new Exception("Auth failure expected"); }
        catch (CodexAppServerException e) { Check(e.Message.Contains("codex login"), "Auth failure explains sign-in"); }

        var starts = new int[3];
        CodexServerSource Source(int index, string mode) => new(mode, ct =>
        { starts[index]++; return CodexAppServerClient.StartAsync(ct, ChildInfo(mode)); });
        await using (var reader = new CodexUsageReader([Source(0, "--fake-codex-legacy"), Source(1, "--fake-codex-supported"), Source(2, "--fake-codex-supported")], _ => { }))
        {
            var full = await reader.ReadSnapshotAsync(token.Token);
            var again = await reader.ReadSnapshotAsync(token.Token);
            Check(full.Tokens?.LifetimeTokens == 1234567 && !full.UsageUnsupported && full.Warning is null && again.Tokens == full.Tokens,
                "Incompatible PATH server falls back to an installed CLI with lifetime usage");
            Check(starts.SequenceEqual(new[] { 1, 1, 0 }), "Compatible CLI is reused without repeated probing");
        }
        Array.Clear(starts);
        await using (var reader = new CodexUsageReader([Source(0, "--fake-codex-supported"), Source(1, "--fake-codex-supported")], _ => { }))
        {
            await reader.ReadSnapshotAsync(token.Token);
            Check(starts.SequenceEqual(new[] { 1, 0, 0 }), "Compatible PATH selection remains authoritative");
        }
        Array.Clear(starts);
        await using (var reader = new CodexUsageReader([Source(0, "--fake-codex-legacy"), Source(1, "--fake-codex")], _ => { }))
        {
            await reader.ReadSnapshotAsync(token.Token);
            var partial = await reader.ReadSnapshotAsync(token.Token);
            Check(partial.UsageUnsupported && partial.RateLimits.Count == 1 && starts.SequenceEqual(new[] { 1, 1, 0 }),
                "If all installations lack usage support, retain quota without reprobe loops");
        }
        Array.Clear(starts);
        await using (var reader = new CodexUsageReader([Source(0, "--fake-codex-auth"), Source(1, "--fake-codex-supported")], _ => { }))
        {
            try { await reader.ReadSnapshotAsync(token.Token); throw new Exception("Auth failure expected"); }
            catch (CodexAppServerException) { Check(starts[1] == 0, "Primary sign-in errors are not bypassed by switching accounts"); }
        }
        Array.Clear(starts);
        await using (var reader = new CodexUsageReader([Source(0, "--fake-codex-legacy"),
            new("broken", _ => throw new IOException("cannot launch")), Source(2, "--fake-codex-supported")], _ => { }))
        {
            Check((await reader.ReadSnapshotAsync(token.Token)).Tokens?.LifetimeTokens == 1234567,
                "One broken installation does not prevent a working compatible fallback");
        }
        var attempts = 0;
        await using (var reader = new CodexUsageReader([Source(0, "--fake-codex-legacy"),
            new("recovering", ct => CodexAppServerClient.StartAsync(ct,
                ChildInfo(++attempts == 1 ? "--fake-codex-transient" : "--fake-codex-supported")))], _ => { }))
        {
            Check((await reader.ReadSnapshotAsync(token.Token)).UsageUnsupported,
                "Quota survives a temporarily unavailable alternative");
            Check((await reader.ReadSnapshotAsync(token.Token)).Tokens?.LifetimeTokens == 1234567,
                "An alternative that temporarily failed can recover on the next refresh");
        }
    }
    internal static ProcessStartInfo ChildInfo(params string[] arguments)
    {
        var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
        var start = new ProcessStartInfo(host) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(CrossPlatformTests).Assembly.Location);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return start;
    }
    private static Process Child(params string[] arguments) => Process.Start(ChildInfo(arguments))!;
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private static void RenderViews(bool render)
    {
        AppBuilder.Configure<QuestApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
        var font = new Typeface(PixelArt.Font, FontStyle.Normal, FontWeight.Bold).GlyphTypeface;
        Console.WriteLine($"HUD font resolved to {font.FamilyName} {font.Weight}.");
        var resetOnce = true;
        var recoveringPlatform = new FakePlatform { OnConfigure = window =>
        {
            if (!resetOnce) return;
            resetOnce = false; window.Width = 0; window.Height = 0;
        } };
        var recovering = new UsageWindow(new(recoveringPlatform, recoveringPlatform),
            new UsageViewModel(_ => throw new InvalidOperationException("No live reads in layout test"), _ => 0), settings: new DesktopSettings());
        Dispatcher.UIThread.RunJobs(); recovering.UpdateLayout();
        Check(recovering.IsVisible && Math.Abs(recovering.Width - 329.6) < 0.5 && recovering.Height == 296
            && recovering.ClientSize.Width > 0 && recovering.ClientSize.Height > 0,
            "First-show native resize cannot leave the HUD at zero size");
        recovering.ExitAsync().GetAwaiter().GetResult();
        var folder = Path.GetFullPath("artifacts/qa"); if (render) Directory.CreateDirectory(folder);
        foreach (var language in new[] { "en", "zh-Hant" })
        foreach (var theme in Enumerable.Range(0, 4))
        foreach (var panel in new[] { "CAMP", "QUESTS", "HISTORY", "COMPACT" })
        foreach (var state in new[] { "ready", "unsupported", "partial", "failed" })
        {
            var fake = new FakePlatform();
            var diagnostic = string.Concat(Enumerable.Repeat("account/usage/read failed: Invalid request; ", 200));
            var model = new UsageViewModel(_ => state == "failed" ? Task.FromException<UsageSnapshot>(new IOException(diagnostic)) : Task.FromResult(new UsageSnapshot(DateTimeOffset.Now,
                [new("codex", "Codex", "SECONDARY", 42, 10080, DateTimeOffset.Now.AddDays(2), null, null)],
                state == "ready" ? new(12345678, null, null, null, null) : null, [], 2,
                state == "ready" ? null : diagnostic) { UsageUnsupported = state == "unsupported" }), _ => 12345);
            var window = new UsageWindow(new(fake, fake), model, true, new DesktopSettings { Language = language, ThemeIndex = theme,
                SelectedPanel = panel == "COMPACT" ? "CAMP" : panel, MinimizedMode = panel == "COMPACT" });
            window.Show();
            var refresh = model.RefreshAsync();
            while (!refresh.IsCompleted) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(1); }
            refresh.GetAwaiter().GetResult(); Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var notice = window.GetVisualDescendants().OfType<TextBlock>().Single(c => c.Name == "UsageNotice");
            var footer = window.GetVisualDescendants().OfType<Button>().Single(c => c.Name == "HudFooter");
            var reset = window.GetVisualDescendants().OfType<TextBlock>().Single(c => c.Name == "NextReset");
            Check(reset.TextLayout.TextLines.All(line => !line.HasCollapsed),
                $"{language}/{theme}/{panel}/{state}: full reset label fits without ellipsis");
            if (state != "ready")
            {
                Check(notice.IsVisible && notice.Bounds.Height <= 16 && notice.Text?.Length < 90,
                    $"{language}/{theme}/{panel}/{state}: long diagnostics cannot expand the notice");
                var noticeBottom = notice.TranslatePoint(new Point(0, notice.Bounds.Height), window)!.Value.Y;
                var footerTop = footer.TranslatePoint(default, window)!.Value.Y;
                Check(noticeBottom <= footerTop, $"{panel}/{state}: notice cannot overlap footer");
            }
            if (panel == "CAMP")
            {
                var hero = window.GetVisualDescendants().OfType<HeroCanvas>().Single();
                Check(hero.Bounds.Height >= 180, $"{state}: warning cannot collapse hero panel");
                var heroBottom = hero.TranslatePoint(new Point(0, hero.Bounds.Height), window)!.Value.Y;
                Check(!notice.IsVisible || heroBottom <= notice.TranslatePoint(default, window)!.Value.Y,
                    $"{state}: hero does not overlap notice");
                var today = window.GetVisualDescendants().OfType<TextBlock>().Single(c => c.Name == "TodayTokens");
                var statsFrame = today.GetVisualAncestors().OfType<PixelFrame>().First();
                Check(today.TranslatePoint(new Point(0, today.Bounds.Height), statsFrame)!.Value.Y <= statsFrame.Bounds.Height - 6,
                    $"{language}/{theme}/{state}: today's tokens stay inside their panel");
            }
            if (render)
            {
                using var bitmap = new RenderTargetBitmap(new((int)window.Width * 2, (int)window.Height * 2), new(192, 192));
                bitmap.Render(window); bitmap.Save(Path.Combine(folder, $"{language}-{theme}-{panel}{(state == "ready" ? "" : "-" + state)}.png"), PngBitmapEncoderOptions.Default);
            }
            var exit = window.ExitAsync(); while (!exit.IsCompleted) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(1); } exit.GetAwaiter().GetResult();
        }
        foreach (var language in new[] { "en", "zh-Hant" })
        foreach (var theme in Enumerable.Range(0, 4))
        {
            UiText.SetLanguage(language); HudColors.SetTheme((HudTheme)theme);
            DesktopSettings? saved = null;
            var options = new SettingsForm(new DesktopSettings { Language = language, ThemeIndex = theme }, value => saved = value);
            options.Show(); Dispatcher.UIThread.RunJobs(); options.UpdateLayout();
            var input = options.GetVisualDescendants().OfType<NumericUpDown>().Single(c => c.Name == "RefreshMinutes");
            input.Value = 23;
            HudColors.SetTheme((HudTheme)((theme + 1) % 4)); options.ApplyTheme();
            Check(input.Value == 23 && saved is null, "Theme changes preserve unsaved settings");
            HudColors.SetTheme((HudTheme)theme); options.ApplyTheme();
            Dispatcher.UIThread.RunJobs(); options.UpdateLayout();
            var save = options.GetVisualDescendants().OfType<Button>().Single(c => c.Name == "SaveSettings");
            var languageButton = options.GetVisualDescendants().OfType<Button>().Single(c => c.Name == "SettingsLanguage");
            var viewport = languageButton.GetVisualAncestors().OfType<ScrollViewer>().First();
            Check(languageButton.TranslatePoint(new Point(0, languageButton.Bounds.Height), viewport)!.Value.Y <= viewport.Bounds.Height,
                "All settings fields fit at the default window size in both languages");
            Check(save.TranslatePoint(new Point(0, save.Bounds.Height), options)!.Value.Y <= options.ClientSize.Height,
                "Settings save action stays visible outside the scrolling content");
            if (render)
            {
                using var bitmap = new RenderTargetBitmap(new((int)options.Width * 2, (int)options.Height * 2), new(192, 192));
                bitmap.Render(options); bitmap.Save(Path.Combine(folder, $"{language}-{theme}-SETTINGS.png"), PngBitmapEncoderOptions.Default);
            }
            save.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Check(saved?.RefreshMinutes == 23 && saved.Language == language, "Styled settings save edited values and language");
        }
        Console.WriteLine("Validated 8 themed settings views, live theme changes and saving.");
        Console.WriteLine($"Validated 128 HUD theme/language/panel/status combinations{(render ? $"; rendered to {folder}" : "")}");
    }
    private sealed class FakePlatform : IHostWindowTracker, IHudWindowIntegration
    {
        internal Action<Window>? OnConfigure { get; init; }
        public HostState Read() => new(true, true, true, new(0, 0, 1000, 1000));
        public void RequestPermission() { } public void Dispose() { }
        public void Configure(Window window) => OnConfigure?.Invoke(window);
        public void Attach(Window window, HostState host) { }
    }
}
