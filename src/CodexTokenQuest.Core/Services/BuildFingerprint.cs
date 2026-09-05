using System.Security.Cryptography;
using System.Text;

namespace CodexTokenQuest.Core;

internal static class BuildFingerprint
{
    internal static string Calculate(string root)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var files = new[] { "src", "assets" }.SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*", SearchOption.AllDirectories))
            .Where(file => !Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar).Any(p => p is "bin" or "obj"))
            .Concat(Directory.EnumerateFiles(root).Where(file => Path.GetExtension(file) is ".props" or ".targets" or ".json"))
            .Order(StringComparer.Ordinal);
        foreach (var file in files)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(root, file).Replace('\\', '/')));
            hash.AppendData([0]); hash.AppendData(File.ReadAllBytes(file)); hash.AppendData([0]);
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
