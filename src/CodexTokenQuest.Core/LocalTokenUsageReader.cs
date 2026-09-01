using System.Text.Json;

namespace CodexTokenQuest.Core;

internal static class LocalTokenUsageReader
{
    internal static long? ReadForDate(DateOnly localDate, string? codexHome = null)
    {
        var root = codexHome;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Environment.GetEnvironmentVariable("CODEX_HOME");
        }
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
        }

        var sessionsRoot = Path.Combine(root, "sessions");
        if (!Directory.Exists(sessionsRoot))
        {
            return null;
        }

        long total = 0;
        var found = false;
        foreach (var folderDate in new[] { localDate.AddDays(-1), localDate, localDate.AddDays(1) })
        {
            var folder = Path.Combine(
                sessionsRoot,
                folderDate.Year.ToString("0000"),
                folderDate.Month.ToString("00"),
                folderDate.Day.ToString("00"));
            if (!Directory.Exists(folder))
            {
                continue;
            }

            try
            {
                foreach (var file in Directory.EnumerateFiles(folder, "*.jsonl", SearchOption.TopDirectoryOnly))
                {
                    AddSessionFile(file, localDate, ref total, ref found);
                }
            }
            catch (IOException)
            {
                // A session folder may change while Codex is rotating logs.
            }
            catch (UnauthorizedAccessException)
            {
                // Keep the account API value available when local sessions cannot be read.
            }
        }

        return found ? total : null;
    }

    private static void AddSessionFile(string path, DateOnly localDate, ref long total, ref bool found)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            while (reader.ReadLine() is { } line)
            {
                if (!TryReadIncrement(line, localDate, out var tokens))
                {
                    continue;
                }

                total = total > long.MaxValue - tokens ? long.MaxValue : total + tokens;
                found = true;
            }
        }
        catch (IOException)
        {
            // The active session can be replaced while it is being read.
        }
        catch (UnauthorizedAccessException)
        {
            // Skip protected session files without breaking the HUD refresh.
        }
    }

    internal static bool TryReadIncrement(string line, DateOnly localDate, out long tokens)
    {
        tokens = 0;
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var recordType) ||
                recordType.GetString() != "event_msg" ||
                !root.TryGetProperty("timestamp", out var timestampNode) ||
                !DateTimeOffset.TryParse(timestampNode.GetString(), out var timestamp) ||
                DateOnly.FromDateTime(timestamp.LocalDateTime) != localDate ||
                !root.TryGetProperty("payload", out var payload) ||
                !payload.TryGetProperty("type", out var payloadType) ||
                payloadType.GetString() != "token_count" ||
                !payload.TryGetProperty("info", out var info) ||
                info.ValueKind != JsonValueKind.Object ||
                !info.TryGetProperty("last_token_usage", out var lastUsage) ||
                lastUsage.ValueKind != JsonValueKind.Object ||
                !lastUsage.TryGetProperty("total_tokens", out var totalTokens) ||
                !totalTokens.TryGetInt64(out tokens) ||
                tokens < 0)
            {
                tokens = 0;
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
