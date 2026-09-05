using System.Diagnostics;

namespace CodexTokenQuest.Core;

internal static class CodexExecutableResolver
{
    internal static string? Resolve(string? path = null, string? home = null, bool? windows = null,
        Func<string, bool>? exists = null)
        => ResolveCandidates(path, home, windows, exists).FirstOrDefault();

    internal static IReadOnlyList<string> ResolveCandidates(string? path = null, string? home = null, bool? windows = null,
        Func<string, bool>? exists = null)
    {
        var isWindows = windows ?? OperatingSystem.IsWindows();
        exists ??= IsExecutable;
        home ??= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var extensions = isWindows ? new[] { "codex.exe", "codex.cmd", "codex.bat" } : ["codex"];
        var folders = (path ?? Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(isWindows ? ';' : ':', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim('"')).ToList();
        folders.AddRange(isWindows
            ? [Path.Combine(home, "AppData", "Roaming", "npm"), Path.Combine(home, ".local", "bin")]
            : new[] { "/opt/homebrew/bin", "/usr/local/bin", Path.Combine(home, ".local", "bin"),
                Path.Combine(home, ".npm-global", "bin"), "/Applications/ChatGPT.app/Contents/Resources",
                "/Applications/Codex.app/Contents/Resources" });
        if (isWindows)
        {
            var bundled = Path.Combine(home, "AppData", "Local", "OpenAI", "Codex", "bin");
            try
            {
                if (Directory.Exists(bundled)) folders.AddRange(Directory.EnumerateDirectories(bundled).OrderByDescending(Directory.GetLastWriteTimeUtc));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return folders.SelectMany(folder => extensions.Select(name => Path.Combine(folder, name))).Where(exists)
            .Distinct(isWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).ToArray();
    }

    private static bool IsExecutable(string path)
    {
        if (!File.Exists(path)) return false;
        if (OperatingSystem.IsWindows()) return true;
        try { return (File.GetUnixFileMode(path) & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    internal static ProcessStartInfo CreateStartInfo(string? executable = null)
    {
        executable ??= Resolve() ?? throw new CodexAppServerException(
            "Codex CLI was not found. Install Codex CLI, run `codex login`, and retry. / 找不到 Codex CLI，請安裝後執行 codex login 並重試。");
        var info = new ProcessStartInfo { FileName = executable, UseShellExecute = false,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        if (OperatingSystem.IsWindows() && Path.GetExtension(executable) is ".cmd" or ".bat")
        {
            // cmd.exe requires an extra outer quote pair for paths containing spaces.
            // The only shell input is the resolved local executable; arguments are constant.
            if (executable.IndexOfAny(['"', '%', '\r', '\n']) >= 0)
                throw new CodexAppServerException("Install Codex CLI in a path without quotes or percent characters.");
            info.FileName = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe";
            info.Arguments = $"/d /v:off /s /c \"\"{executable}\" app-server --listen stdio://\"";
        }
        else
        {
            info.ArgumentList.Add("app-server"); info.ArgumentList.Add("--listen"); info.ArgumentList.Add("stdio://");
        }
        return info;
    }
}
