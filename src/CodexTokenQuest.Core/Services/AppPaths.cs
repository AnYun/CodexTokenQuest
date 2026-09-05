namespace CodexTokenQuest.Core;

internal static class AppPaths
{
    internal static string StateDirectory => OperatingSystem.IsMacOS()
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "CodexTokenQuest")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexTokenQuest");

    internal static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(StateDirectory);
            File.AppendAllText(Path.Combine(StateDirectory, "lifecycle.log"), $"{DateTimeOffset.Now:O} {message}\n");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

// A persistent lock file, not a PID file: the OS releases the lease after a crash.
// Never delete it, which could allow two processes to lock different inodes.
internal sealed class InstanceLease : IDisposable
{
    private readonly FileStream _stream;
    private InstanceLease(FileStream stream) => _stream = stream;
    internal static InstanceLease? TryAcquire(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try { return new(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)); }
        catch (IOException) { return null; }
    }
    public void Dispose() => _stream.Dispose();
}
