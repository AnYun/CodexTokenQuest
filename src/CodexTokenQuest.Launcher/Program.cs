using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using CodexTokenQuest.Core;

if (args.Length < 1 || !Directory.Exists(args[0])) return 2;
var preview = args.Skip(1).Contains("--preview");
var lockName = preview ? "preview.lock" : "desktop.lock";
var root = Path.GetFullPath(args[0]);
using var launchLease = InstanceLease.TryAcquire(Path.Combine(AppPaths.StateDirectory, "launch.lock"));
if (launchLease is null) return 0;
try
{
    foreach (var instance in new[] { "desktop.lock", "preview.lock" })
    {
        using var running = InstanceLease.TryAcquire(Path.Combine(AppPaths.StateDirectory, instance));
        if (running is null) { AppPaths.Log("HUD already running."); return 0; }
    }
    if (!OperatingSystem.IsWindows() && !(OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64))
        throw new PlatformNotSupportedException("Supported platforms: Windows and Apple Silicon macOS.");
    var sdk = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? Environment.ProcessPath
        ?? throw new InvalidOperationException("dotnet host path unavailable.");
    if (!Path.GetFileNameWithoutExtension(sdk).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        sdk = Path.Combine(Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..")), OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
    sdk = new FileInfo(sdk).ResolveLinkTarget(true)?.FullName ?? sdk;
    Environment.SetEnvironmentVariable("DOTNET_ROOT", Path.GetDirectoryName(sdk));
    var output = Path.Combine(root, "artifacts", "desktop", RuntimeInformation.RuntimeIdentifier);
    var app = Path.Combine(AppPaths.StateDirectory, "Codex Token Quest.app");
    var install = OperatingSystem.IsMacOS() ? Path.Combine(app, "Contents", "MacOS") : output;
    var executable = Path.Combine(install, OperatingSystem.IsWindows() ? "CodexTokenQuest.Desktop.exe" : "CodexTokenQuest.Desktop");
    var fingerprint = BuildFingerprint.Calculate(root) + RuntimeInformation.RuntimeIdentifier + sdk;
    var stamp = Path.Combine(install, ".source-fingerprint");
    if (!File.Exists(executable) || !File.Exists(stamp) || File.ReadAllText(stamp) != fingerprint)
    {
        AppPaths.Log("Building shared desktop application.");
        await Run(sdk, ["build", Path.Combine(root, "src", "CodexTokenQuest.Desktop", "CodexTokenQuest.Desktop.csproj"), "-c", "Release", "-o", output,
            "--artifacts-path", Path.Combine(root, "artifacts", "build", RuntimeInformation.RuntimeIdentifier), "--nologo", "-p:UseSharedCompilation=false"]);
        if (OperatingSystem.IsMacOS())
        {
            // Fixed bundle identity and location give Accessibility a stable application.
            // Only generated files in this application's own bundle are replaced.
            Directory.CreateDirectory(install);
            foreach (var file in Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories))
            {
                var destination = Path.Combine(install, Path.GetRelativePath(output, file));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(file, destination, true);
                File.SetUnixFileMode(destination, File.GetUnixFileMode(file));
            }
            var resources = Path.Combine(app, "Contents", "Resources"); Directory.CreateDirectory(resources);
            File.Copy(Path.Combine(root, "assets", "icons", "plugin-icon.png"), Path.Combine(resources, "icon.png"), true);
            File.WriteAllText(Path.Combine(app, "Contents", "Info.plist"), $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                <plist version="1.0"><dict>
                  <key>CFBundleExecutable</key><string>CodexTokenQuest.Desktop</string>
                  <key>CFBundleIdentifier</key><string>com.anyun.codex-token-quest</string>
                  <key>CFBundleName</key><string>Codex Token Quest</string>
                  <key>CFBundleDisplayName</key><string>Codex Token Quest</string>
                  <key>CFBundlePackageType</key><string>APPL</string>
                  <key>CFBundleVersion</key><string>2.0.0</string>
                  <key>LSUIElement</key><true/>
                  <key>NSHighResolutionCapable</key><true/>
                  <key>LSEnvironment</key><dict><key>DOTNET_ROOT</key><string>{SecurityElement.Escape(Path.GetDirectoryName(sdk))}</string></dict>
                </dict></plist>
                """);
            // Local ad-hoc signing is necessary for the generated Apple Silicon bundle.
            // This is not Developer ID signing or notarization.
            File.WriteAllText(stamp, fingerprint);
            await Run("/usr/bin/codesign", ["--force", "--deep", "--sign", "-", app]);
        }
        else File.WriteAllText(stamp, fingerprint);
    }
    AppPaths.Log($"Launching HUD: {executable}");
    if (OperatingSystem.IsMacOS()) await Run("/usr/bin/open", ["-g", app, "--args", ..args.Skip(1)]);
    else
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = root };
        foreach (var argument in args.Skip(1)) start.ArgumentList.Add(argument);
        Process.Start(start);
    }
    // Keep the launch lease until the HUD has taken its own lease. A rapid second
    // Hook must not launch again during application initialization.
    for (var attempt = 0; attempt < 100; attempt++)
    {
        await Task.Delay(100);
        using var probe = InstanceLease.TryAcquire(Path.Combine(AppPaths.StateDirectory, lockName));
        if (probe is null) return 0;
    }
    throw new InvalidOperationException("HUD did not become ready within ten seconds. See lifecycle.log.");
}
catch (Exception e) { AppPaths.Log($"Launch failed: {e.Message}"); Console.Error.WriteLine(e.Message); return 1; }

async Task Run(string executable, string[] arguments)
{
    var info = new ProcessStartInfo(executable) { WorkingDirectory = root, UseShellExecute = false,
        CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var argument in arguments) info.ArgumentList.Add(argument);
    using var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {executable}");
    var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
    using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10));
    try { await process.WaitForExitAsync(timeout.Token); }
    catch (OperationCanceledException) { process.Kill(true); throw new TimeoutException($"{Path.GetFileName(executable)} timed out."); }
    var output = (await stdout) + (await stderr);
    if (process.ExitCode != 0) throw new InvalidOperationException(output);
}
